namespace ahis.template.identity.Models.DTOs;

public sealed record PasskeyOptionsDto(string CeremonyId, System.Text.Json.JsonElement Options);
public sealed record PasskeyCredentialDto(Guid Id, string DisplayName, DateTime CreatedAtUtc, DateTime? LastUsedAtUtc);
public sealed record PasskeyRegistrationDto(string DisplayName, string StepUpProof, string? Password, string? RecoveryCode, string AttestationResponseJson, string CeremonyId);
public sealed record PasskeyAssertionDto(string AssertionResponseJson, string CeremonyId);
public sealed record PasskeyRemovalEvidence(string? Password, string? RecoveryCode);
