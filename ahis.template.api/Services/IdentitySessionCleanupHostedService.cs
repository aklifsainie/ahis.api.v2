using ahis.template.identity.Interfaces;

namespace ahis.template.api.Services;

public sealed class IdentitySessionCleanupHostedService : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromDays(1);
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<IdentitySessionCleanupHostedService> _logger;

    public IdentitySessionCleanupHostedService(
        IServiceScopeFactory scopeFactory,
        ILogger<IdentitySessionCleanupHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var cleanup = scope.ServiceProvider.GetRequiredService<IIdentitySessionCleanupService>();
                await cleanup.CleanupAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Identity session cleanup failed.");
            }

            await Task.Delay(Interval, stoppingToken);
        }
    }
}
