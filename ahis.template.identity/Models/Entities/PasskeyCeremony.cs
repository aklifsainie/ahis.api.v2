namespace ahis.template.identity.Models.Entities;

public enum PasskeyCeremonyPurpose { Registration = 1, Authentication = 2 }

public sealed class PasskeyCeremony
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public byte[] ChallengeReferenceHash { get; set; } = Array.Empty<byte>();
    public byte[] ChallengeHash { get; set; } = Array.Empty<byte>();
    public PasskeyCeremonyPurpose Purpose { get; set; }
    public string? UserId { get; set; }
    public string? SecurityVersion { get; set; }
    public string RpConfigurationId { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? ConsumedAtUtc { get; set; }
    public string RateLimitKeyHash { get; set; } = string.Empty;
    public string? UserRateLimitKeyHash { get; set; }
    public string OptionsJson { get; set; } = string.Empty;
    public byte[] ProtectedChallenge { get; set; } = Array.Empty<byte>();
}
