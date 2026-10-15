using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ahis.template.identity.Contexts;
using ahis.template.identity.Interfaces;
using ahis.template.identity.Models.DTOs;
using ahis.template.identity.Models.Entities;
using FluentResults;
using Fido2NetLib;
using Fido2NetLib.Objects;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.DataProtection;
using ahis.template.domain.Models.ViewModels.AuthenticationVM;

namespace ahis.template.identity.Services;

public sealed class PasskeyService : IPasskeyService
{
    private const string RpConfigurationId = "development-localhost-v1";
    private static readonly TimeSpan CeremonyLifetime = TimeSpan.FromMinutes(5);
    private readonly IdentityContext _context;
    private readonly UserManager<ApplicationUser> _users;
    private readonly IAccountSecurityProofService _proof;
    private readonly IIdentityTokenStateService _tokenState;
    private readonly IIdentityRestrictionService _restrictions;
    private readonly IAuthenticationService _authentication;
    private readonly ILogger<PasskeyService> _logger;
    private readonly Fido2? _fido2;
    private readonly bool _enabled;
    private readonly IDataProtector _challengeProtector;

    public PasskeyService(IdentityContext context, UserManager<ApplicationUser> users,
        IAccountSecurityProofService proof, IIdentityTokenStateService tokenState,
        IIdentityRestrictionService restrictions, IAuthenticationService authentication,
        IHostEnvironment environment, ILogger<PasskeyService> logger, IDataProtectionProvider dataProtectionProvider)
    {
        _context = context; _users = users; _proof = proof; _tokenState = tokenState;
        _restrictions = restrictions; _authentication = authentication; _logger = logger;
        _challengeProtector = dataProtectionProvider.CreateProtector("AHIS.Passkeys.Challenge.v1");
        _enabled = environment.IsDevelopment();
        if (_enabled)
        {
            _fido2 = new Fido2(new Fido2Configuration
            {
                RPID = "localhost", RPName = "AHIS Development",
                Origins = new HashSet<string>(StringComparer.Ordinal) { "https://localhost:7270", "https://localhost:7280" }
            });
        }
    }

    public async Task<Result<PasskeyOptionsDto>> RegistrationOptionsAsync(string userId, string? ip, CancellationToken ct)
    {
        if (!Ready() || !await RateAllowedAsync(ip, userId, ct)) return Fail<PasskeyOptionsDto>();
        var user = await _users.FindByIdAsync(userId);
        if (user is null || user.MustResetPassword || !await _restrictions.IsAuthenticationAllowedAsync(user)) return Fail<PasskeyOptionsDto>();
        var credentials = await _context.PasskeyCredentials.Where(x => x.UserId == userId).ToListAsync(ct);
        if (credentials.Count >= 10) return Fail<PasskeyOptionsDto>();
        var options = _fido2!.RequestNewCredential(new RequestNewCredentialParams
        {
            User = new Fido2User { Id = Encoding.UTF8.GetBytes(user.Id), Name = user.UserName ?? user.Id, DisplayName = user.FullName is { Length: > 0 } name ? name : user.UserName ?? user.Id },
            ExcludeCredentials = credentials.Select(x => new PublicKeyCredentialDescriptor(Decode(x.CredentialId))).ToList(),
            AuthenticatorSelection = new AuthenticatorSelection { UserVerification = UserVerificationRequirement.Required, ResidentKey = ResidentKeyRequirement.Required },
            AttestationPreference = AttestationConveyancePreference.None
        });
        var ceremonyId = await SaveCeremonyAsync(PasskeyCeremonyPurpose.Registration, user.Id, _tokenState.GetSecurityVersion(user), options.Challenge, options.ToJson(), ip, ct);
        return ceremonyId is null ? Fail<PasskeyOptionsDto>() : Result.Ok(new PasskeyOptionsDto(ceremonyId, ToBrowserOptions(options.ToJson())));
    }

    public async Task<Result<AuthenticationResponseVM>> IssuePasskeySessionAsync(string userId, CancellationToken cancellationToken)
    {
        var user = await _users.FindByIdAsync(userId);
        if (user is null || user.MustResetPassword || !await _restrictions.IsAuthenticationAllowedAsync(user))
            return Fail<AuthenticationResponseVM>();
        return await _authentication.IssuePasskeySessionAsync(userId, cancellationToken);
    }

