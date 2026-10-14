namespace ahis.template.identity.Models.Entities;

/// <summary>Immutable singleton completion marker for the CLI-only first Superadmin bootstrap.</summary>
public sealed class FirstSuperadminBootstrap
{
    public byte Id { get; set; } = 1;
    public required string TargetUserId { get; set; }
    public DateTime CompletedAtUtc { get; set; }
}
