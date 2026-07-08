using Microsoft.JSInterop;

namespace ImaginArte.Client.Blazor.Rooms;

public sealed class LocalStorageBrowserStorage(IJSRuntime jsRuntime) : IBrowserStorage
{
    public ValueTask<string?> GetItemAsync(string key)
    {
        return jsRuntime.InvokeAsync<string?>("localStorage.getItem", key);
    }

    public ValueTask SetItemAsync(string key, string value)
    {
        return jsRuntime.InvokeVoidAsync("localStorage.setItem", key, value);
    }

    public ValueTask RemoveItemAsync(string key)
    {
        return jsRuntime.InvokeVoidAsync("localStorage.removeItem", key);
    }
}
