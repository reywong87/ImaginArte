using ImaginArte.Application.Rooms;
using ImaginArte.Domain;

namespace ImaginArte.Infrastructure.Persistence;

public sealed class InMemoryRoomRepository : IRoomRepository
{
    private readonly Lock _gate = new();
    private readonly Dictionary<string, Room> _rooms = new(StringComparer.Ordinal);

    public Task<Room?> GetByCodeAsync(RoomCode code, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(code);
        cancellationToken.ThrowIfCancellationRequested();

        lock (_gate)
        {
            _rooms.TryGetValue(code.Value, out var room);
            return Task.FromResult(room);
        }
    }

    public Task AddAsync(Room room, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(room);
        cancellationToken.ThrowIfCancellationRequested();

        lock (_gate)
        {
            if (!_rooms.TryAdd(room.Code.Value, room))
            {
                throw new RoomAlreadyExistsException(room.Code.Value);
            }
        }

        return Task.CompletedTask;
    }

    public Task SaveAsync(Room room, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(room);
        cancellationToken.ThrowIfCancellationRequested();

        lock (_gate)
        {
            if (!_rooms.ContainsKey(room.Code.Value))
            {
                throw new RoomNotFoundException(room.Code.Value);
            }

            _rooms[room.Code.Value] = room;
        }

        return Task.CompletedTask;
    }
}