    public async Task<Result<PasskeyCredentialDto>> CompleteRegistrationAsync(string userId, PasskeyRegistrationDto request, CancellationToken ct)
    {
        if (!Ready() || string.IsNullOrWhiteSpace(request.DisplayName) || request.DisplayName.Length > 80 ||
            !await _proof.IsValidAsync(userId, request.StepUpProof, ct)) return Fail<PasskeyCredentialDto>();
        await using var tx = await _context.Database.BeginTransactionAsync(ct);
        var ceremony = await ClaimCeremonyAsync(request.CeremonyId, PasskeyCeremonyPurpose.Registration, userId, null, ct);
        var user = await _users.FindByIdAsync(userId);
        if (ceremony is null || user is null || user.MustResetPassword || !await _restrictions.IsAuthenticationAllowedAsync(user) ||
            !string.Equals(ceremony.SecurityVersion, _tokenState.GetSecurityVersion(user), StringComparison.Ordinal)) return Fail<PasskeyCredentialDto>();
        var count = await _context.PasskeyCredentials.CountAsync(x => x.UserId == userId, ct);
        if (count >= 10) return Fail<PasskeyCredentialDto>();
        var response = JsonSerializer.Deserialize<AuthenticatorAttestationRawResponse>(request.AttestationResponseJson);
        if (response is null) return Fail<PasskeyCredentialDto>();
        try
        {
            var registered = await _fido2!.MakeNewCredentialAsync(new MakeNewCredentialParams
            {
                AttestationResponse = response,
                OriginalOptions = RestoreCreateOptions(ceremony),
                IsCredentialIdUniqueToUserCallback = async (args, token) => !await _context.PasskeyCredentials.AnyAsync(x => x.CredentialId == Encode(args.CredentialId), token)
            }, ct);
            var parsedAttestation = AuthenticatorAttestationResponse.Parse(response);
            if (!parsedAttestation.AttestationObject.AuthData.UserPresent || !parsedAttestation.AttestationObject.AuthData.UserVerified)
                return Fail<PasskeyCredentialDto>();
            var entity = new PasskeyCredential
            {
                CredentialId = Encode(registered.Id), UserId = userId, PublicKey = registered.PublicKey,
                SignCount = registered.SignCount, DisplayName = request.DisplayName.Trim(),
                CreatedAtUtc = DateTime.UtcNow, IsBackupEligible = registered.IsBackupEligible, IsBackedUp = registered.IsBackedUp
            };
            _context.PasskeyCredentials.Add(entity);
            await _context.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            return Result.Ok(ToDto(entity));
        }
        catch (Exception ex) { await tx.RollbackAsync(ct); _logger.LogInformation("Passkey registration rejected ({FailureType}).", ex.GetType().Name); return Fail<PasskeyCredentialDto>(); }
    }

    public async Task<Result<IReadOnlyList<PasskeyCredentialDto>>> ListAsync(string userId, CancellationToken ct)
    {
        if (!Ready()) return Fail<IReadOnlyList<PasskeyCredentialDto>>();
        var rows = await _context.PasskeyCredentials.AsNoTracking().Where(x => x.UserId == userId).OrderBy(x => x.CreatedAtUtc).ToListAsync(ct);
        return Result.Ok<IReadOnlyList<PasskeyCredentialDto>>(rows.Select(ToDto).ToArray());
    }

    public async Task<Result> DeleteAsync(string userId, Guid credentialId, string? stepUpProof, string? password, string? recoveryCode, CancellationToken ct)
    {
        if (!Ready() || !await _proof.IsValidAsync(userId, stepUpProof, ct)) return Result.Fail("Unable to remove passkey.");
        await using var tx = await _context.Database.BeginTransactionAsync(ct);
        var row = await _context.PasskeyCredentials.SingleOrDefaultAsync(x => x.Id == credentialId && x.UserId == userId, ct);
        var user = await _users.FindByIdAsync(userId);
        if (row is null || user is null) return Result.Fail("Unable to remove passkey.");
        var remains = await _context.PasskeyCredentials.CountAsync(x => x.UserId == userId && x.Id != credentialId, ct);
        var hasPassword = await _users.HasPasswordAsync(user);
        var recovery = await _users.CountRecoveryCodesAsync(user);
        try
        {
            if (remains == 0)
            {
                var hasAlternativeRecovery = hasPassword && (user.TwoFactorEnabled || recovery > 0);
                var verifiedPassword = hasPassword && !string.IsNullOrWhiteSpace(password) && await _users.CheckPasswordAsync(user, password);
                var verifiedRecovery = recovery > 0 && !string.IsNullOrWhiteSpace(recoveryCode) &&
                    (await _users.RedeemTwoFactorRecoveryCodeAsync(user, recoveryCode)).Succeeded;
                if (!hasAlternativeRecovery || (!verifiedPassword && !verifiedRecovery)) return Result.Fail("Unable to remove passkey.");
            }
            _context.PasskeyCredentials.Remove(row);
            await _context.SaveChangesAsync(ct); await tx.CommitAsync(ct);
            return Result.Ok();
        }
        catch
        {
            await tx.RollbackAsync(CancellationToken.None);
            return Result.Fail("Unable to remove passkey.");
        }
    }

