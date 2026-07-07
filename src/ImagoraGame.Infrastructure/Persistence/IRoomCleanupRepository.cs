namespace ImagoraGame.Infrastructure.Persistence;

public interface IRoomCleanupRepository
{
    Task<int> DeleteInactiveRoomsAsync(
        DateTimeOffset cutoffUtc,
        CancellationToken cancellationToken = default);
}
