namespace ahis.template.identity.Interfaces;

public interface IIdentitySessionCleanupService
{
    Task CleanupAsync(CancellationToken cancellationToken = default);
}
