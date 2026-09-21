# ImaginArte agent guide

## Verify changes

- Requires the .NET 10 SDK.
- From the repository root, restore and build with `dotnet restore ImaginArte.sln` and `dotnet build ImaginArte.sln`.
- Run the complete test suite with `dotnet test ImaginArte.sln --no-restore -m:1`. Run a focused suite with `dotnet test tests/<project>/<project>.csproj --no-restore`; use `--filter FullyQualifiedName~<name>` for one test.
- If tests fail with `System.Net.Sockets.SocketException (13): Permission denied` while MSBuild creates internal pipes, rerun outside the sandbox.

## Read before changing behavior

- Product and game behavior is specified in `docs/`; read the relevant document before changing rules or UI behavior. `docs/07-progress-board.md` is the visual/progress-board specification and explicitly keeps game rules out of the UI.
- The solution already has domain, application, contracts, infrastructure, server, Blazor client, and corresponding test projects. Keep changes in the owning layer rather than adding business rules to endpoints, hubs, or Razor components.
- `ImaginArte.Domain` owns entities, scoring, state transitions, and invariants; it must remain independent of ASP.NET Core, SignalR, Blazor, and persistence.
- `ImaginArte.Application` owns commands, queries, orchestration, and ports. `ImaginArte.Infrastructure` implements persistence adapters. `ImaginArte.Contracts` owns the HTTP/SignalR request, response, and event shapes.
- `ImaginArte.Server` is the composition root and exposes Minimal APIs plus the `/hubs/rooms` SignalR hub. SignalR publishes state but is not the source of truth; recover state through application handlers/repositories.

## Runtime gotchas

- Local server development uses SQLite from `src/ImaginArte.Server/appsettings.Development.json`; supported `Persistence:Provider` values are `InMemory`, `SQLite`, and `PostgreSQL`.
- PostgreSQL requires `Persistence__ConnectionString`; do not commit connection strings. Production also configures `RoomCleanup__Enabled` and `Cors__AllowedOrigins__0` through environment variables.
- Run the backend with `dotnet run --project src/ImaginArte.Server` (HTTP profile: `http://localhost:5080`) and the client separately with `dotnet run --project src/ImaginArte.Client.Blazor` (HTTP profile: `http://localhost:5288`).
- For phone testing on the LAN, use the server/client `wifi` profiles and replace localhost URLs in the client `wwwroot/appsettings.json` with the machine’s LAN IP.
- Realtime clients subscribe with `SubscribeToRoom(roomCode)` or `SubscribeAsPlayer(roomCode, playerId)`. Reconnects must fetch fresh snapshots; missed events are not replayed.
