using ImagoraGame.Domain;

namespace ImagoraGame.Application.Rooms;

public interface IRoomRepository
{
    Task<Room?> GetByCodeAsync(RoomCode code, CancellationToken cancellationToken = default);

    Task AddAsync(Room room, CancellationToken cancellationToken = default);

    Task SaveAsync(Room room, CancellationToken cancellationToken = default);
}
