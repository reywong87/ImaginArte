# Product Vision

## Summary
ImagoraGame is a family storytelling party game played on a TV, with each player using a smartphone as their private controller.

The game should feel simple enough for children and adults to play together, while giving the host a smooth living-room experience.

## Core Experience
- A host opens a game room on the TV.
- Players join from phones using a room code or QR code.
- Each round, one storyteller gives a clue for a chosen image.
- Other players submit images they think match the clue.
- Everyone votes for the image they believe was the storyteller's.
- Scores are revealed and the next round begins.
- The TV shows an original ImagoraGame progress board where player avatars advance after scoring.

## Product Goals
- Fast setup on a couch or family table.
- Clear separation between public TV information and private phone actions.
- A shared TV board that makes score progress visible, social, and fun to watch.
- Realtime updates with minimal friction.
- Rules that can be adjusted later for house variants.
- A codebase that can evolve from Blazor to React without rewriting the game core.

## First Playable Scope
- Local web app.
- One active room.
- Multiple players joining by code.
- Basic round lifecycle.
- Temporary image/card set.
- Realtime TV and mobile views.
- Original TV progress board with player avatars or tokens.
- Local SQLite persistence only where useful.
- Online MVP deployment path using Render and Neon PostgreSQL.

## Out Of Scope Initially
- Online matchmaking.
- Accounts and profiles.
- Persistent global leaderboards.
- Custom card uploads.
- Moderation tools.
- Native mobile apps.
