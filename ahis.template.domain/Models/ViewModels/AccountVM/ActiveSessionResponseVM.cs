namespace ahis.template.domain.Models.ViewModels.AccountVM;

public sealed class ActiveSessionResponseVM
{
    public Guid SessionId { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime LastUsedAt { get; init; }
    public DateTime ExpiresAt { get; init; }
    public bool IsCurrent { get; init; }
}

public sealed class SecuritySummaryResponseVM
{
    public bool EmailConfirmed { get; init; }
    public bool PhoneConfirmed { get; init; }
    public bool PasswordPresent { get; init; }
    public bool TwoFactorEnabled { get; init; }
    public bool AuthenticatorConfigured { get; init; }
    public int RemainingRecoveryCodeCount { get; init; }
    public int ActiveSessionCount { get; init; }
}
