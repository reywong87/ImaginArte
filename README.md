# ImaginArte

ImaginArte is a family visual storytelling party game inspired by Dixit-style play: a shared TV screen shows the room and game progress, while each player uses a phone as a private controller.

The project is built with a pragmatic Clean Architecture approach. Game rules live in the domain, use cases live in the application layer, persistence lives in infrastructure, and the user experience is being migrated from Blazor WebAssembly to an independent React application. ASP.NET Core and SignalR remain the backend boundary for both TV and mobile experiences.

The React migration is intentionally being delivered in separate vertical slices. The independent React client scaffold now exists; game screens and API/realtime integrations are introduced separately. This document records the target architecture and deployment decisions and does not imply that every screen has been implemented.

## Current Status

The codebase currently includes:

- Domain model for rooms, players, cards, phases, votes, reveal state, scoring, and winners.
- Room and round use cases: create room, join room, start game, choose storyteller card, submit guess card, vote, score, complete, and advance rounds.
- Shared HTTP and realtime contracts between server and client.
- ASP.NET Core server with Minimal APIs, SignalR, CORS, health check, and state publishing.
- Swappable persistence: in-memory, local SQLite, and PostgreSQL for deployment.
- Periodic cleanup for inactive rooms when SQLite or PostgreSQL is used.
- Initial Blazor WebAssembly client for TV and mobile flows, plus a React/TypeScript/Vite scaffold for the replacement; React game screens and integrations are not yet complete.
- Tests for domain, application, contracts, infrastructure, server, and client behavior.

## Stack

- .NET 10
- ASP.NET Core
- React and TypeScript for the target frontend.
- Vite for the target frontend build and local development workflow.
- Motion for purposeful React animations, especially board progress and reveal transitions; use CSS for simple state changes.
- SignalR
- SQLite for local development
- PostgreSQL, prepared for Neon in production
- Render as the planned backend/API/SignalR hosting target.
- Cloudflare Pages as the planned independent React hosting target.
- Cloudflare R2/CDN as the planned production location for card images.

### Frontend tooling decisions

- **Motion** is the approved animation library for the React client, and the `motion` package is installed. Import React APIs from `motion/react`. Use it for meaningful transitions such as board-token movement, arrival states, and restrained reveals; do not use it for decorative motion everywhere. The scaffold placeholder does not add animations.
- All Motion animations must respect `prefers-reduced-motion`. Reduced motion must replace travel, parallax, particles, and large transforms with direct state changes or brief fades while keeping score and game state understandable.
- **Playwright** is the approved browser-validation tool for the React client. When the client and E2E suite are introduced, add `@playwright/test` as a development dependency and use it to validate public TV and private mobile flows, reconnect states, privacy boundaries, keyboard navigation, responsive layouts, and reduced-motion behavior.
- Playwright browser automation available through development tooling does not by itself require a project dependency. Do not add Playwright configuration or browser binaries before the React client has an executable test suite.

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
  ImaginArte.Client.React/       # independent React + TypeScript + Vite frontend
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
- `ImaginArte.Client.Blazor` is the legacy/transition client and renders the initial TV/mobile views.
- `ImaginArte.Client.React` will render the replacement TV/mobile views and send intents to the same HTTP and SignalR contracts. It must not duplicate game rules.

SignalR is used as a realtime delivery mechanism, not as the source of truth. State is recovered through use cases and repositories. A React client must fetch a fresh public or private snapshot after connection and reconnection; missed realtime events are not replayed.

## Frontend Migration Boundary

The frontend migration is deliberately separate from the game implementation:

- The backend, domain rules, application handlers, persistence, HTTP routes, and SignalR hub remain authoritative.
- React components may render view models and collect intents, but must not calculate scoring, choose legal phases, rotate storytellers, or mutate game state locally.
- The existing HTTP and SignalR contracts are the integration boundary. Do not couple React to domain or infrastructure assemblies.
- The TV is a public surface and the phone is a private controller. Follow `docs/04-ui-tv-mobile-flow.md` and `docs/08-screen-specifications.md` for visibility rules.
- The board and visual behavior must follow `docs/06-design-system.md`, `docs/07-progress-board.md`, and the approved visual references. Mockups are visual references, not a source of game rules.
- React migration work should be specified and implemented in separate vertical slices. Do not remove Blazor or change all screens in one unreviewed step.

The React project scaffold establishes this boundary without changing the .NET solution. Migrating screens, adding service integrations, and removing Blazor remain future implementation work.

## Documentation And Browser Validation

