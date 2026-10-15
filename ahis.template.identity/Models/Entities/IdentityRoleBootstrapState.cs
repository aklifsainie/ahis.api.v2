namespace ahis.template.identity.Models.Entities;

/// <summary>Durable record that the one-time first Superadmin bootstrap completed.</summary>
public sealed class IdentityRoleBootstrapState
{
    public const int SingletonId = 1;

    public int Id { get; set; } = SingletonId;
    public required string TargetUserId { get; set; }
    public DateTime CompletedAtUtc { get; set; }
}
