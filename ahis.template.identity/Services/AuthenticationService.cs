using ahis.template.domain.Enums;
using ahis.template.domain.Models.ViewModels.AuthenticationVM;
using ahis.template.identity.Contexts;
using ahis.template.identity.Interfaces;
using ahis.template.identity.Models;
using ahis.template.identity.Models.Entities;
using ahis.template.identity.SharedKernel;
using FluentResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace ahis.template.identity.Services
{
    public class AuthenticationService : IAuthenticationService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IdentityContext _context;
        private readonly IEmailSender _emailSender;
        private readonly IdentityUnitOfWork _unitOfWork;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthenticationService> _logger;
        private readonly IIdentityTokenStateService _tokenState;
        private readonly Microsoft.AspNetCore.Http.IHttpContextAccessor _httpContextAccessor;

        public AuthenticationService(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IdentityContext context,
            IEmailSender emailSender,
            IdentityUnitOfWork unitOfWork,
            IConfiguration configuration,
            ILogger<AuthenticationService> logger,
            IIdentityTokenStateService tokenState,
            Microsoft.AspNetCore.Http.IHttpContextAccessor httpContextAccessor)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
            _emailSender = emailSender;
            _unitOfWork = unitOfWork;
            _configuration = configuration;
            _logger = logger;
            _tokenState = tokenState;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<Result<AuthenticationResponseVM>> LoginAsync(string userNameOrEmail, string password, bool rememberMe = false)
        {
            
            try
            {
                // Check for user existance
                var user = await _userManager.FindByNameAsync(userNameOrEmail) ?? await _userManager.FindByEmailAsync(userNameOrEmail);
                if (user == null)
                    return Result.Fail<AuthenticationResponseVM>("Invalid credentials.");

                if (!_tokenState.IsEligible(user))
                    return Result.Fail<AuthenticationResponseVM>("User is not active.");


                // Login 
                /// CheckPasswordSignInAsync - Only check for the correct credential
                /// PasswordSignInAsync - Check overall like username, password, IsEmailConfirm, IsActive etc.
                var signInResult = await _signInManager.PasswordSignInAsync(user, password, isPersistent: rememberMe, lockoutOnFailure: true);
                //var signInResult = await _signInManager.CheckPasswordSignInAsync(user, password, lockoutOnFailure: true);

                if (!signInResult.Succeeded)
                {
                    if (signInResult.IsLockedOut)
                        return Result.Fail<AuthenticationResponseVM>("User is locked out.");

                    if (signInResult.RequiresTwoFactor)
                    {
                        var challengeResult = await IssueTwoFactorChallengeAsync(user);
                        return challengeResult.IsSuccess
                            ? Result.Ok(new AuthenticationResponseVM { RequiresTwoFactor = true })
                            : Result.Fail<AuthenticationResponseVM>("Login failed.");
                    }

                    return Result.Fail<AuthenticationResponseVM>("Invalid credentials.");
                }

                await ClearIdentityApplicationCookieAsync();

                // create tokens
                var securityVersion = _tokenState.GetSecurityVersion(user);
                if (securityVersion is null)
                    return Result.Fail<AuthenticationResponseVM>("Login failed.");
                var accessToken = await GenerateJwtTokenAsync(user, securityVersion);
                var (refreshToken, refreshExpiresAt) = GenerateRefreshToken();

                // persist refresh token

                await _unitOfWork.BeginTransactionAsync();

                await StoreRefreshTokenAsync(user.Id, refreshToken, refreshExpiresAt, securityVersion);

                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();

                var isEmailConfirmed = await _userManager.IsEmailConfirmedAsync(user);
                var hasPassword = await _userManager.HasPasswordAsync(user);
                var isTwoFactorEnabled = await _userManager.GetTwoFactorEnabledAsync(user);

                var response = new AuthenticationResponseVM
                {
                    AccessToken = accessToken,
                    ExpiresInSeconds = int.Parse(_configuration["Jwt:AccessTokenExpirySeconds"] ?? "3600"),
                    RefreshToken = refreshToken,
                    RefreshTokenExpiresAt = refreshExpiresAt,
                    UserId = user.Id.ToString(),
                    RequiresTwoFactor = isTwoFactorEnabled,
                    IsPasswordCreated = hasPassword,
                    IsEmailConfirmed = isEmailConfirmed
                };

                return Result.Ok(response);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(ex, "Login failed");
                return Result.Fail<AuthenticationResponseVM>("Login failed.");
            }
            
        }

        //public async Task<Result> LogoutAsync()
        //{
        //    try
        //    {
        //        await _signInManager.SignOutAsync();
        //        return Result.Ok();
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Logout failed");
        //        return Result.Fail("Logout failed.");
        //    }
        //}

        public async Task LogoutAsync(string refreshToken)
        {
            try
            {
                await _unitOfWork.BeginTransactionAsync();
                var tokenHash = RefreshTokenHashing.Compute(refreshToken);
                var token = await _context.RefreshTokens
                    .FirstOrDefaultAsync(x =>
                        x.TokenHash == tokenHash &&
                        !x.IsRevoked &&
                        x.ExpiresAt > DateTime.UtcNow);

                if (token == null)
                    return;

                var revokedAt = DateTime.UtcNow;
                await RevokeSessionAsync(token.SessionId, revokedAt);
                await _unitOfWork.CommitTransactionAsync();

                await _signInManager.SignOutAsync();
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Logout failed.");
            }
            finally
            {
                await _unitOfWork.RollbackTransactionAsync();
            }

        }

        public async Task<Result<AuthenticationResponseVM>> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken)
        {
            try
            {
                await _unitOfWork.BeginTransactionAsync(cancellationToken);
                var tokenHash = RefreshTokenHashing.Compute(refreshToken);
                var storedToken = await _context.RefreshTokens
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.TokenHash == tokenHash, cancellationToken);

                if (storedToken == null)
                    return Result.Fail("Invalid refresh token.");

                if (storedToken.IsRevoked)
                {
                    _logger.LogWarning(
                        "Refresh token reuse detected for user {UserId}",
                        storedToken.UserId);

                    var reusedBy = await _userManager.FindByIdAsync(storedToken.UserId);
                    if (reusedBy is not null)
                    {
                        var invalidation = await _tokenState.InvalidateAsync(reusedBy, cancellationToken);
                        if (!invalidation.Succeeded)
                            return Result.Fail("Invalid refresh token.");
                    }
                    await _unitOfWork.CommitTransactionAsync(cancellationToken);

                    return Result.Fail("Invalid refresh token.");
                }

                if (storedToken.ExpiresAt <= DateTime.UtcNow)
                    return Result.Fail("Invalid refresh token.");

                var user = await _userManager.FindByIdAsync(storedToken.UserId);
                if (!_tokenState.Matches(user, storedToken.SecurityVersion))
                    return Result.Fail("Invalid refresh token.");

                var securityVersion = _tokenState.GetSecurityVersion(user!);
                if (securityVersion is null)
                    return Result.Fail("Invalid refresh token.");

                var now = DateTime.UtcNow;
                var revoked = await _context.RefreshTokens
                    .Where(token => token.Id == storedToken.Id && !token.IsRevoked && token.ExpiresAt > now)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(token => token.IsRevoked, true)
                        .SetProperty(token => token.RevokedAt, now)
                        .SetProperty(token => token.LastUsedAt, now), cancellationToken);

                if (revoked != 1)
                {
                    var concurrentUse = await _context.RefreshTokens
                        .AsNoTracking()
                        .FirstOrDefaultAsync(token => token.Id == storedToken.Id, cancellationToken);

                    if (concurrentUse?.IsRevoked == true)
                    {
                        var invalidation = await _tokenState.InvalidateAsync(user!, cancellationToken);
                        if (invalidation.Succeeded)
                            await _unitOfWork.CommitTransactionAsync(cancellationToken);
                    }

                    return Result.Fail("Invalid refresh token.");
                }

                var accessToken = await GenerateJwtTokenAsync(user!, securityVersion);
                var (newRefreshToken, newRefreshExpiresAt) = GenerateRefreshToken();

                await StoreRefreshTokenAsync(
                    user!.Id,
                    newRefreshToken,
                    newRefreshExpiresAt,
                    securityVersion,
                    storedToken.SessionId,
                    storedToken.Id);

                await _context.RefreshSessions
                    .Where(session => session.Id == storedToken.SessionId && !session.IsRevoked)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(session => session.LastUsedAt, now)
                        .SetProperty(session => session.ExpiresAt, newRefreshExpiresAt), cancellationToken);

                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                return Result.Ok(new AuthenticationResponseVM
                {
                    AccessToken = accessToken,
                    ExpiresInSeconds =
                        int.Parse(_configuration["Jwt:AccessTokenExpirySeconds"] ?? "3600"),
                    RefreshToken = newRefreshToken,
                    RefreshTokenExpiresAt = newRefreshExpiresAt,
                    UserId = user.Id
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "RefreshTokenAsync failed");

                return Result.Fail("Failed to refresh token.");
            }
            finally
            {
                await _unitOfWork.RollbackTransactionAsync(CancellationToken.None);
            }
        }


        public async Task<Result<AuthenticationResponseVM>> VerifyTwoFactorAsync(TwoFactorProviderEnum provider, string code, bool rememberMachine = false)
        {
            try
            {
                await _unitOfWork.BeginTransactionAsync();
                var context = _httpContextAccessor.HttpContext;
                if (context is null)
                    return Result.Fail("Invalid or expired two-factor challenge.");

                var challenge = await Microsoft.AspNetCore.Authentication.AuthenticationHttpContextExtensions.AuthenticateAsync(
                    context,
                    Microsoft.AspNetCore.Identity.IdentityConstants.TwoFactorUserIdScheme);
                var challengeUserId = challenge.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
                var challengeVersion = challenge.Principal?.FindFirstValue(IIdentityTokenStateService.SecurityVersionClaim);
                var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
                if (user is null || string.IsNullOrWhiteSpace(challengeUserId) ||
                    !string.Equals(challengeUserId, user.Id, StringComparison.Ordinal) ||
                    !_tokenState.Matches(user, challengeVersion))
                {
                    await ClearTwoFactorChallengeAsync();
                    return Result.Fail("Invalid or expired two-factor challenge.");
                }

                if (!_tokenState.IsEligible(user) || !user.TwoFactorEnabled)
                {
                    await ClearTwoFactorChallengeAsync();
                    return Result.Fail("Two-factor authentication is not enabled.");
                }

                SignInResult signInResult;

                switch (provider)
                {
                    case TwoFactorProviderEnum.Authenticator:
                        signInResult = await _signInManager
                            .TwoFactorAuthenticatorSignInAsync(
                                code,
                                rememberMachine,
                                rememberClient: false);
                        break;

                    case TwoFactorProviderEnum.RecoveryCode:
                        signInResult = await _signInManager
                            .TwoFactorRecoveryCodeSignInAsync(code);
                        break;

                    default:
                        await ClearTwoFactorChallengeAsync();
                        return Result.Fail("Unsupported two-factor provider.");
                }

                if (signInResult.IsLockedOut)
                {
                    await ClearTwoFactorChallengeAsync();
                    return Result.Fail("Account is locked.");
                }

                if (!signInResult.Succeeded)
                    return Result.Fail("Invalid or expired verification code.");

                await ClearIdentityApplicationCookieAsync();

                // Generate tokens
                var securityVersion = _tokenState.GetSecurityVersion(user);
                if (securityVersion is null)
                    return Result.Fail("Verify 2FA failed.");
                var accessToken = await GenerateJwtTokenAsync(user, securityVersion);
                var (refreshToken, refreshExpiresAt) = GenerateRefreshToken();

                await StoreRefreshTokenAsync(user.Id, refreshToken, refreshExpiresAt, securityVersion);

                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();

                return Result.Ok(new AuthenticationResponseVM
                {
                    AccessToken = accessToken,
                    ExpiresInSeconds = int.Parse(
                        _configuration["Jwt:AccessTokenExpirySeconds"] ?? "3600"),
                    RefreshToken = refreshToken,
                    RefreshTokenExpiresAt = refreshExpiresAt,
                    UserId = user.Id.ToString(),
                    RequiresTwoFactor = false
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Verify 2FA failed");
                return Result.Fail("Verify 2FA failed.");
            }
            finally
            {
                await _unitOfWork.RollbackTransactionAsync();
            }
        }



        public async Task<Result> RevokeRefreshTokensAsync(string userId)
        {
            try
            {
                await _unitOfWork.BeginTransactionAsync();
                var user = await _userManager.FindByIdAsync(userId);
                if (user is null)
                    return Result.Fail("User not found.");

                var invalidation = await _tokenState.InvalidateAsync(user);
                if (!invalidation.Succeeded)
                    return Result.Fail("Failed to revoke refresh tokens.");
                await _unitOfWork.CommitTransactionAsync();

                return Result.Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to revoke refresh tokens for user {UserId}", userId);
                return Result.Fail("Failed to revoke refresh tokens.");
            }
            finally
            {
                await _unitOfWork.RollbackTransactionAsync();
            }
        }

        public async Task<Result> ForgotPasswordAsync(string email, string callbackBaseUrl)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return Result.Fail("Invalid email.");
            }
                

            var user = await _userManager.FindByEmailAsync(email);

            // Public callers receive the same success result for every syntactically valid email.
            if (user == null || !_tokenState.IsEligible(user) || !await _userManager.IsEmailConfirmedAsync(user))
                return Result.Ok();
                

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);

            var encodedToken = WebUtility.UrlEncode(token);

            var resetLink =
                $"{callbackBaseUrl}/reset-password" +
                $"?userId={user.Id}&token={encodedToken}";

            try
            {
                await _emailSender.SendEmailAsync(
                    user.Email!,
                    "Reset your password",
                    $"Click the link to reset your password: {resetLink}"
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Password reset email delivery failed.");
            }

            return Result.Ok();
        }

        public async Task<Result> ResetPasswordAsync(string userId, string token, string newPassword)
        {
            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(newPassword))
            {
                return Result.Fail("Invalid reset request.");
            }

            var user = await _userManager.FindByIdAsync(userId);

            // Prevent user enumeration
            if (user == null || !await _userManager.IsEmailConfirmedAsync(user))
            {
                return Result.Fail("Invalid reset request.");
            }

            var decodedToken = WebUtility.UrlDecode(token);

            var identityResult = await _userManager.ResetPasswordAsync(user, decodedToken, newPassword);

            if (!identityResult.Succeeded)
            {
                var errors = identityResult.Errors
                    .Select(e =>
                        new Error(e.Description)
                        .WithMetadata("Field", "password"))
                    .ToList();

                return Result.Fail(errors);
            }

            // Optional but recommended: invalidate sessions
            var invalidation = await _tokenState.InvalidateAsync(user);
            if (!invalidation.Succeeded)
                return Result.Fail("Failed to invalidate sessions.");

            return Result.Ok();
        }



        #region Helpers

        private async Task<Result> IssueTwoFactorChallengeAsync(ApplicationUser user)
        {
            var context = _httpContextAccessor.HttpContext;
            var securityVersion = _tokenState.GetSecurityVersion(user);
            if (context is null || securityVersion is null)
                return Result.Fail("Unable to create two-factor challenge.");

            var identity = new ClaimsIdentity(Microsoft.AspNetCore.Identity.IdentityConstants.TwoFactorUserIdScheme);
            // SignInManager.GetTwoFactorAuthenticationUserAsync reads the user ID
            // from NameIdentifier when resolving the protected 2FA challenge.
            identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, user.Id));
            identity.AddClaim(new Claim(IIdentityTokenStateService.SecurityVersionClaim, securityVersion));

            await Microsoft.AspNetCore.Authentication.AuthenticationHttpContextExtensions.SignInAsync(
                context,
                Microsoft.AspNetCore.Identity.IdentityConstants.TwoFactorUserIdScheme,
                new ClaimsPrincipal(identity),
                new Microsoft.AspNetCore.Authentication.AuthenticationProperties
                {
                    IsPersistent = false,
                    AllowRefresh = false,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(5)
                });

            return Result.Ok();
        }

        private Task ClearTwoFactorChallengeAsync()
        {
            var context = _httpContextAccessor.HttpContext;
            return context is null
                ? Task.CompletedTask
                : Microsoft.AspNetCore.Authentication.AuthenticationHttpContextExtensions.SignOutAsync(
                    context,
                    Microsoft.AspNetCore.Identity.IdentityConstants.TwoFactorUserIdScheme);
        }

        private Task ClearIdentityApplicationCookieAsync()
        {
            var context = _httpContextAccessor.HttpContext;
            return context is null
                ? Task.CompletedTask
                : Microsoft.AspNetCore.Authentication.AuthenticationHttpContextExtensions.SignOutAsync(
                    context,
                    Microsoft.AspNetCore.Identity.IdentityConstants.ApplicationScheme);
        }

        private async Task<string> GenerateJwtTokenAsync(ApplicationUser user, string securityVersion)
        {
            var claims = new List<Claim>
            {
                // REQUIRED for ASP.NET Identity & Authorize()
                new Claim(ClaimTypes.NameIdentifier, user.Id),

                // Optional
                new Claim(ClaimTypes.Name, user.UserName ?? string.Empty),
                new Claim(ClaimTypes.Email, user.Email ?? string.Empty),

                // JWT standard claims
                new Claim(JwtRegisteredClaimNames.Sub, user.Id),
                new Claim(JwtRegisteredClaimNames.UniqueName, user.UserName ?? string.Empty),
                new Claim(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            claims.Add(new Claim(IIdentityTokenStateService.SecurityVersionClaim, securityVersion));
            claims.Add(new Claim(IIdentityTokenStateService.TokenUseClaim, IIdentityTokenStateService.AccessTokenUse));

            // Add custom user claims,if any
            var userClaims = await _userManager.GetClaimsAsync(user);
            claims.AddRange(userClaims);

            // ADD ROLES
            var roles = await _userManager.GetRolesAsync(user);
            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:SigningKey"] ?? throw new InvalidOperationException("Jwt:SigningKey not configured")));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expiry = DateTime.UtcNow.AddSeconds(int.Parse(_configuration["Jwt:AccessTokenExpirySeconds"] ?? "3600"));

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: expiry,
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }


        private (string token, DateTime expiresAt) GenerateRefreshToken()
        {
            var random = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(random);
            var token = Convert.ToBase64String(random);
            var expires = DateTime.UtcNow.AddDays(int.Parse(_configuration["Jwt:RefreshTokenExpiryDays"] ?? "30"));
            return (token, expires);
        }

        private async Task StoreRefreshTokenAsync(
            string userId,
            string token,
            DateTime expiresAt,
            string securityVersion,
            int? sessionId = null,
            int? parentTokenId = null)
        {
            var now = DateTime.UtcNow;
            if (sessionId is null)
            {
                var session = new RefreshSession
                {
                    PublicId = Guid.NewGuid(),
                    UserId = userId,
                    CreatedAt = now,
                    LastUsedAt = now,
                    ExpiresAt = expiresAt,
                    IsRevoked = false
                };

                await _context.RefreshSessions.AddAsync(session);
                await _context.SaveChangesAsync();
                sessionId = session.Id;
            }

            var refreshToken = new RefreshToken
            {
                UserId = userId,
                SessionId = sessionId.Value,
                ParentTokenId = parentTokenId,
                TokenHash = RefreshTokenHashing.Compute(token),
                SecurityVersion = securityVersion,
                ExpiresAt = expiresAt,
                CreatedAt = now,
                LastUsedAt = now,
                IsRevoked = false,
                RevokedAt = null
            };

            await _context.RefreshTokens.AddAsync(refreshToken);
        }

        private async Task RevokeSessionAsync(int sessionId, DateTime revokedAt)
        {
            await _context.RefreshTokens
                .Where(token => token.SessionId == sessionId && !token.IsRevoked)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(token => token.IsRevoked, true)
                    .SetProperty(token => token.RevokedAt, revokedAt));

            await _context.RefreshSessions
                .Where(session => session.Id == sessionId && !session.IsRevoked)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(session => session.IsRevoked, true)
                    .SetProperty(session => session.RevokedAt, revokedAt)
                    .SetProperty(session => session.LastUsedAt, revokedAt));
        }


        #endregion
    }
}
