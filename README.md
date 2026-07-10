# ImaginArte

ImaginArte is a family visual storytelling party game inspired by Dixit-style play: a shared TV screen shows the room and game progress, while each player uses a phone as a private controller.

The project is built with a pragmatic Clean Architecture approach. Game rules live in the domain, use cases live in the application layer, persistence lives in infrastructure, and the initial user experience is exposed through ASP.NET Core, SignalR, and Blazor WebAssembly.

## Current Status

The codebase currently includes:

- Domain model for rooms, players, cards, phases, votes, reveal state, scoring, and winners.
- Room and round use cases: create room, join room, start game, choose storyteller card, submit guess card, vote, score, complete, and advance rounds.
- Shared HTTP and realtime contracts between server and client.
- ASP.NET Core server with Minimal APIs, SignalR, CORS, health check, and state publishing.
- Swappable persistence: in-memory, local SQLite, and PostgreSQL for deployment.
- Periodic cleanup for inactive rooms when SQLite or PostgreSQL is used.
- Initial Blazor WebAssembly client for TV and mobile flows.
- Tests for domain, application, contracts, infrastructure, server, and client behavior.

## Stack

- .NET 10
- ASP.NET Core
- Blazor WebAssembly
- SignalR
- SQLite for local development
- PostgreSQL, prepared for Neon in production
- Render as the planned backend/API/SignalR hosting target

## Structure

```text
docs/
  00-product-vision.md
  01-game-rules.md
  02-technical-architecture.md
  03-realtime-flow.md
  04-ui-tv-mobile-flow.md
  05-ai-development-workflow.md
src/
  ImaginArte.Domain/
  ImaginArte.Application/
  ImaginArte.Contracts/
  ImaginArte.Infrastructure/
  ImaginArte.Server/
  ImaginArte.Client.Blazor/
tests/
  ImaginArte.*.Tests/
```

## Architecture

The dependency direction is intentional:

- `ImaginArte.Domain` does not depend on UI, databases, SignalR, or ASP.NET Core.
- `ImaginArte.Application` coordinates commands, queries, and ports around the domain.
- `ImaginArte.Infrastructure` implements repositories and persistence.
- `ImaginArte.Contracts` contains shared DTOs for HTTP and realtime communication.
- `ImaginArte.Server` composes dependencies, exposes APIs, and publishes SignalR events.
- `ImaginArte.Client.Blazor` renders the initial TV/mobile views and sends intents to the server.

SignalR is used as a realtime delivery mechanism, not as the source of truth. State is recovered through use cases and repositories.

## Requirements

Install the .NET 10 SDK. This repository was verified locally with:

```bash
dotnet --info
```

Relevant output:

```text
.NET SDK 10.0.300
Microsoft.AspNetCore.App 10.0.8
Microsoft.NETCore.App 10.0.8
```

## Run Locally

Restore and build:

```bash
dotnet restore ImaginArte.sln
dotnet build ImaginArte.sln
```

Run the server:

```bash
dotnet run --project src/ImaginArte.Server
```

By default, the server HTTP profile listens on:

```text
http://localhost:5080
```

Run the Blazor client in another terminal:

```bash
dotnet run --project src/ImaginArte.Client.Blazor
```

By default, the client HTTP profile listens on:

```text
http://localhost:5288
```

Main client routes:

- `/` home screen.
- `/tv` create/open the TV experience.
- `/tv/{roomCode}` TV lobby for a room.
- `/join` join from mobile.
- `/join/{roomCode}` join from mobile with a prefilled room code.

## Local Configuration

The server uses `src/ImaginArte.Server/appsettings.Development.json`:

```json
{
  "Persistence": {
    "Provider": "SQLite",
    "ConnectionString": "Data Source=imaginarte.db"
  }
}
```

Supported providers:

- `InMemory`
- `SQLite`
- `PostgreSQL`

The Blazor client reads `src/ImaginArte.Client.Blazor/wwwroot/appsettings.json`:

```json
{
  "ApiBaseUrl": "http://localhost:5080/",
  "MobileJoinBaseUrl": "http://localhost:5288/"
}
```

For testing with phones on the same network, replace `localhost` with your machine's LAN IP, for example `http://<your-lan-ip>:5080/` and `http://<your-lan-ip>:5288/`, and use the `wifi` launch profile.

## HTTP API

The server exposes:

| Method | Route | Purpose |
| --- | --- | --- |
| `GET` | `/health` | Basic health check |
| `POST` | `/rooms` | Create a room |
| `POST` | `/rooms/{roomCode}/players` | Join a player |
| `POST` | `/rooms/{roomCode}/start` | Start the game |
| `POST` | `/rooms/{roomCode}/storyteller-card` | Choose storyteller card and clue |
| `POST` | `/rooms/{roomCode}/guess-cards` | Submit a player's guess card |
| `POST` | `/rooms/{roomCode}/votes` | Cast a vote |
| `POST` | `/rooms/{roomCode}/score` | Calculate round scoring |
| `POST` | `/rooms/{roomCode}/complete-round` | Complete the round |
| `POST` | `/rooms/{roomCode}/advance-round` | Advance the phase |
| `POST` | `/rooms/{roomCode}/next-round` | Start the next round |
| `GET` | `/rooms/{roomCode}` | Get public room state |
| `GET` | `/rooms/{roomCode}/players/{playerId}` | Get private player state |

Domain rule errors are returned as `400`, missing rooms as `404`, and duplicate rooms as `409`.

## Realtime

SignalR is available at:

```text
/hubs/rooms
```

Hub methods:

- `SubscribeToRoom(roomCode)` for the public TV view.
- `SubscribeAsPlayer(roomCode, playerId)` for a player's private connection.

Published events:

- `RoomUpdated`
- `PlayerStateUpdated`

Reconnects should request fresh snapshots; missed events are not expected to be replayed.

## Game Rules

The current flow follows `docs/01-game-rules.md`:

1. A storyteller chooses a card and gives a clue.
2. The other players submit a card that fits the clue.
3. The TV shows the submitted cards in random order.
4. Players vote for the card they think belonged to the storyteller.
5. Votes are revealed, scoring is calculated, and the round advances.

Default scoring:

- If some, but not all, guessers find the storyteller card: the storyteller and correct guessers receive 3 points.
- If everyone finds it or nobody finds it: the storyteller receives 0 points and guessers receive 2 points.
- Each vote received by a player's submitted card gives that player 1 point.

The default target score is 25 points.

## Tests

Run the full suite:

```bash
dotnet test ImaginArte.sln --no-restore -m:1
```

Local verification:

```text
271 tests passed
0 failures
```

Note: in sandboxed environments, MSBuild may need permission to create internal pipes. If you see `System.Net.Sockets.SocketException (13): Permission denied`, run the tests outside the sandbox.

## Deployment

Production is prepared for PostgreSQL. On Render, configure:

```text
Persistence__Provider=PostgreSQL
Persistence__ConnectionString=<Neon connection string>
RoomCleanup__Enabled=true
Cors__AllowedOrigins__0=<frontend origin>
```

`GET /health` can be used as the liveness endpoint. The Neon connection string must be configured as an environment secret and must not be committed.

## Development Guide

Before changing behavior, read the relevant documents in `docs/`. The project is intended to move forward through small vertical slices:

- keep rules in the domain;
- coordinate behavior in the application layer;
- expose stable HTTP and SignalR contracts;
- avoid business rules in Blazor components or hubs;
- add tests when rules, state, persistence, or realtime contracts change.

See `AGENTS.md` for the repository-specific working instructions.
