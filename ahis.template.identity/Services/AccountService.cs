using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text;
using FluentResults;
using ahis.template.identity.Interfaces;
using ahis.template.identity.Models.Entities;
using ahis.template.identity.Models.DTOs;
using ahis.template.identity.SharedKernel;
using ahis.template.identity.Contexts;
using Microsoft.EntityFrameworkCore;


namespace ahis.template.identity.Services
{
    

    public class AccountService : IAccountService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IEmailSender _emailSender;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AccountService> _logger;
        private readonly IIdentityTokenStateService _tokenState;
        private readonly IAccountSecurityProofService _securityProof;
        private readonly IdentityUnitOfWork _unitOfWork;
        private readonly IdentityContext _context;

        public AccountService(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IEmailSender emailSender,
            IConfiguration configuration,
            ILogger<AccountService> logger,
            IIdentityTokenStateService tokenState,
            IAccountSecurityProofService securityProof,
            IdentityUnitOfWork unitOfWork,
            IdentityContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _emailSender = emailSender;
            _configuration = configuration;
            _logger = logger;
            _tokenState = tokenState;
            _securityProof = securityProof;
            _unitOfWork = unitOfWork;
            _context = context;
        }

        // 1. Register new user (no password yet)
        public async Task<Result<string>> RegisterAsync(string email, string userName, string callbackBaseUrl)
        {
            try
            {
                var existing = await _userManager.FindByEmailAsync(email);
                if (existing != null)
                {
                    _logger.LogWarning("Registration failed: email already exists {Email}", email);
                    return Result.Fail<string>("Email is already registered.");
                }

                var user = new ApplicationUser
                {
                    UserName = userName,
                    Email = email,
                    EmailConfirmed = false,
                    IsActive = true,
                    IsDeleted = false,
                    CreatedAt = DateTime.UtcNow
                };

                var createResult = await _userManager.CreateAsync(user);
                if (!createResult.Succeeded)
                {
                    _logger.LogWarning("User creation failed: {Errors}", string.Join(';', createResult.Errors.Select(e => e.Description)));
                    var errors = string.Join(';', createResult.Errors.Select(e => e.Description));
                    return Result.Fail<string>(errors);
                }

                // Send confirmation email
                var emailSendResult = await SendEmailConfirmationAsync(user, callbackBaseUrl);
                if (!emailSendResult.IsSuccess)
                {
                    _logger.LogWarning("Failed to send confirmation email to {Email}", email);
                    return Result.Fail<string>("User created but failed to send confirmation email.");
                }

                return Result.Ok(user.Id.ToString());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during RegisterAsync");
                return Result.Fail<string>("An unexpected error occurred while registering user.");
            }
        }

        // 2. Send email for verification
        public async Task<Result> SendEmailConfirmationAsync(ApplicationUser user, string callbackBaseUrl)
        {
            try
            {
                var configuredCallbackBaseUrl = _configuration["Identity:PublicClientBaseUrl"];
                if (string.IsNullOrWhiteSpace(configuredCallbackBaseUrl))
                {
                    _logger.LogError("Confirmation email cannot be resent because Identity:PublicClientBaseUrl is not configured.");
                    return Result.Ok();
                }

                var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

                // build callback url: e.g. {callbackBaseUrl}/api/account/confirm-email?userId={userId}&token={token}
                var callbackUrl = BuildCallbackUrl(configuredCallbackBaseUrl, "account/confirm-email", new Dictionary<string, string>
                {
                    ["userId"] = user.Id.ToString(),
                    ["token"] = encodedToken
                });

                var subject = "Confirm your email";
                var message = $"Please confirm your account by <a href=\"{callbackUrl}\">clicking here</a>.";

                await _emailSender.SendEmailAsync(user.Email!, subject, message);

                _logger.LogInformation("Email confirmation sent to {Email}", user.Email);

                return Result.Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SendEmailConfirmationAsync failed");
                return Result.Fail("Failed to send confirmation email.");
            }
        }

