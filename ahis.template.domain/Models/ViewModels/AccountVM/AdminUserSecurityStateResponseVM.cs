namespace ahis.template.domain.Models.ViewModels.AccountVM;

public sealed class AdminUserSecurityStateResponseVM
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
