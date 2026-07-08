namespace ImaginArte.Client.Blazor.Rooms;

public interface IBrowserStorage
{
    ValueTask<string?> GetItemAsync(string key);

    ValueTask SetItemAsync(string key, string value);

    ValueTask RemoveItemAsync(string key);
}
