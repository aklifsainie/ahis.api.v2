namespace ahis.template.identity.Interfaces;

public interface IEffectiveSuperadminLock
{
    Task<bool> AcquireAsync(CancellationToken cancellationToken);
}
