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

        public AuthenticationService(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IdentityContext context,
            IEmailSender emailSender,
            IdentityUnitOfWork unitOfWork,
            IConfiguration configuration,
            ILogger<AuthenticationService> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
            _emailSender = emailSender;
            _unitOfWork = unitOfWork;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<Result<AuthenticationResponseVM>> CheckAccountStateByEmailAsync(string userNameOrEmail)
        {
            if (string.IsNullOrWhiteSpace(userNameOrEmail))
                return Result.Fail<AuthenticationResponseVM>("Invalid email.");

            var user = await _userManager.FindByNameAsync(userNameOrEmail) ?? await _userManager.FindByEmailAsync(userNameOrEmail);
            if (user == null)
            {
                // Prevent user enumeration attacks
                return Result.Ok(new AuthenticationResponseVM
                {
                    IsEmailConfirmed = false,
                    IsPasswordCreated = false,
                    RequiresTwoFactor = false
                });
            }

            var isEmailConfirmed = await _userManager.IsEmailConfirmedAsync(user);
            var hasPassword = await _userManager.HasPasswordAsync(user);
            var isTwoFactorEnabled = await _userManager.GetTwoFactorEnabledAsync(user);

            return Result.Ok(new AuthenticationResponseVM
            {
                UserId = user.Id.ToString(),
                IsEmailConfirmed = isEmailConfirmed,
                IsPasswordCreated = hasPassword,
                RequiresTwoFactor = isTwoFactorEnabled
            });
        }


        public async Task<Result<AuthenticationResponseVM>> LoginAsync(string userNameOrEmail, string password, bool rememberMe = false)
        {
            
            try
            {
                // Check for user existance
                var user = await _userManager.FindByNameAsync(userNameOrEmail) ?? await _userManager.FindByEmailAsync(userNameOrEmail);
                if (user == null)
                    return Result.Fail<AuthenticationResponseVM>("Invalid credentials.");

                if (!user.IsActive || user.IsDeleted)
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
                        return Result.Ok(new AuthenticationResponseVM { RequiresTwoFactor = true, UserId = user.Id.ToString() });

                    return Result.Fail<AuthenticationResponseVM>("Invalid credentials.");
                }

                // create tokens
                var accessToken = await GenerateJwtTokenAsync(user);
                var (refreshToken, refreshExpiresAt) = GenerateRefreshToken();

                // persist refresh token

                await _unitOfWork.BeginTransactionAsync();

                await StoreRefreshTokenAsync(user.Id, refreshToken, refreshExpiresAt);

                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();

                var response = new AuthenticationResponseVM
                {
                    AccessToken = accessToken,
                    ExpiresInSeconds = int.Parse(_configuration["Jwt:AccessTokenExpirySeconds"] ?? "3600"),
                    RefreshToken = refreshToken,
                    RefreshTokenExpiresAt = refreshExpiresAt,
                    UserId = user.Id.ToString(),
                    RequiresTwoFactor = false
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
            var token = await _context.RefreshTokens
                .FirstOrDefaultAsync(x =>
                    x.Token == refreshToken &&
                    !x.IsRevoked &&
                    x.ExpiresAt > DateTime.UtcNow);

            if (token == null)
                return;

            token.IsRevoked = true;
            token.RevokedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            await _signInManager.SignOutAsync();

        }

        public async Task<Result<AuthenticationResponseVM>> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken)
        {

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var storedToken = await _context.RefreshTokens
                    .AsTracking()
                    .FirstOrDefaultAsync(x => x.Token == refreshToken, cancellationToken);

                if (storedToken == null)
                    return Result.Fail("Invalid refresh token.");

                if (storedToken.IsRevoked)
                {
                    _logger.LogWarning(
                        "Refresh token reuse detected for user {UserId}",
                        storedToken.UserId);

                    await RevokeRefreshTokensAsync(storedToken.UserId);
                    await _unitOfWork.CommitTransactionAsync();

                    return Result.Fail(
                        "Refresh token reuse detected. All sessions revoked.");
                }

                if (storedToken.ExpiresAt <= DateTime.UtcNow)
                    return Result.Fail("Expired refresh token.");

                var user = await _userManager.FindByIdAsync(storedToken.UserId);
                if (user == null)
                    return Result.Fail("User not found.");

                // Rotate token
                storedToken.IsRevoked = true;
                storedToken.RevokedAt = DateTime.UtcNow;

                var accessToken = await GenerateJwtTokenAsync(user);
                var (newRefreshToken, newRefreshExpiresAt) = GenerateRefreshToken();

                await StoreRefreshTokenAsync(
                    user.Id,
                    newRefreshToken,
                    newRefreshExpiresAt);

                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();

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
                await _unitOfWork.RollbackTransactionAsync();

                _logger.LogError(ex, "RefreshTokenAsync failed");

                return Result.Fail("Failed to refresh token.");
            }
        }


        public async Task<Result<AuthenticationResponseVM>> VerifyTwoFactorAsync(string userId, TwoFactorProviderEnum provider, string code, bool rememberMachine = false)
        {
            await _unitOfWork.BeginTransactionAsync();

            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                    return Result.Fail("User not found.");

                if (!user.TwoFactorEnabled)
                    return Result.Fail("Two-factor authentication is not enabled.");

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
                        return Result.Fail("Unsupported two-factor provider.");
                }

                if (signInResult.IsLockedOut)
                    return Result.Fail("Account is locked.");

                if (!signInResult.Succeeded)
                    return Result.Fail("Invalid or expired verification code.");

                // If recovery code is used → force regeneration later
                if (provider == TwoFactorProviderEnum.RecoveryCode)
                {
                    // optional: mark flag, audit log, etc.
                }

                // Generate tokens
                var accessToken = await GenerateJwtTokenAsync(user);
                var (refreshToken, refreshExpiresAt) = GenerateRefreshToken();

                await StoreRefreshTokenAsync(user.Id, refreshToken, refreshExpiresAt);

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
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(ex, "Verify 2FA failed");
                return Result.Fail("Verify 2FA failed.");
            }
        }



        public async Task<Result> RevokeRefreshTokensAsync(string userId)
        {
            await _unitOfWork.BeginTransactionAsync();

            try
            {
                var tokens = await _context.RefreshTokens
                    .Where(x => x.UserId == userId && !x.IsRevoked)
                    .ToListAsync();

                if (!tokens.Any())
                    return Result.Ok(); // idempotent behavior

                foreach (var token in tokens)
                {
                    token.IsRevoked = true;
                }

                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();

                _logger.LogInformation(
                    "Revoked {Count} refresh tokens for user {UserId}",
                    tokens.Count,
                    userId);

                return Result.Ok();
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(ex, "Failed to revoke refresh tokens for user {UserId}", userId);
                return Result.Fail("Failed to revoke refresh tokens.");
            }
        }

        public async Task<Result> ForgotPasswordAsync(string email, string callbackBaseUrl)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return Result.Fail("Invalid email.");
            }
                

            var user = await _userManager.FindByEmailAsync(email);

            // Prevent user enumeration
            if (user == null || !await _userManager.IsEmailConfirmedAsync(user))
            {
                return Result.Fail("Invalid email.");
            }
                

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);

            var encodedToken = WebUtility.UrlEncode(token);

            var resetLink =
                $"{callbackBaseUrl}/reset-password" +
                $"?userId={user.Id}&token={encodedToken}";

            await _emailSender.SendEmailAsync(
                user.Email!,
                "Reset your password",
                $"Click the link to reset your password: {resetLink}"
            );

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
            await _userManager.UpdateSecurityStampAsync(user);

            return Result.Ok();
        }



        #region Helpers

        private async Task<string> GenerateJwtTokenAsync(ApplicationUser user)
        {
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.UniqueName, user.UserName ?? ""),
                new Claim(JwtRegisteredClaimNames.Email, user.Email ?? "")
            };

            var userClaims = await _userManager.GetClaimsAsync(user);
            claims.AddRange(userClaims);

            var roles = await _userManager.GetRolesAsync(user);
            foreach (var role in roles)
                claims.Add(new Claim(ClaimTypes.Role, role));

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:SigningKey"] ?? throw new InvalidOperationException("Jwt:SigningKey not configured")));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expiry = DateTime.UtcNow.AddSeconds(int.Parse(_configuration["Jwt:AccessTokenExpirySeconds"] ?? "3600"));

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"] ?? "",
                audience: _configuration["Jwt:Audience"] ?? "",
                claims: claims,
                expires: expiry,
                signingCredentials: creds);

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

        private async Task StoreRefreshTokenAsync(string userId, string token, DateTime expiresAt)
        {
            var refreshToken = new RefreshToken
            {
                UserId = userId,
                Token = token,
                ExpiresAt = expiresAt,
                CreatedAt = DateTime.UtcNow,
                IsRevoked = false,
                RevokedAt = null
            };

            await _context.RefreshTokens.AddAsync(refreshToken);
        }


        #endregion


        public JwtPayload DecodeToken(string token)
        {
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);
            return jwtToken.Payload;
        }

        public IEnumerable<Claim> GetClaims(string token)
        {
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);
            return jwtToken.Claims;
        }
    }
}
