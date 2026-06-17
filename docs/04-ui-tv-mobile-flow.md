# UI TV And Mobile Flow

## Design Principle
The TV is public and shared. Phones are private and action-oriented.

The TV should tell the room what is happening and make progress exciting to watch. Phones should tell each player what they can do now.

## TV Views
### Lobby
- Room code.
- QR code.
- Joined players.
- Start game control for host.

### Storyteller Turn
- Current storyteller name.
- Waiting state while storyteller chooses a card and clue.
- Clue display after submitted.

### Guesser Submission
- Clue.
- Submission progress.
- No private hands shown.

### Voting
- Submitted cards in randomized order.
- Clear card labels or numbers.
- Voting progress.

### Reveal And Scoring
- Show storyteller card.
- Show vote results.
- Show points gained this round.
- Show total scoreboard.
- Animate player progress on the shared board after scoring.

### Progress Board
- Use an original ImagoraGame visual design, not a copy of any existing board game.
- Base concept: an imaginative path or map with one avatar/token per player.
- Advance tokens according to points gained after each scoring phase.
- Keep the board readable from a couch on a TV.
- Make the board a shared social moment; the phone remains only a private controller.

## Mobile Views
### Join
- Room code entry or QR deep link.
- Player display name.

### Lobby
- Player identity.
- Waiting for host.

### Storyteller Action
- Private hand.
- Select card.
- Enter clue.
- Submit.

### Guesser Submission
- Clue.
- Private hand.
- Select matching card.
- Submit.

### Vote
- Public submitted cards.
- Disable own card.
- Cast one vote.

### Waiting States
- Confirm the player's completed action.
- Show concise waiting message.

## UI Boundary Notes
- Blazor components should render state and collect user intents.
- Components should not contain scoring or round transition logic.
- Shared UI state should come from application-facing view models.
- Future React migration should be possible by reusing HTTP and SignalR contracts.
- Board animation should consume score/progress state from application view models, not calculate game results.

## Accessibility And Family Use
- Large TV typography.
- Clear card numbers during voting.
- Simple mobile actions with one primary button.
- Avoid time pressure in the first version.
