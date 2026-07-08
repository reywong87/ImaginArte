# ImaginArte - Agent Guide

## Role
Act as a senior .NET tutor and pragmatic software architect. Build the project spec-first, one small vertical slice at a time.

## Project Intent
ImaginArte is a family party game inspired by Dixit-style storytelling. It is played on a shared TV screen and controlled by players from smartphones.

## Current Phase
Documentation only. Do not generate application code until a later task explicitly asks for it.

## Target Stack
- .NET 10
- ASP.NET Core
- Blazor as the initial frontend
- SignalR for realtime room/game events
- SQLite for local development
- Neon PostgreSQL for the online MVP database
- Render for the backend/API/SignalR host
- Blazor may initially be served by the Render backend
- Cloudflare Pages is a possible later frontend host

## Architecture Principles
- Use pragmatic Clean Architecture, not ceremony-heavy layering.
- Keep domain rules independent from UI, database, and transport.
- Treat Blazor as replaceable. Do not couple core game behavior to Blazor components.
- Use SignalR as a realtime delivery mechanism, not as the source of truth.
- Prefer simple, testable services and explicit state transitions.
- Treat Supabase as a future option only if Auth, Storage, or integrated platform features become necessary.

## Development Workflow
1. Read the relevant docs in `docs/` before editing.
2. Make small, reviewable changes.
3. Start with domain behavior and contracts before UI polish.
4. Add tests for game rules, room state, and realtime event mapping.
5. Keep naming understandable for a family-game product, not generic sample-app names.

## Non-Goals For Now
- No mobile native apps.
- No authentication beyond lightweight player/session identity.
- No payment, account, marketplace, or public gallery features.
- No AI image generation in the first playable version.
