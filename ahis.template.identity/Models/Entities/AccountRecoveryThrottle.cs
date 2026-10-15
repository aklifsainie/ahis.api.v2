using System.ComponentModel.DataAnnotations;

namespace ahis.template.identity.Models.Entities;

public sealed class AccountRecoveryThrottle
{
    public int Id { get; set; }

    [Required, MaxLength(64)]
    public string SubjectHash { get; set; } = null!;

    public DateTime StartWindowStartedAt { get; set; }
    public int StartCount { get; set; }
    public DateTime CompletionWindowStartedAt { get; set; }
    public int FailedCompletionCount { get; set; }
    public DateTime UpdatedAt { get; set; }
}
