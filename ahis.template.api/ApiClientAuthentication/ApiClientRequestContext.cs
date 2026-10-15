namespace ahis.template.api.ApiClientAuthentication;

public sealed record ApiClientRequestContext(Guid ClientDatabaseId, Guid KeyId, int RateLimitPerMinute);