    public async Task<Result<PasskeyOptionsDto>> AuthenticationOptionsAsync(string? ip, CancellationToken ct)
    {
        if (!Ready() || !await RateAllowedAsync(ip, null, ct)) return Fail<PasskeyOptionsDto>();
        var options = _fido2!.GetAssertionOptions(new GetAssertionOptionsParams { UserVerification = UserVerificationRequirement.Required });
        var id = await SaveCeremonyAsync(PasskeyCeremonyPurpose.Authentication, null, null, options.Challenge, options.ToJson(), ip, ct);
        return id is null ? Fail<PasskeyOptionsDto>() : Result.Ok(new PasskeyOptionsDto(id, ToBrowserOptions(options.ToJson())));
    }

    public async Task<Result<AuthenticationResponseVM>> CompleteAuthenticationAsync(PasskeyAssertionDto request, string? ip, CancellationToken ct)
    {
        if (!Ready() || !await RateAllowedAsync(ip, null, ct)) return Fail<AuthenticationResponseVM>();
        await using var tx = await _context.Database.BeginTransactionAsync(ct);
        var ceremony = await ClaimCeremonyAsync(request.CeremonyId, PasskeyCeremonyPurpose.Authentication, null, Hash("ip:" + (ip ?? "unknown")), ct);
        if (ceremony is null) return Fail<AuthenticationResponseVM>();
        var committed = false;
        try
        {
            var response = JsonSerializer.Deserialize<AuthenticatorAssertionRawResponse>(request.AssertionResponseJson);
            if (response is null) return Fail<AuthenticationResponseVM>();
            var credentialId = Encode(response.RawId);
            var key = await _context.PasskeyCredentials.SingleOrDefaultAsync(x => x.CredentialId == credentialId, ct);
            if (key is null) return Fail<AuthenticationResponseVM>();
            var user = await _users.FindByIdAsync(key.UserId);
            if (user is null || user.MustResetPassword || !await _restrictions.IsAuthenticationAllowedAsync(user)) return Fail<AuthenticationResponseVM>();
            var assertion = await _fido2!.MakeAssertionAsync(new MakeAssertionParams
            {
                AssertionResponse = response, OriginalOptions = RestoreAssertionOptions(ceremony),
                StoredPublicKey = key.PublicKey, StoredSignatureCounter = checked((uint)key.SignCount),
                IsUserHandleOwnerOfCredentialIdCallback = (args, _) => Task.FromResult(args.UserHandle.SequenceEqual(Encoding.UTF8.GetBytes(user.Id)) &&
                    args.CredentialId.SequenceEqual(Decode(key.CredentialId)))
            }, ct);
            var parsedAssertion = AuthenticatorAssertionResponse.Parse(response);
            if (!parsedAssertion.AuthenticatorData.UserPresent || !parsedAssertion.AuthenticatorData.UserVerified)
                return Fail<AuthenticationResponseVM>();
            key.SignCount = assertion.SignCount; key.LastUsedAtUtc = DateTime.UtcNow;
            key.IsBackedUp = assertion.IsBackedUp;
            key.IsBackupEligible = parsedAssertion.AuthenticatorData.IsBackupEligible;
            await _context.SaveChangesAsync(ct);
            var issued = await _authentication.IssuePasskeySessionAsync(user.Id, ct);
            if (issued.IsFailed) return Fail<AuthenticationResponseVM>();
            await tx.CommitAsync(ct);
            committed = true;
            return Result.Ok(issued.Value);
        }
        catch (Exception ex) { _logger.LogInformation("Passkey assertion rejected ({FailureType}).", ex.GetType().Name); return Fail<AuthenticationResponseVM>(); }
        finally { if (!committed) await tx.RollbackAsync(ct); }
    }

