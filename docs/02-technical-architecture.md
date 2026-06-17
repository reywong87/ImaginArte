# Technical Architecture

## Architecture Style
Use pragmatic Clean Architecture with clear boundaries and minimal ceremony.

Suggested solution shape for later implementation:

- `Domain`: entities, value objects, game rules, scoring, state transitions.
- `Application`: use cases, commands, queries, orchestration, ports.
- `Infrastructure`: persistence, time/random providers, external services.
- `Web`: ASP.NET Core host, Blazor UI, SignalR hubs, API endpoints.

## Dependency Direction
- `Domain` depends on nothing application-specific.
- `Application` depends on `Domain`.
- `Infrastructure` depends on `Application` abstractions.
- `Web` depends on `Application` and hosts UI/realtime endpoints.

## Frontend Strategy
Blazor is the first frontend, but not the product boundary.

To keep React migration possible later:
- Put game orchestration in application services.
- Expose stable HTTP and SignalR contracts.
- Avoid placing business rules in Blazor components.
- Keep view models separate from domain entities.
- Allow the Blazor frontend to be served from the ASP.NET Core backend for the MVP.
- Keep open the option to move the frontend to Cloudflare Pages later.

## Realtime Strategy
SignalR should broadcast state changes and receive player intents.

Hubs should:
- Validate connection/session context.
- Delegate commands to application services.
- Broadcast events or snapshots returned by application services.

Hubs should not:
- Own game state.
- Calculate scores.
- Decide legal moves beyond basic transport validation.

## Persistence Strategy
Start with SQLite for local development.

Use Neon PostgreSQL for the online/production MVP database.

Design persistence behind repository or unit-of-work abstractions only when they serve real use cases. Keep migrations simple. Avoid database-specific assumptions that block moving from SQLite to PostgreSQL.

Do not use Supabase initially. Reconsider it later only if Auth, Storage, or integrated platform features become important.

## Deployment Strategy
- Host the ASP.NET Core backend, API, SignalR hubs, and initial Blazor frontend on Render.
- Use Neon PostgreSQL for the hosted database.
- Keep the frontend deployable separately later, likely on Cloudflare Pages, by preserving HTTP and SignalR contracts.

## State Model
Important aggregate candidates:
- GameRoom
- Player
- Deck/Card
- Round
- Submission
- Vote
- Scoreboard
- BoardProgress

Game state should be recoverable from persistence or explicit snapshots, not from connected clients.

## Testing Priorities
- Round lifecycle.
- Legal and illegal player actions.
- Scoring outcomes.
- Storyteller rotation.
- Realtime event mapping.
- Persistence behavior for room recovery.
