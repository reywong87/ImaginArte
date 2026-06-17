# AI Development Workflow

## Purpose
This project is intended to be developed spec-first with Codex. Each implementation task should reference the docs and produce a small, working increment.

## How To Ask Codex For Work
Use focused tasks such as:

- "Create the solution structure for the documented architecture."
- "Implement the domain model for room, player, round, submission, and vote."
- "Add unit tests for the default scoring rules."
- "Create the SignalR contracts for room events without UI implementation."
- "Build the Blazor lobby screen using the existing application services."
- "Design the TV progress board state model before implementing animations."

## Expected Codex Behavior
Codex should:
- Read relevant docs before editing.
- Explain the intended slice briefly.
- Implement only the requested slice.
- Add or update tests when behavior changes.
- Avoid unrelated refactors.
- Keep domain behavior independent from Blazor and SignalR.

## Recommended Build Order
1. Solution and project structure.
2. Domain model and rule tests.
3. Application use cases for room and round actions.
4. In-memory infrastructure for early testing.
5. SignalR contracts and hubs.
6. Blazor lobby and TV shell.
7. Mobile join and player action views.
8. TV progress board state and animation slice.
9. SQLite persistence.
10. Reconnect and snapshot recovery.
11. Neon PostgreSQL and Render deployment preparation.

## Definition Of Done For Each Slice
- The requested behavior exists.
- Tests cover important rules or contracts.
- Public interfaces are named clearly.
- No business rules are hidden in UI components.
- Frontend choices do not leak into domain or application rules.
- Documentation is updated if the design changes.

## Documentation Maintenance
When implementation reveals a better design, update the relevant doc in the same task. Keep docs brief and useful for the next development step.