    private async Task<string?> SaveCeremonyAsync(PasskeyCeremonyPurpose purpose, string? userId, string? version, byte[] challenge, string options, string? ip, CancellationToken ct)
    {
        var id = Microsoft.AspNetCore.WebUtilities.WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
        _context.PasskeyCeremonies.Add(new PasskeyCeremony
        {
            ChallengeReferenceHash = SHA256.HashData(Encoding.UTF8.GetBytes(id)), ChallengeHash = SHA256.HashData(challenge), Purpose = purpose,
            UserId = userId, SecurityVersion = version, RpConfigurationId = RpConfigurationId,
            CreatedAtUtc = DateTime.UtcNow, ExpiresAtUtc = DateTime.UtcNow.Add(CeremonyLifetime), RateLimitKeyHash = Hash("ip:" + (ip ?? "unknown")),
            UserRateLimitKeyHash = userId is null ? null : Hash("user:" + userId), OptionsJson = StripChallenge(options).GetRawText(), ProtectedChallenge = _challengeProtector.Protect(challenge)
        });
        await _context.SaveChangesAsync(ct);
        return id;
    }

    private async Task<PasskeyCeremony?> ClaimCeremonyAsync(string id, PasskeyCeremonyPurpose purpose, string? userId, string? rateLimitKeyHash, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(id)) return null;
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(id)); var now = DateTime.UtcNow;
        var query = _context.PasskeyCeremonies.Where(x => x.ChallengeReferenceHash.SequenceEqual(hash) && x.Purpose == purpose && x.ConsumedAtUtc == null && x.ExpiresAtUtc > now && x.RpConfigurationId == RpConfigurationId);
        if (userId is not null) query = query.Where(x => x.UserId == userId);
        else if (purpose == PasskeyCeremonyPurpose.Authentication && rateLimitKeyHash is not null) query = query.Where(x => x.RateLimitKeyHash == rateLimitKeyHash);
        var row = await query.SingleOrDefaultAsync(ct);
        if (row is null) return null;
        var challenge = _challengeProtector.Unprotect(row.ProtectedChallenge);
        if (!CryptographicOperations.FixedTimeEquals(row.ChallengeHash, SHA256.HashData(challenge))) return null;
        var claimed = await _context.PasskeyCeremonies.Where(x => x.Id == row.Id && x.ConsumedAtUtc == null && x.ExpiresAtUtc > now).ExecuteUpdateAsync(s => s.SetProperty(x => x.ConsumedAtUtc, now), ct);
        return claimed == 1 ? row : null;
    }

    private async Task<bool> RateAllowedAsync(string? ip, string? userId, CancellationToken ct)
    {
        var since = DateTime.UtcNow.AddMinutes(-1); var ipHash = Hash("ip:" + (ip ?? "unknown"));
        if (await _context.PasskeyCeremonies.CountAsync(x => x.CreatedAtUtc >= since && x.RateLimitKeyHash == ipHash, ct) >= 5) return false;
        if (userId is not null && await _context.PasskeyCeremonies.CountAsync(x => x.CreatedAtUtc >= since && x.UserRateLimitKeyHash == Hash("user:" + userId), ct) >= 5) return false;
        return true;
    }

    private bool Ready() => _enabled && _fido2 is not null;
    private CredentialCreateOptions RestoreCreateOptions(PasskeyCeremony row)
    {
        var json = JsonDocument.Parse(row.OptionsJson);
        var node = System.Text.Json.Nodes.JsonNode.Parse(json.RootElement.GetRawText())!;
        node["challenge"] = Encode(_challengeProtector.Unprotect(row.ProtectedChallenge));
        return CredentialCreateOptions.FromJson(node.ToJsonString());
    }
    private AssertionOptions RestoreAssertionOptions(PasskeyCeremony row)
    {
        var json = System.Text.Json.Nodes.JsonNode.Parse(row.OptionsJson)!;
        json["challenge"] = Encode(_challengeProtector.Unprotect(row.ProtectedChallenge));
        return AssertionOptions.FromJson(json.ToJsonString());
    }
    internal static System.Text.Json.JsonElement StripChallenge(string json)
    {
        var node = System.Text.Json.Nodes.JsonNode.Parse(json)!;
        node["challenge"] = string.Empty;
        return JsonDocument.Parse(node.ToJsonString()).RootElement.Clone();
    }
    internal static System.Text.Json.JsonElement ToBrowserOptions(string json) => JsonDocument.Parse(json).RootElement.Clone();
    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
    private static string Encode(byte[] value) => Microsoft.AspNetCore.WebUtilities.WebEncoders.Base64UrlEncode(value);
    private static byte[] Decode(string value) => Microsoft.AspNetCore.WebUtilities.WebEncoders.Base64UrlDecode(value);
    private static Result<T> Fail<T>() => Result.Fail<T>("Passkey operation failed.");
    private static PasskeyCredentialDto ToDto(PasskeyCredential x) => new(x.Id, x.DisplayName, x.CreatedAtUtc, x.LastUsedAtUtc);
}
