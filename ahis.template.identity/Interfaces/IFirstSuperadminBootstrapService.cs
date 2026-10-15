namespace ahis.template.identity.Interfaces;

public interface IFirstSuperadminBootstrapService
{
    Task<bool> BootstrapAsync(string userId, CancellationToken cancellationToken = default);
}
