# ImaginArte agent guide

## Verify changes

- Requires the .NET 10 SDK.
- From the repository root, restore and build with `dotnet restore ImaginArte.sln` and `dotnet build ImaginArte.sln`.
- Run the complete test suite with `dotnet test ImaginArte.sln --no-restore -m:1`. Run a focused suite with `dotnet test tests/<project>/<project>.csproj --no-restore`; use `--filter FullyQualifiedName~<name>` for one test.
- If tests fail with `System.Net.Sockets.SocketException (13): Permission denied` while MSBuild creates internal pipes, rerun outside the sandbox.

## Spec acceptance criteria

- Do not launch the `spec-verifier` subagent automatically when the last implementation-plan step of a spec is complete. Acceptance-criteria verification is a manual follow-up; the user will explicitly request it.
- When the user requests verification, use the `spec-verifier` subagent to check each acceptance criterion against the implementation, tests, and relevant screens.
- Have the subagent update the spec with each criterion's verification status and concrete evidence. Keep unmet criteria visible as gaps; do not mark them satisfied without evidence.

## Read before changing behavior

- Product and game behavior is specified in `docs/`; read the relevant document before changing rules or UI behavior. `docs/07-progress-board.md` is the visual/progress-board specification and explicitly keeps game rules out of the UI.
- The solution already has domain, application, contracts, infrastructure, server, a legacy Blazor client, and corresponding test projects. React is the target UI replacement, but `src/ImaginArte.Client.React` is not yet implemented. Keep changes in the owning layer rather than adding business rules to endpoints, hubs, Razor components, or future React components.
- `ImaginArte.Domain` owns entities, scoring, state transitions, and invariants; it must remain independent of ASP.NET Core, SignalR, Blazor, and persistence.
- `ImaginArte.Application` owns commands, queries, orchestration, and ports. `ImaginArte.Infrastructure` implements persistence adapters. `ImaginArte.Contracts` owns the HTTP/SignalR request, response, and event shapes.
- `ImaginArte.Server` is the composition root and exposes Minimal APIs plus the `/hubs/rooms` SignalR hub. SignalR publishes state but is not the source of truth; recover state through application handlers/repositories.

## React Migration Rules

The frontend is being migrated in explicit specs and vertical slices. Until a future implementation spec says otherwise:

- For every task involving React, read and follow the approved `docs/09-react-tutoring-guide.md` before planning or editing. Treat it as a mandatory tutoring contract: explain in Spanish, assume no prior React knowledge, use concrete Blazor analogies and their limits, introduce concepts progressively, explain files/data flow/verification, and finish with a learning recap. Do not provide unexplained React code.
- Do not create a replacement React screen as an incidental part of a backend or domain change.
- Do not remove `src/ImaginArte.Client.Blazor` or alter the solution-wide client setup until React has feature parity and the removal is explicitly specified.
- The future React client must be independent from .NET assemblies and consume the existing HTTP and SignalR contracts through TypeScript models/services.
- React components render state and collect intents. They must not implement scoring, phase transitions, storyteller rotation, card ownership rules, or other domain decisions.
- Use HTTP commands for authoritative mutations and SignalR for realtime delivery. After initial connection, reconnect, or recovery, fetch a fresh snapshot; do not attempt to replay missed events in the browser.
- Keep public TV information separate from private mobile information. Never expose hands, card ownership, pending-player identity, or individual votes where the screen specifications prohibit them.
- Treat `docs/08-screen-specifications.md` as the screen behavior source of truth and `docs/06-design-system.md`, `docs/07-progress-board.md`, and `docs/assets/visual-references/mockups/` as visual/design references.
- Mockups are not authoritative for game rules, privacy boundaries, accessibility, or interaction correctness. Do not reproduce overlaps, clipped controls, or information disclosures identified in the screen specification.
- Keep the React API base URL configurable at build time, rather than hard-coding a deployment host.
- Motion is the approved React animation dependency when animation is needed. Use `motion` with imports from `motion/react` for meaningful board, reveal, and state transitions; use CSS for simple states and do not add animation as decoration by default.
- Every animation must respect `prefers-reduced-motion`. Replace board travel, parallax, particles, and large transforms with direct state changes or brief fades while preserving readable state, score, and outcome information.
- Do not install `motion` or create its configuration until the React client is introduced. Do not add another animation library without an explicit decision.
- Playwright is the approved browser E2E tool for the React client. Once the client has an executable test suite, use `@playwright/test` to validate TV/mobile flows, viewport layouts, reconnect/recovery states, privacy boundaries, keyboard access, and reduced-motion behavior.
- Playwright tests must assert user-visible behavior through accessible roles, labels, and text where possible. Cover both the public TV view and private mobile view; never weaken privacy assertions to match a mockup.
- Browser automation exposed by the development environment is not a reason to add Playwright to the repository early. Add `@playwright/test`, configuration, and browser installation only with the React E2E slice.
- Use Context7 before using current documentation for libraries, frameworks, SDKs, APIs, CLI tools, or cloud services, including React, Vite, Motion, Playwright, and SignalR. Resolve the library ID first and prefer official/high-quality documentation.
- Context7 is not required for ordinary refactoring, business-logic debugging, or general programming concepts. Do not use external documentation to move game rules into the UI.

