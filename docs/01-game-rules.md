# Game Rules

## Players
- Recommended: 3 to 8 players.
- One device displays the shared TV view.
- Each player controls their own private hand from a smartphone.

## Round Roles
- One player is the storyteller.
- All other players are guessers.
- The storyteller role rotates each round.

## Round Flow
1. The storyteller chooses one card from their private hand.
2. The storyteller enters or says a short clue.
3. Each guesser chooses one card from their private hand that could match the clue.
4. The TV reveals all submitted cards in random order.
5. All guessers vote for the card they believe belongs to the storyteller.
6. The server locks voting after the final guesser votes. Until then, the TV shows only aggregate progress, not individual votes.
7. The TV reveals which guesser voted for each card while keeping the storyteller card unmarked.
8. The TV explicitly reveals the storyteller card.
9. Scores are calculated.
10. Used cards are discarded and hands are refilled.
11. The next storyteller starts a new round.

## Card Deck
- The temporary deck contains exactly 98 illustrated cards, identified as `TEMP-001` through `TEMP-098`.
- Each player starts with six cards.
- If the available deck cannot complete a draw during a refill, the discarded cards are shuffled back into the deck and drawing continues.
- Cards that remain in players' hands are never recycled.

## Voting Rules
- The storyteller does not vote.
- A guesser cannot vote for their own submitted card.
- A guesser may vote only once.

## Default Scoring
- If at least one guesser finds the storyteller card and at least one does not:
  - Storyteller: 3 points.
  - Correct guessers: 3 points.
  - Players whose submitted card received votes: 1 point per vote.
- If everyone finds the storyteller card:
  - Storyteller: 0 points.
  - Each guesser: 2 points.
- If nobody finds the storyteller card:
  - Storyteller: 0 points.
  - Each guesser: 2 points.
  - Players whose submitted card received votes: 1 point per vote.

## End Of Game
- The game ends when at least one player reaches the configured target score.
- Suggested targets:
  - Short game: 15 points.
  - Medium game: 25 points.
  - Long game: 35 points.
- If several players reach the target in the same round, the highest score wins.
- If first place is tied, play one final tiebreaker round.
- Future variants may use fixed round count or manual host ending.

## Rule Design Notes
- Keep rules deterministic and unit-testable.
- Store rule options separately from round state.
- Treat target score and tiebreaker behavior as configurable rule options.
- Do not put scoring logic in UI components or SignalR hubs.
