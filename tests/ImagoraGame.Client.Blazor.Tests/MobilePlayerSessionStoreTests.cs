using ImagoraGame.Client.Blazor.Rooms;

namespace ImagoraGame.Client.Blazor.Tests;

public sealed class MobilePlayerSessionStoreTests
{
    [Fact]
    public async Task SaveAsync_ThenLoadAsync_StoresSessionPerRoom()
    {
        var storage = new FakeBrowserStorage();
        var store = new MobilePlayerSessionStore(storage);
        var playerId = Guid.NewGuid();

        await store.SaveAsync(new MobilePlayerSession("abc123", playerId, " Alice "));
        var session = await store.LoadAsync("ABC123");

        Assert.NotNull(session);
        Assert.Equal("ABC123", session.RoomCode);
        Assert.Equal(playerId, session.PlayerId);
        Assert.Equal("Alice", session.PlayerName);
    }

    [Fact]
    public async Task SaveAsync_NormalizesRoomCodeInStorageKey()
    {
        var storage = new FakeBrowserStorage();
        var store = new MobilePlayerSessionStore(storage);

        await store.SaveAsync(new MobilePlayerSession(" abc123 ", Guid.NewGuid(), "Alice"));

        Assert.Contains("imagora.rooms.ABC123.player", storage.Items.Keys);
    }

    [Fact]
    public async Task LoadAsync_WhenStoredJsonIsInvalid_IgnoresSession()
    {
        var storage = new FakeBrowserStorage
        {
            Items =
            {
                ["imagora.rooms.ABC123.player"] = "{not valid json"
            }
        };
        var store = new MobilePlayerSessionStore(storage);

        var session = await store.LoadAsync("ABC123");

        Assert.Null(session);
    }

    [Fact]
    public async Task LoadAsync_WhenStoredPlayerIdIsInvalid_IgnoresSession()
    {
        var storage = new FakeBrowserStorage
        {
            Items =
            {
                ["imagora.rooms.ABC123.player"] =
                    """{"roomCode":"ABC123","playerId":"not-a-guid","playerName":"Alice"}"""
            }
        };
        var store = new MobilePlayerSessionStore(storage);

        var session = await store.LoadAsync("ABC123");

        Assert.Null(session);
    }

    private sealed class FakeBrowserStorage : IBrowserStorage
    {
        public Dictionary<string, string> Items { get; } = [];

        public ValueTask<string?> GetItemAsync(string key)
        {
            Items.TryGetValue(key, out var value);
            return ValueTask.FromResult<string?>(value);
        }

        public ValueTask SetItemAsync(string key, string value)
        {
            Items[key] = value;
            return ValueTask.CompletedTask;
        }

        public ValueTask RemoveItemAsync(string key)
        {
            Items.Remove(key);
            return ValueTask.CompletedTask;
        }
    }
}
