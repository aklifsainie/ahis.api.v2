namespace ahis.template.domain.Security;

public static class HumanPermissionCatalog
{
    public const string ClaimType = "human_permission";
    public const string CountryRead = "country.read";
    public const string CountryWrite = "country.write";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        CountryRead,
        CountryWrite
    };
}
