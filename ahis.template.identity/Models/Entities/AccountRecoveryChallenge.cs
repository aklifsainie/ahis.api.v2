using System.ComponentModel.DataAnnotations;

namespace ahis.template.identity.Models.Entities;

public sealed class AccountRecoveryChallenge
{
    public int Id { get; set; }

    [Required, MaxLength(450)]
    public string UserId { get; set; } = null!;

    [Required]
    public byte[] ChallengeHash { get; set; } = null!;

    [Required, MaxLength(64)]
    public string SecurityVersion { get; set; } = null!;

    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? ConsumedAt { get; set; }
}
