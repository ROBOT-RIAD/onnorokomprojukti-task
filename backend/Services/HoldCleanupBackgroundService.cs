using SchoolAdmission.Api.Services;

namespace SchoolAdmission.Api.Services;

public class HoldCleanupBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<HoldCleanupBackgroundService> _logger;

    public HoldCleanupBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<HoldCleanupBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var admissionService = scope.ServiceProvider.GetRequiredService<IAdmissionService>();
                await admissionService.CleanupExpiredHoldsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during background hold cleanup.");
            }

            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }
}
