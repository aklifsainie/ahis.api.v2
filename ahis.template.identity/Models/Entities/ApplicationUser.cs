using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace ahis.template.identity.Models.Entities
{
    public class ApplicationUser : IdentityUser
    {
        [MaxLength(100)]
        public string? FirstName { get; set; }

        [MaxLength(100)]
        public string? LastName { get; set; }

        public string FullName => string.Join(' ', new[] { FirstName, LastName }.Where(s => !string.IsNullOrWhiteSpace(s)));

        /// <summary>
        /// The user's date of birth.
        /// </summary>
        public DateTime? DateOfBirth { get; set; }

        /// <summary>
        /// Whether the user has completed initial account configuration (e.g. profile setup).
        /// </summary>
        public bool IsAccountConfigured { get; set; } = false;

        /// <summary>
        /// The date of the user verified their email address.
        /// </summary>
        public DateTime? EmailVerifiedAt { get; set; }


        // Two-factor authentication (TOTP) support for authenticator apps (e.g. Google Authenticator)

        /// <summary>
        /// The base32 secret key used to generate TOTP codes for authenticator apps.
        /// Stored as a protected string; keep it encrypted at rest if possible.
        /// </summary>
        [MaxLength(500)]
        public string? AuthenticatorKey { get; set; }

        /// <summary>
        /// Provisioning URI (otpauth://...) for QR code generation. Optional and derived from AuthenticatorKey.
        /// </summary>
        [MaxLength(1000)]
        public string? AuthenticatorUri { get; set; }

        /// <summary>
        /// Serialized recovery codes (e.g. JSON array or encrypted blob). Use a secure storage/encryption.
        /// </summary>
        [MaxLength(4000)]
        public string? RecoveryCodes { get; set; }

        /// <summary>
        /// Timestamp when two-factor authentication was enabled.
        /// </summary>
        public DateTime? TwoFactorEnabledAt { get; set; }

        /// <summary>
        /// Whether the user has completed authenticator app setup (has a key configured).
        /// Note: IdentityUser.TwoFactorEnabled indicates if 2FA is enabled overall.
        /// </summary>
        public bool IsAuthenticatorConfigured => TwoFactorEnabled && !string.IsNullOrWhiteSpace(AuthenticatorKey);

        // Soft delete / activation

        /// <summary>
        /// Soft-delete flag. When true the user is considered deleted and should be excluded from queries.
        /// </summary>
        public bool IsDeleted { get; set; } = false;

        /// <summary>
        /// Active flag. When false the user cannot authenticate even if credentials are valid.
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// When the user was deactivated/soft-deleted.
        /// </summary>
        public DateTime? DeletedAt { get; set; }

        /// <summary>
        /// Audit: when the user record was created.
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Audit: when the user record was last updated.
        /// </summary>
        public DateTime? UpdatedAt { get; set; }

        /// <summary>
        /// Convenience property that indicates whether the user is currently locked out according to Identity lockout rules.
        /// </summary>
        public bool IsLockedOut => LockoutEnabled && LockoutEnd.HasValue && LockoutEnd.Value > DateTimeOffset.UtcNow;

    }
}
