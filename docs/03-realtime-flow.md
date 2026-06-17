# Realtime Flow

## Realtime Goals
- Keep TV and phones synchronized.
- Make private player actions visible only on that player's device.
- Make public room state visible on the TV.
- Recover gracefully from refreshes and reconnects.

## Connection Roles
- Host/TV connection: public room display and host controls.
- Player connection: private hand, submission, vote, and readiness.

## Joining A Room
1. TV creates or opens a room.
2. Server generates a short room code.
3. TV shows room code and QR join URL.
4. Player opens join URL.
5. Player enters display name.
6. Server creates or resumes a player session.
7. Server sends private player snapshot to phone.
8. Server broadcasts updated lobby snapshot to TV.

## Command Flow
Clients send intents, not final state.

Examples:
- `JoinRoom`
- `StartGame`
- `ChooseStorytellerCard`
- `SubmitGuessCard`
- `CastVote`
- `AdvanceRound`

Application services validate each intent and return resulting state changes.

## Event Flow
Server broadcasts public and private updates.

Public examples:
- `RoomUpdated`
- `GameStarted`
- `RoundPhaseChanged`
- `SubmissionsReady`
- `VotesRevealed`
- `ScoreboardUpdated`
- `BoardProgressUpdated`
- `TiebreakerRoundStarted`
- `GameCompleted`

Private examples:
- `PlayerHandUpdated`
- `SubmissionAccepted`
- `VoteAccepted`
- `ActionRejected`

Board progress events should be derived from scoring results. They should drive TV animation, not calculate scores.

## Round Phases
- `Lobby`
- `StorytellerChoosing`
- `GuessersSubmitting`
- `Voting`
- `Reveal`
- `Scoring`
- `RoundComplete`
- `GameComplete`

## Reconnect Behavior
On reconnect, the client requests a fresh snapshot:
- TV receives the full public room snapshot.
- Player receives public room context plus private player state.

Do not rely on missed realtime events being replayed to rebuild UI state.