        // 3. Confirm email
        public async Task<Result> ConfirmEmailAsync(string userId, string encodedToken, CancellationToken cancellationToken)
        {
            try
            {

                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                    return Result.Fail("User not found.");

                var token = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(encodedToken));
                var result = await _userManager.ConfirmEmailAsync(user, token);
                if (!result.Succeeded)
                {
                    var errors = string.Join(';', result.Errors.Select(e => e.Description));
                    _logger.LogWarning("Email confirmation failed for user {UserId}: {Errors}", userId, errors);
                    return Result.Fail(errors);
                }

                user.EmailVerifiedAt = DateTime.UtcNow;
                var updateResult = await _userManager.UpdateAsync(user);
                if (!updateResult.Succeeded)
                    return Result.Fail("Unable to confirm email.");

                var setupResult = await SendInitialPasswordSetupEmailAsync(user, cancellationToken);
                if (!setupResult.IsSuccess)
                    return Result.Fail("Email was confirmed, but the password setup email could not be sent.");

                return Result.Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ConfirmEmailAsync error");
                return Result.Fail("An unexpected error occurred while confirming email.");
            }
        }

        // 4. Set password on first login (user has no password yet)
        public async Task<Result> SetPasswordFirstTimeAsync(
            string userId,
            string token,
            string password,
            CancellationToken cancellationToken)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (!_tokenState.IsEligible(user) || user is null || !user.EmailConfirmed)
                    return Result.Fail("Invalid or expired password setup link.");

                var decodedToken = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(token));
                var isValidToken = await _userManager.VerifyUserTokenAsync(
                    user,
                    InitialPasswordSetupTokenProvider.ProviderName,
                    InitialPasswordSetupTokenProvider.Purpose,
                    decodedToken);
                if (!isValidToken)
                    return Result.Fail("Invalid or expired password setup link.");

                // If user already has password, prevent using this method
                var hasPassword = await _userManager.HasPasswordAsync(user);
                if (hasPassword)
                    return Result.Fail("Invalid or expired password setup link.");

                var addPasswordResult = await _userManager.AddPasswordAsync(user, password);
                if (!addPasswordResult.Succeeded)
                {
                    var errors = string.Join(';', addPasswordResult.Errors.Select(e => e.Description));
                    _logger.LogWarning("AddPassword failed for user {UserId}: {Errors}", userId, errors);
                    return Result.Fail(errors);
                }

                return Result.Ok();
            }
            catch (FormatException)
            {
                return Result.Fail("Invalid or expired password setup link.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SetPasswordFirstTimeAsync error");
                return Result.Fail("An unexpected error occurred while setting password.");
            }
        }


        // 5. Update profile (first login profile completion)
        public async Task<Result<ProfileUpdateDto>> UpdateProfileAsync(string userId, ProfileUpdateDto dto)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return Result.Fail("User not found.");
                }
                    

                user.FirstName = dto.FirstName?.Trim();
                user.LastName = dto.LastName?.Trim();
                user.DateOfBirth = dto.DateOfBirth;
                user.PhoneNumber = dto.PhoneNumber;
                user.IsAccountConfigured = dto.MarkAccountConfigured;
                user.UpdatedAt = DateTime.UtcNow;

                var updateResult = await _userManager.UpdateAsync(user);
                if (!updateResult.Succeeded)
                {
                    var errors = string.Join(';', updateResult.Errors.Select(e => e.Description));
                    _logger.LogWarning("UpdateProfileAsync failed for user {UserId}: {Errors}", userId, errors);
                    return Result.Fail(errors);
                }

                return Result.Ok<ProfileUpdateDto>(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UpdateProfileAsync error");
                return Result.Fail("An unexpected error occurred while updating profile.");
            }
        }

        // 6. Authenticator setup: reset/generate key and return provisioning URI
        public async Task<Result<AuthenticatorSetupDto>> GenerateAuthenticatorSetupAsync(string userId)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return Result.Fail<AuthenticatorSetupDto>("User not found.");
                }

                if (user.TwoFactorEnabled)
                {
                    return Result.Fail<AuthenticatorSetupDto>(
                        "Two-factor authentication is already enabled. Use an approved authenticator reset flow.");
                }

                // Reset authenticator key (so QR code is generated fresh)
                var resetResult = await _userManager.ResetAuthenticatorKeyAsync(user);
                if (!resetResult.Succeeded)
                {
                    _logger.LogWarning("Failed to reset authenticator key for user {UserId}", userId);
                    return Result.Fail<AuthenticatorSetupDto>("Failed to generate authenticator setup.");
                }

                var key = await _userManager.GetAuthenticatorKeyAsync(user);
                if (string.IsNullOrWhiteSpace(key))
                {
                    _logger.LogWarning("Authenticator key was not available after reset for user {UserId}", userId);
                    return Result.Fail<AuthenticatorSetupDto>("Failed to generate authenticator setup.");
                }

                // Build otpauth URI
                var issuer = _configuration["Identity:Issuer"] ?? _configuration["AppSettings:AppName"] ?? "AHIS";
                var email = user.Email ?? user.UserName;
                var uri = $"otpauth://totp/{Uri.EscapeDataString(issuer)}:{Uri.EscapeDataString(email)}?secret={key}&issuer={Uri.EscapeDataString(issuer)}&digits=6";

                // Save values (do not enable 2FA yet)
                user.AuthenticatorKey = key;
                user.AuthenticatorUri = uri;
                var updateResult = await _userManager.UpdateAsync(user);
                if (!updateResult.Succeeded)
                {
                    _logger.LogWarning("Failed to save authenticator setup for user {UserId}", userId);
                    return Result.Fail<AuthenticatorSetupDto>("Failed to generate authenticator setup.");
                }

                return Result.Ok(new AuthenticatorSetupDto { Key = key, ProvisionUri = uri });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GenerateAuthenticatorSetupAsync error");
                return Result.Fail<AuthenticatorSetupDto>("Failed to generate authenticator setup.");
            }
        }

        // Enable authenticator after user verifies a code
        public async Task<Result<IEnumerable<string>>> EnableAuthenticatorAsync(string userId, string verificationCode)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                    return Result.Fail<IEnumerable<string>>("User not found.");

                // Verify token
                var isValid = await _userManager.VerifyTwoFactorTokenAsync(user, _userManager.Options.Tokens.AuthenticatorTokenProvider, verificationCode);
                if (!isValid)
                    return Result.Fail<IEnumerable<string>>("Invalid verification code.");

                if (user.TwoFactorEnabled)
                    return Result.Fail<IEnumerable<string>>("Two-factor authentication is already enabled.");

                await _unitOfWork.BeginTransactionAsync();

                // Enable two factor for user
                var enableResult = await _userManager.SetTwoFactorEnabledAsync(user, true);
                if (!enableResult.Succeeded)
                    return Result.Fail<IEnumerable<string>>("Failed to enable two-factor authentication.");
                user.TwoFactorEnabledAt = DateTime.UtcNow;

                // Generate recovery codes
                var recovery = await _userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 10);
                var codes = recovery?.ToArray() ?? Array.Empty<string>();
                if (codes.Length != 10)
                    return Result.Fail<IEnumerable<string>>("Failed to generate recovery codes.");

                var updateResult = await _userManager.UpdateAsync(user);
                if (!updateResult.Succeeded)
                    return Result.Fail<IEnumerable<string>>("Failed to enable two-factor authentication.");

                await _unitOfWork.CommitTransactionAsync();

                return Result.Ok(codes.AsEnumerable());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "EnableAuthenticatorAsync error");
                return Result.Fail<IEnumerable<string>>("Failed to enable authenticator.");
            }
            finally
            {
                await _unitOfWork.RollbackTransactionAsync(CancellationToken.None);
            }
        }

        public async Task<Result> DisableAuthenticatorAsync(string userId)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                    return Result.Fail("User not found.");

                var disableResult = await _userManager.SetTwoFactorEnabledAsync(user, false);
                if (!disableResult.Succeeded)
                    return Result.Fail("Failed to disable authenticator.");
                var clearCodesResult = await _userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 0);
                if (clearCodesResult is null)
                    return Result.Fail("Failed to disable authenticator.");
                user.AuthenticatorKey = null;
                user.AuthenticatorUri = null;
                user.TwoFactorEnabledAt = null;

                var update = await _userManager.UpdateAsync(user);
                if (!update.Succeeded)
                    return Result.Fail("Failed to disable authenticator.");

                var invalidation = await _tokenState.InvalidateAsync(user);
                return invalidation.Succeeded ? Result.Ok() : Result.Fail("Failed to invalidate sessions.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DisableAuthenticatorAsync error");
                return Result.Fail("Failed to disable authenticator.");
            }
        }

        public async Task<Result> ChangePasswordAsync(string userId, string currentPassword, string newPassword, CancellationToken cancellationToken)
        {

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return Result.Fail("User not found.");

            var result = await _userManager.ChangePasswordAsync(
                user,
                currentPassword,
                newPassword);

            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e =>
                {
                    var error = new Error(e.Description);

                    if (e.Code.Contains("Password"))
                        error.WithMetadata("Field", "newPassword");
                    else if (e.Code.Contains("PasswordMismatch"))
                        error.WithMetadata("Field", "currentPassword");

                    return error;
                });

                return Result.Fail(errors);
            }

            // Invalidate all existing sessions
            var invalidation = await _tokenState.InvalidateAsync(user, cancellationToken);
            if (!invalidation.Succeeded)
                return Result.Fail("Failed to invalidate sessions.");

            _logger.LogInformation(
                "Password changed successfully for user {UserId}",
                userId);

            return Result.Ok();
        }

        public async Task<Result<string>> ReauthenticateAsync(
            string userId,
            string password,
            string? twoFactorCode,
            CancellationToken cancellationToken)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (!_tokenState.IsEligible(user) || string.IsNullOrWhiteSpace(password))
                return Result.Fail<string>("Re-authentication failed.");

            if (!await _userManager.CheckPasswordAsync(user!, password))
                return Result.Fail<string>("Re-authentication failed.");

            if (user!.TwoFactorEnabled)
            {
                if (string.IsNullOrWhiteSpace(twoFactorCode) ||
                    !await _userManager.VerifyTwoFactorTokenAsync(
                        user,
                        _userManager.Options.Tokens.AuthenticatorTokenProvider,
                        twoFactorCode))
                {
                    return Result.Fail<string>("Re-authentication failed.");
                }
            }

            return await _securityProof.CreateAsync(user, cancellationToken);
        }

        public async Task<Result> RevokeAllSessionsAsync(
            string userId,
            string stepUpProof,
            CancellationToken cancellationToken)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (!_tokenState.IsEligible(user) ||
                !await _securityProof.IsValidAsync(userId, stepUpProof, cancellationToken))
            {
                return Result.Fail("Unable to revoke sessions.");
            }

            try
            {
                await _unitOfWork.BeginTransactionAsync(cancellationToken);
                var invalidation = await _tokenState.InvalidateAsync(user!, cancellationToken);
                if (!invalidation.Succeeded)
                    return OperationalSessionRevocationFailure();

                await _unitOfWork.CommitTransactionAsync(cancellationToken);
                await NotifySecurityChangeAsync(user!.Email, "Sessions revoked", "All sessions for your account were revoked. Sign in again to continue.");
                return Result.Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Session revocation failed for user {UserId}", userId);
                return OperationalSessionRevocationFailure();
            }
            finally
            {
                await _unitOfWork.RollbackTransactionAsync(CancellationToken.None);
            }
        }

        public async Task<Result<(IReadOnlyList<ActiveSessionDto> Sessions, int TotalCount)>> GetActiveSessionsAsync(
            string userId,
            Guid? currentSessionId,
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken)
        {
            var now = DateTime.UtcNow;
            var sessions = _context.RefreshSessions
                .AsNoTracking()
                .Where(session => session.UserId == userId && !session.IsRevoked && session.ExpiresAt > now);

            var totalCount = await sessions.CountAsync(cancellationToken);
            var items = await sessions
                .OrderByDescending(session => session.LastUsedAt)
                .ThenByDescending(session => session.Id)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(session => new ActiveSessionDto
                {
                    SessionId = session.PublicId,
                    CreatedAt = session.CreatedAt,
                    LastUsedAt = session.LastUsedAt,
                    ExpiresAt = session.ExpiresAt,
                    IsCurrent = currentSessionId.HasValue && session.PublicId == currentSessionId.Value
                })
                .ToListAsync(cancellationToken);

            return Result.Ok(((IReadOnlyList<ActiveSessionDto>)items, totalCount));
        }

        public async Task<Result> ResetAuthenticatorAsync(
            string userId,
            string stepUpProof,
            CancellationToken cancellationToken)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user is null || !user.TwoFactorEnabled ||
                !await _securityProof.IsValidAsync(userId, stepUpProof, cancellationToken))
            {
                return Result.Fail("Unable to reset authenticator.");
            }

            try
            {
                await _unitOfWork.BeginTransactionAsync(cancellationToken);
                var disableResult = await _userManager.SetTwoFactorEnabledAsync(user, false);
                var resetResult = disableResult.Succeeded
                    ? await _userManager.ResetAuthenticatorKeyAsync(user)
                    : IdentityResult.Failed();
                if (!resetResult.Succeeded)
                    return Result.Fail("Unable to reset authenticator.");

                var recoveryCodes = await _userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 0);
                if (recoveryCodes is null)
                    return Result.Fail("Unable to reset authenticator.");

                user.AuthenticatorKey = null;
                user.AuthenticatorUri = null;
                user.TwoFactorEnabledAt = null;
                user.UpdatedAt = DateTime.UtcNow;
                var updateResult = await _userManager.UpdateAsync(user);
                if (!updateResult.Succeeded)
                    return Result.Fail("Unable to reset authenticator.");

                var invalidation = await _tokenState.InvalidateAsync(user, cancellationToken);
                if (!invalidation.Succeeded)
                    return Result.Fail("Unable to reset authenticator.");

                await _unitOfWork.CommitTransactionAsync(cancellationToken);
                await NotifySecurityChangeAsync(user.Email, "Authenticator reset", "Your authenticator was reset. Configure and verify a new authenticator before enabling two-factor authentication again.");
                return Result.Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Authenticator reset failed for user {UserId}", userId);
                return Result.Fail("Unable to reset authenticator.");
            }
            finally
            {
                await _unitOfWork.RollbackTransactionAsync(CancellationToken.None);
            }
        }

        public async Task<Result> RequestEmailChangeAsync(
            string userId,
            string newEmail,
            string callbackBaseUrl,
            string stepUpProof,
            CancellationToken cancellationToken)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user is null || string.IsNullOrWhiteSpace(newEmail) || string.IsNullOrWhiteSpace(callbackBaseUrl) ||
                !await _securityProof.IsValidAsync(userId, stepUpProof, cancellationToken))
            {
                return Result.Fail("Unable to process email change.");
            }

            var existing = await _userManager.FindByEmailAsync(newEmail);
            if (existing is not null && existing.Id != user.Id)
                return Result.Fail("Unable to process email change.");

            try
            {
                var token = await _userManager.GenerateChangeEmailTokenAsync(user, newEmail);
                var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
                var callbackUrl = BuildCallbackUrl(callbackBaseUrl, "account/change-email/confirm", new Dictionary<string, string>
                {
                    ["userId"] = user.Id,
                    ["newEmail"] = newEmail,
                    ["token"] = encodedToken
                });

                await _emailSender.SendEmailAsync(
                    newEmail,
                    "Confirm your new email address",
                    $"Confirm your new email address by <a href=\"{callbackUrl}\">clicking here</a>.");
                await NotifySecurityChangeAsync(user.Email, "Email change requested", "A request was made to change the email address on your account.");
                return Result.Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Email change request failed for user {UserId}", userId);
                return Result.Fail("Unable to process email change.");
            }
        }

        public async Task<Result> ConfirmEmailChangeAsync(
            string userId,
            string newEmail,
            string token,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(newEmail) || string.IsNullOrWhiteSpace(token))
                return Result.Fail("Unable to confirm email change.");

            var user = await _userManager.FindByIdAsync(userId);
            if (user is null)
                return Result.Fail("Unable to confirm email change.");

            try
            {
                var decodedToken = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(token));
                var previousEmail = user.Email;
                var userNameMatchesPreviousEmail = string.Equals(user.UserName, previousEmail, StringComparison.OrdinalIgnoreCase);

                await _unitOfWork.BeginTransactionAsync(cancellationToken);
                var changeResult = await _userManager.ChangeEmailAsync(user, newEmail, decodedToken);
                if (!changeResult.Succeeded)
                    return Result.Fail("Unable to confirm email change.");

                if (userNameMatchesPreviousEmail)
                {
                    var userNameResult = await _userManager.SetUserNameAsync(user, newEmail);
                    if (!userNameResult.Succeeded)
                        return Result.Fail("Unable to confirm email change.");
                }

                user.EmailVerifiedAt = DateTime.UtcNow;
                user.UpdatedAt = DateTime.UtcNow;
                var updateResult = await _userManager.UpdateAsync(user);
                if (!updateResult.Succeeded)
                    return Result.Fail("Unable to confirm email change.");

                var invalidation = await _tokenState.InvalidateAsync(user, cancellationToken);
                if (!invalidation.Succeeded)
                    return Result.Fail("Unable to confirm email change.");

                await _unitOfWork.CommitTransactionAsync(cancellationToken);
                await NotifySecurityChangeAsync(previousEmail, "Email address changed", "The email address on your account was changed.");
                return Result.Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Email change confirmation failed for user {UserId}", userId);
                return Result.Fail("Unable to confirm email change.");
            }
            finally
            {
                await _unitOfWork.RollbackTransactionAsync(CancellationToken.None);
            }
        }

        public async Task<Result> DeactivateAsync(
            string userId,
            bool confirmation,
            string stepUpProof,
            CancellationToken cancellationToken)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user is null || !confirmation || !await _securityProof.IsValidAsync(userId, stepUpProof, cancellationToken))
                return Result.Fail("Unable to deactivate account.");

            try
            {
                await _unitOfWork.BeginTransactionAsync(cancellationToken);
                user.IsActive = false;
                user.IsDeleted = true;
                user.DeletedAt = DateTime.UtcNow;
                user.UpdatedAt = DateTime.UtcNow;
                var updateResult = await _userManager.UpdateAsync(user);
                if (!updateResult.Succeeded)
                    return Result.Fail("Unable to deactivate account.");

                var invalidation = await _tokenState.InvalidateAsync(user, cancellationToken);
                if (!invalidation.Succeeded)
                    return Result.Fail("Unable to deactivate account.");

                await _unitOfWork.CommitTransactionAsync(cancellationToken);
                await NotifySecurityChangeAsync(user.Email, "Account deactivated", "Your account was deactivated. Contact support if you need help with account recovery.");
                return Result.Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Account deactivation failed for user {UserId}", userId);
                return Result.Fail("Unable to deactivate account.");
            }
            finally
            {
                await _unitOfWork.RollbackTransactionAsync(CancellationToken.None);
            }
        }

        public async Task<Result> ResendConfirmationEmailAsync(string email, string callbackBaseUrl, CancellationToken cancellationToken)
        {
            try
            {
                var user = await _userManager.FindByEmailAsync(email);


                if (user == null || !_tokenState.IsEligible(user))
                    return Result.Ok();

                if (user.EmailConfirmed)
                {
                    if (!await _userManager.HasPasswordAsync(user))
                        await SendInitialPasswordSetupEmailAsync(user, cancellationToken);
                    return Result.Ok();
                }


                var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

                // build callback url: e.g. {callbackBaseUrl}/api/account/confirm-email?userId={userId}&token={token}
                var callbackUrl = BuildCallbackUrl(callbackBaseUrl, "account/confirm-email", new Dictionary<string, string>
                {
                    ["userId"] = user.Id.ToString(),
                    ["token"] = encodedToken
                });

                var subject = "Confirm your email";
                var message = $"Please confirm your account by <a href=\"{callbackUrl}\">clicking here</a>.";

                await _emailSender.SendEmailAsync(user.Email!, subject, message);

                _logger.LogInformation("Email confirmation sent to {Email}", user.Email);

                return Result.Ok();
            }
            catch (Exception ex)
            {
                // Never leak errors to client
                _logger.LogError(
                    ex,
                    "Failed to resend confirmation email for {Email}",
                    email);

                return Result.Ok();
            }
        }

        public async Task<Result<AccountMeDto>> GetMyAccountAsync(string userId, CancellationToken cancellationToken)
        {
            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
            {
                _logger.LogWarning(
                    "Authenticated user not found. UserId: {UserId}",
                    userId);

                return Result.Fail("User not found.");
            }

            return Result.Ok(new AccountMeDto
            {
                UserId = user.Id,
                Email = user.Email!,
                Username = user.UserName!,
                FirstName = user.FirstName,
                LastName = user.LastName,
                DateOfBirth = user.DateOfBirth,
                PhoneNumber = user.PhoneNumber,
                EmailConfirmed = user.EmailConfirmed,
                TwoFactorEnabled = user.TwoFactorEnabled
            });
        }


        #region Helpers

        private async Task<Result> SendInitialPasswordSetupEmailAsync(
            ApplicationUser user,
            CancellationToken cancellationToken)
        {
            var callbackBaseUrl = _configuration["Identity:PublicClientBaseUrl"];
            if (string.IsNullOrWhiteSpace(callbackBaseUrl))
            {
                _logger.LogError("Initial password setup email cannot be sent because Identity:PublicClientBaseUrl is not configured.");
                return Result.Fail("Password setup email is unavailable.");
            }

            var token = await _userManager.GenerateUserTokenAsync(
                user,
                InitialPasswordSetupTokenProvider.ProviderName,
                InitialPasswordSetupTokenProvider.Purpose);
            var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
            var callbackUrl = BuildCallbackUrl(callbackBaseUrl, "set-password", new Dictionary<string, string>
            {
                ["userId"] = user.Id,
                ["token"] = encodedToken
            });

            try
            {
                await _emailSender.SendEmailAsync(
                    user.Email!,
                    "Set your password",
                    $"Set your password by <a href=\"{callbackUrl}\">opening this secure link</a>.");
                return Result.Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send initial password setup email.");
                return Result.Fail("Password setup email is unavailable.");
            }
        }

        private async Task NotifySecurityChangeAsync(string? email, string subject, string message)
        {
            if (string.IsNullOrWhiteSpace(email))
                return;

            try
            {
                await _emailSender.SendEmailAsync(email, subject, message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send security notification {Subject}", subject);
            }
        }

        private static Result OperationalSessionRevocationFailure() =>
            Result.Fail(new Error("Unable to revoke sessions.")
                .WithMetadata("OperationalFailure", true));

        private string BuildCallbackUrl(string baseUrl, string path, IDictionary<string, string> query)
        {
            if (baseUrl.EndsWith('/')) baseUrl = baseUrl.TrimEnd('/');
            var sb = new StringBuilder();
            sb.Append(baseUrl);
            if (!path.StartsWith('/')) sb.Append('/');
            sb.Append(path);

            if (query != null && query.Count > 0)
            {
                sb.Append('?');
                sb.Append(string.Join('&', query.Select(kv => $"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value)}")));
            }

            return sb.ToString();
        }

        #endregion
    }
}
