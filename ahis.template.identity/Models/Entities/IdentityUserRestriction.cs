namespace ahis.template.identity.Models.Entities;

public enum IdentityRestrictionCategory
{
    OrdinaryLockout = 1,
    AdministrativeSecurityHold = 2,
    LegacyUnclassified = 3
}

public enum LegacyRestrictionDecision
{
    OrdinaryLockout = 1,
    AdministrativeSecurityHold = 2,
    NotRestricted = 3
}

/// <summary>Immutable provenance for a restriction; closing preserves the original record.</summary>
public sealed class IdentityUserRestriction
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string UserId { get; set; }
    public IdentityRestrictionCategory Category { get; set; }
    public DateTime StartedAtUtc { get; set; }
    public DateTime? ExpiresAtUtc { get; set; }
    public DateTime? EndedAtUtc { get; set; }
    public string Origin { get; set; } = string.Empty;
    public string? PlacedByUserId { get; set; }
    public string? EndedByUserId { get; set; }
    public LegacyRestrictionDecision? ReviewDecision { get; set; }
    public DateTime? ReviewedAtUtc { get; set; }
    public string? ReviewedByUserId { get; set; }
    public string? EvidenceReference { get; set; }
    public string? InternalReasonCode { get; set; }
}
