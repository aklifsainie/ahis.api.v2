namespace ahis.template.identity.Interfaces;

public interface IIdentityRoleBootstrapService
{
    Task InitializeAsync(CancellationToken cancellationToken = default);
}
