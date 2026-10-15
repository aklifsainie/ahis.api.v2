using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ahis.template.identity.Models.DTOs
{
    public class AccountMeDto
    {
        public string UserId { get; init; } = default!;
        public string Email { get; init; } = default!;
        public string Username { get; init; } = default!;
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string PhoneNumber { get; set; }
        public bool EmailConfirmed { get; init; }
        public bool TwoFactorEnabled { get; init; }
    }

    public sealed class ActiveSessionDto
    {
        public Guid SessionId { get; init; }
        public DateTime CreatedAt { get; init; }
        public DateTime LastUsedAt { get; init; }
        public DateTime ExpiresAt { get; init; }
        public bool IsCurrent { get; init; }
    }

    public sealed class AdminUserSecurityStateDto
    {
        public string UserId { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public bool IsDeleted { get; set; }
        public bool IsLockedOut { get; set; }
        public bool EmailConfirmed { get; set; }
        public bool PhoneConfirmed { get; set; }
        public bool PasswordPresent { get; set; }
        public bool TwoFactorEnabled { get; set; }
        public bool AuthenticatorConfigured { get; set; }
        public int RemainingRecoveryCodeCount { get; set; }
        public int ActiveSessionCount { get; set; }
    }

    public sealed class SecuritySummaryDto
    {
        public bool EmailConfirmed { get; init; }
        public bool PhoneConfirmed { get; init; }
        public bool PasswordPresent { get; init; }
        public bool TwoFactorEnabled { get; init; }
        public bool AuthenticatorConfigured { get; init; }
        public int RemainingRecoveryCodeCount { get; init; }
        public int ActiveSessionCount { get; init; }
    }
}