The current documentation update only establishes these boundaries. React project creation, package selection, SignalR client implementation, screen migration, visual implementation, and Blazor removal belong to later specs.

## Card Assets And Catalog

- Domain and persisted room state use stable `CardId` values; they must not store image binaries or UI-specific paths.
- The target production storage is Cloudflare R2/CDN for optimized card images, with a versioned manifest containing stable IDs and accessibility metadata.
- A first React slice may bundle cards under `public/cards/`, but this is a delivery detail that must remain replaceable by R2.
- Do not put card images in PostgreSQL or room snapshots.
- Do not replace the current temporary deck or introduce a real card catalog without a dedicated spec covering IDs, manifest shape, missing assets, versioning, and tests. The current `TEMP-001` to `TEMP-120` deck is intentionally still present.
- `docs/assets/cartas/` contains source/design assets currently; it is not automatically a runtime catalog.

## Runtime gotchas

- Local server development uses SQLite from `src/ImaginArte.Server/appsettings.Development.json`; supported `Persistence:Provider` values are `InMemory`, `SQLite`, and `PostgreSQL`.
- PostgreSQL requires `Persistence__ConnectionString`; do not commit connection strings. Production also configures `RoomCleanup__Enabled` and `Cors__AllowedOrigins__0` through environment variables.
- Run the backend with `dotnet run --project src/ImaginArte.Server` (HTTP profile: `http://localhost:5080`) and the client separately with `dotnet run --project src/ImaginArte.Client.Blazor` (HTTP profile: `http://localhost:5288`).
- For phone testing on the LAN, use the server/client `wifi` profiles and replace localhost URLs in the client `wwwroot/appsettings.json` with the machine’s LAN IP.
- Realtime clients subscribe with `SubscribeToRoom(roomCode)` or `SubscribeAsPlayer(roomCode, playerId)`. Reconnects must fetch fresh snapshots; missed events are not replayed.
- A separately hosted React client requires exact-origin CORS configuration on the server, HTTPS in production, and `wss://` SignalR connectivity. Do not broaden CORS to `AllowAnyOrigin` as a shortcut.

## Target Frontend Deployment

The documented target topology is:

```text
React UI       -> Cloudflare Pages
ASP.NET Core   -> Render
SignalR hub    -> Render
PostgreSQL     -> Neon
Card images    -> Cloudflare R2/CDN
```

Serving the React build from Render is acceptable as a temporary transition, but independent Cloudflare Pages hosting is the preferred target. The frontend must remain replaceable without changing the backend's HTTP and SignalR contracts.
