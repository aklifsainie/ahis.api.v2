namespace ahis.template.domain.Security;

public sealed record HumanPermissionDefinition(
    string Code,
    string Label,
    string Description,
    string Category);

public static class HumanPermissionCatalog
{
    public const string ClaimType = "human_permission";
    public const string CountryRead = "country.read";
    public const string CountryWrite = "country.write";

    public static readonly IReadOnlyList<HumanPermissionDefinition> Definitions =
    [
        new(CountryRead, "View countries", "View active country reference data.", "Country"),
        new(CountryWrite, "Manage countries", "Create, update, and delete country reference data.", "Country")
    ];

    public static readonly IReadOnlySet<string> All = Definitions
        .Select(definition => definition.Code)
        .ToHashSet(StringComparer.Ordinal);
}
