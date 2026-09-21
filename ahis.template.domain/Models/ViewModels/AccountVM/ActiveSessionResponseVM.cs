namespace ahis.template.domain.Models.ViewModels.AccountVM;

public sealed class ActiveSessionResponseVM
{
    public Guid SessionId { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime LastUsedAt { get; init; }
    public DateTime ExpiresAt { get; init; }
    public bool IsCurrent { get; init; }
}
