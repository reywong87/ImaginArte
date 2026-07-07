using Microsoft.Extensions.Options;

public sealed class RoomCleanupBackgroundService : BackgroundService
{
    private readonly RoomCleanupRunner _runner;
    private readonly IOptions<RoomCleanupOptions> _options;
    private readonly ILogger<RoomCleanupBackgroundService> _logger;

    public RoomCleanupBackgroundService(
        RoomCleanupRunner runner,
        IOptions<RoomCleanupOptions> options,
        ILogger<RoomCleanupBackgroundService> logger)
    {
        _runner = runner;
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await RunCleanupAsync(stoppingToken);

        using var timer = new PeriodicTimer(
            TimeSpan.FromMinutes(_options.Value.IntervalMinutes));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await RunCleanupAsync(stoppingToken);
        }
    }

    private async Task RunCleanupAsync(CancellationToken stoppingToken)
    {
        try
        {
            await _runner.RunOnceAsync(stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "SQLite room cleanup failed.");
        }
    }
}
