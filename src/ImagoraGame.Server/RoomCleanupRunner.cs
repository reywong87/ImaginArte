using ImagoraGame.Infrastructure.Persistence;
using Microsoft.Extensions.Options;

public sealed class RoomCleanupRunner
{
    private readonly IRoomCleanupRepository _rooms;
    private readonly IOptions<RoomCleanupOptions> _options;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<RoomCleanupRunner> _logger;

    public RoomCleanupRunner(
        IRoomCleanupRepository rooms,
        IOptions<RoomCleanupOptions> options,
        TimeProvider timeProvider,
        ILogger<RoomCleanupRunner> logger)
    {
        _rooms = rooms;
        _options = options;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<int> RunOnceAsync(CancellationToken cancellationToken = default)
    {
        var ttl = TimeSpan.FromHours(_options.Value.InactiveRoomTtlHours);
        var cutoffUtc = _timeProvider.GetUtcNow().Subtract(ttl);
        var deletedRooms = await _rooms.DeleteInactiveRoomsAsync(cutoffUtc, cancellationToken);

        _logger.LogInformation(
            "Deleted {DeletedRooms} inactive SQLite rooms older than {CutoffUtc}.",
            deletedRooms,
            cutoffUtc);

        return deletedRooms;
    }
}
