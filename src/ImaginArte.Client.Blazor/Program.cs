using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using ImaginArte.Client.Blazor;
using ImaginArte.Client.Blazor.Rooms;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var apiBaseUrl = builder.Configuration["ApiBaseUrl"];
if (string.IsNullOrWhiteSpace(apiBaseUrl))
{
    throw new InvalidOperationException("Configuration value 'ApiBaseUrl' is required.");
}

var apiBaseUri = new Uri(apiBaseUrl, UriKind.Absolute);
var mobileJoinBaseUrl = builder.Configuration["MobileJoinBaseUrl"];
var mobileJoinBaseUri = string.IsNullOrWhiteSpace(mobileJoinBaseUrl)
    ? null
    : new Uri(mobileJoinBaseUrl, UriKind.Absolute);

builder.Services.AddScoped(_ => new HttpClient { BaseAddress = apiBaseUri });
builder.Services.AddSingleton(new RoomClientOptions(apiBaseUri, mobileJoinBaseUri));
builder.Services.AddSingleton<IRoomRealtimeConnectionFactory, SignalRRoomRealtimeConnectionFactory>();
builder.Services.AddScoped<IBrowserStorage, LocalStorageBrowserStorage>();
builder.Services.AddScoped<IMobilePlayerSessionStore, MobilePlayerSessionStore>();
builder.Services.AddTransient<RoomClient>();
builder.Services.AddTransient<MobileJoinClient>();
builder.Services.AddTransient<MobileJoinCoordinator>();
builder.Services.AddTransient<MobileRoomClient>();

await builder.Build().RunAsync();