- Use **Context7** before relying on current documentation for a library, framework, SDK, API, CLI tool, or cloud service. Resolve the library first, then query its official or highest-quality documentation. This applies to React, Vite, Motion, Playwright, SignalR, and future frontend dependencies.
- Context7 is for current external documentation and API usage. It is not required for ordinary refactoring, business-logic debugging, or general programming concepts.
- Use **Playwright** for end-to-end behavior that requires a real browser. Prefer accessible roles, labels, and user-visible state over implementation selectors. Cover both TV and mobile viewport profiles and verify that prohibited private information never appears on the public TV surface.
- Keep browser tests independent from domain rules. They should assert rendered contracts and user flows; scoring, phase transitions, and legality remain authoritative in the backend.

## Card Asset Strategy

Cards have two separate concerns:

1. The domain stores stable card identifiers (`CardId`) in hands, submissions, votes, and persisted room snapshots.
2. The frontend resolves those identifiers to image and accessibility metadata.

The recommended production arrangement is:

- Store card image files as optimized web assets in Cloudflare R2, served through a CDN.
- Keep a versioned card manifest containing the stable ID, image URL, and accessible description.
- Do not store image binaries in PostgreSQL or inside room JSON snapshots.
- Keep the manifest and image set compatible with persisted card IDs; never silently remove an asset referenced by an active or recoverable room.

For the first React slice, static card assets may be bundled under the React application's `public/cards/` directory. Moving those files to R2 later must not require changing domain rules or room persistence.

The current domain still creates a temporary `TEMP-001` to `TEMP-120` deck. Replacing it with the real card catalog is intentionally out of scope for this documentation phase and must be handled by a future spec. The repository currently contains visual card assets under `docs/assets/cartas/`; those files are design/source assets, not yet a runtime card catalog.

## Requirements

Install Node.js `>=22.12.0` with npm for the React client, and the .NET 10 SDK for the backend and Blazor client. This repository was verified locally with:

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

Run the legacy Blazor client in another terminal while it remains part of the transition:

```bash
dotnet run --project src/ImaginArte.Client.Blazor
```

By default, the client HTTP profile listens on:

```text
http://localhost:5288
```

Run the independent React client from another terminal:

```bash
cd src/ImaginArte.Client.React
cp .env.example .env.local
npm ci
npm run dev
```

Vite serves the development client at `http://localhost:5173` by default. The current screen is only a scaffold placeholder; it does not call the API or use SignalR.

Run the client checks and create a production build with:

```bash
npm run lint
npm run typecheck
npm run build
```

After building, serve the production output locally with:

```bash
npm run preview
```

Vite serves the preview at `http://localhost:4173` by default.

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

The legacy Blazor client reads `src/ImaginArte.Client.Blazor/wwwroot/appsettings.json`:

```json
{
  "ApiBaseUrl": "http://localhost:5080/",
  "MobileJoinBaseUrl": "http://localhost:5288/"
}
```

For testing with phones on the same network, replace `localhost` with your machine's LAN IP, for example `http://<your-lan-ip>:5080/` and `http://<your-lan-ip>:5288/`, and use the `wifi` launch profile.

The React client reserves `VITE_API_BASE_URL` for its build-time API base URL. For local development, copy `.env.example` to `.env.local`; the example points to `http://localhost:5080`. This variable is public configuration that is included in browser code, so never put secrets in any `VITE_*` variable. The scaffold placeholder does not connect to the API yet. The deployed React origin must be added to the server's `Cors:AllowedOrigins` configuration, and the production React host must use HTTPS so SignalR can use `wss://`.

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

Production is prepared for PostgreSQL. The target deployment topology is:

```text
React UI       -> Cloudflare Pages
ASP.NET Core   -> Render
SignalR hub    -> Render
PostgreSQL     -> Neon
Card images    -> Cloudflare R2/CDN
```

The simpler transition option is to serve the React build from the ASP.NET Core host on Render. Independent Cloudflare Pages hosting is preferred for the target architecture because it keeps the UI deployable independently while preserving the existing HTTP and SignalR boundary.

On Render, configure:

```text
Persistence__Provider=PostgreSQL
Persistence__ConnectionString=<Neon connection string>
RoomCleanup__Enabled=true
Cors__AllowedOrigins__0=<frontend origin>
```

When the React app is hosted separately, CORS must allow only the exact trusted frontend origins and must allow credentials for SignalR. Do not use `AllowAnyOrigin` for the production frontend. The React SignalR client must opt into automatic reconnect and fetch a fresh snapshot after reconnecting.

`GET /health` can be used as the liveness endpoint. The Neon connection string must be configured as an environment secret and must not be committed.

## Development Guide

Before changing behavior, read the relevant documents in `docs/`. The project is intended to move forward through small vertical slices:

- keep rules in the domain;
- coordinate behavior in the application layer;
- expose stable HTTP and SignalR contracts;
- avoid business rules in React/Blazor components or hubs;
- use the mockups for visual direction without copying prohibited information or interaction errors from them;
- add tests when rules, state, persistence, or realtime contracts change.

See `AGENTS.md` for the repository-specific working instructions.
