namespace ahis.template.identity.Services;

public sealed class IdentityRoleBootstrapOptions
{
    public const string SectionName = "Identity:Bootstrap";

    public string? SuperadminEmail { get; init; }
}
