namespace ahis.template.identity.Models.Entities;

public sealed class PasskeyCredential
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string CredentialId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public byte[] PublicKey { get; set; } = Array.Empty<byte>();
    public long SignCount { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? LastUsedAtUtc { get; set; }
    public bool IsBackupEligible { get; set; }
    public bool IsBackedUp { get; set; }
}
