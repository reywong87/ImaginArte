# Screen Specifications

## Scope And Non-Negotiable Boundaries

This document is the screen source of truth for the shared TV and private mobile controllers. It specifies presentation and interaction only. Game rules, scoring, role rotation, and phase authority remain outside the interface.

- TV is public, spacious, and readable from 2-3 metres. It may show board context, but the central safe area remains available for game content.
- Mobile is private, uses Paper Warm with subtle paper texture, never shows the full board, and presents one clear primary action. A selection remains reversible until explicit confirmation.
- Playable cards remain portrait `2:3`; no title or text overlays the illustration. Voting numbers sit outside the art.
- Before revelation, TV never shows hands, any selected or submitted-card owner, the storyteller card identity, individual votes, or which players made pending choices.
- During voting and revelation, cards occupy the board's central free area. During revelation, tokens do not move; token movement belongs only to board progress.

# Shared TV

## 1. Room Lobby

### Purpose
Bring players into a room and let the host begin when the room is ready.

### User And Surface
All players view the shared TV; only the host acts on the TV.

### Visible Information
Room code, QR code, joined-player names, player count, configured target score, board context, and host-ready status.

### Prohibited Information
Hands, player card identities, future storyteller, private session details, or a start control for non-hosts.

### Primary Action
Host selects **Start game** and explicitly confirms it.

### States And Transitions
Empty, joining, ready, and host-confirmation states; on start, transition to **Waiting for storyteller**. If a player disconnects, retain their name with a public neutral reconnecting status.

### Visual Composition
Room code and QR form the dominant central stage; roster sits in a quiet side or lower band. The 25-node board is background context, with no constellation entering the central stage.

### Accessibility
Room code uses large tabular figures; QR has the manual-code alternative; roster status uses text and icon, not color alone; host control has a persistent focus state.

### Visual Reference
`docs/assets/visual-references/draft/hoja-referencia-lobby-imaginarte.png`.

### Acceptance Criteria
From 2-3 m, a guest can read the room code, see how many players joined, and identify the host-only start action without seeing any private game data.

## 2. Waiting For Storyteller

### Purpose
Announce the active narrator and hold the room while they privately choose a card and clue.

### User And Surface
All players view TV; the narrator acts only on their mobile; host has no phase-advance action.

### Visible Information
Narrator name, role label, concise waiting instruction, and optional board context with the active narrator token halo and contextual label.

### Prohibited Information
Narrator hand, selected card, clue before submission, selection progress, and any other player's private state.

### Primary Action
No TV action; the narrator completes the private mobile action.

### States And Transitions
Waiting and narrator-reconnecting states; after valid narrator submission, transition to **Clue and submission progress**.

### Visual Composition
Use the upper safe band for phase and narrator identity. Keep the central stage mostly open; the active token may use the board's Moon Gold halo outside the overlay zone.

### Accessibility
Use a literal message such as “Ana is choosing a card and clue”; role is conveyed by text, label, halo, and token identity.

### Visual Reference
No dedicated reference yet; follow `hoja-referencia-lobby-imaginarte.png` board framing.

### Acceptance Criteria
The room understands who is acting and what it is waiting for, while no visual element reveals the narrator's card or clue.

## 3. Clue And Submission Progress

### Purpose
Present the narrator's clue and indicate collective submission progress without exposing individual choices.

### User And Surface
All players view TV; guessers act on mobiles; narrator waits; host has no phase-advance action.

### Visible Information
Clue, narrator name, aggregate submission count such as “3 of 5 cards received,” and optional board context.

### Prohibited Information
Hands, submitted-card art, submitter identity, who remains pending, narrator card identity, or any voting information.

### Primary Action
No TV action; each guesser submits privately.

### States And Transitions
Collecting, all-submitted, and reconnecting states. Aggregate progress updates without identifying players. When all required submissions exist, transition to **Voting**.

### Visual Composition
Place clue in the upper safe band using display type only if fully readable; place aggregate progress in the lower status band. Keep the central card area empty until voting.

### Accessibility
Clue remains high-contrast on an opaque Paper Warm surface; progress states pair count, text, and icon; no countdown or time pressure.

### Visual Reference
No dedicated reference yet; use the clue treatment established by `votacion con cartas y pista.png`.

### Acceptance Criteria
The clue is readable from 2-3 m, and the TV communicates only aggregate completion, never a pending player's identity or a selected card.

## 4. Voting

### Purpose
Show the randomized submitted-card set for private guesser voting.

### User And Surface
All players view TV; guessers vote on mobile; narrator and host do not advance the phase.

### Visible Information
Clue, randomized submitted cards, stable high-contrast card numbers, and aggregate vote count such as “2 of 5 votes cast.”

### Prohibited Information
Hands, votes or vote targets, voter identity, pending-voter identity, narrator-card identity, card ownership, or selection indicators.

### Primary Action
No TV action; guessers vote privately.

### States And Transitions
Voting, aggregate-progress, all-voted, and reconnecting states. Card order and numbers remain stable. When all eligible votes exist, transition to **Vote reveal and round points**.

### Visual Composition
Cards occupy the central wide safe area; labels sit beside or below art. Clue uses the upper band and progress the lower band. Board nodes and constellations remain peripheral.

### Accessibility
Card numbers use the Card Number token, remain visible throughout, and do not rely on art recognition. Do not animate or reorder cards while voting. Reduced motion keeps the set static.

### Visual Reference
`docs/assets/visual-references/draft/votacion con cartas y pista.png`.

### Acceptance Criteria
Every card is identifiable by a readable stable number; no prohibited private information is visible; a couch viewer can understand that voting is still in progress.

## 5. Vote Reveal And Round Points

### Purpose
Reveal the narrator card, public vote distribution, and points earned in the completed round.

### User And Surface
All players view TV; no player acts; host may only use a clearly labelled continuation control after results are understood.

### Visible Information
Clue, card set, narrator-card label, public vote distribution per card, each player's round-point change, total-score scoreboard, and neutral result summary.

### Prohibited Information
Private hands, a mapping from individual voter to vote, private mobile confirmations, and token movement or updated board positions before the next screen.

### Primary Action
Host selects **Show board progress** after explicit confirmation; no automatic advance.

### States And Transitions
Reveal settles before score information appears; result-ready waits for host. Transition to **Board progress after scoring**, or to **End of game and tie** when the resolved phase says the game has ended.

### Visual Composition
Keep cards in the central wide safe area. Use labels and connectors outside the illustrations for narrator and vote relationships. Scores occupy the lower safe band; board tokens remain frozen.

### Accessibility
Narrator identification, vote counts, and points use text plus connectors or position, never color only. Reveal changes respect reduced motion through instant labels or brief fades.

### Visual Reference
`docs/assets/visual-references/draft/pantalla-revelacion-puntuacion-imaginarte.png`.

### Acceptance Criteria
The narrator card, vote totals, and round points are unambiguous; individual voters remain anonymous; no token has moved during this screen.

## 6. Board Progress After Scoring

### Purpose
Make the resolved score changes a shared board moment before the next round.

### User And Surface
All players view TV; no player acts; host proceeds only after progress completes or is skipped through a clear confirmation.

### Visible Information
25-node route, token constellations, recent `+N` markers, movement trails, updated positions, active/leader context where applicable, and a concise next-round status.

### Prohibited Information
Hands, discarded or refilled cards, private actions, votes, and recalculated scoring details beyond already-resolved round changes.

### Primary Action
Host selects **Next round** after the board state is legible.

### States And Transitions
Movement, settled-board, reduced-motion, and final-arrival states. Follow `07-progress-board.md` priority, constellation, safe-zone, and portal rules. Transition to **Waiting for storyteller** or **End of game and tie**.

### Visual Composition
The board is dominant. Tokens sit outside their nodes and open away from the central overlay zones; movement follows route segments, never crosses the centre. Use only one dominant advancement focus at once.

### Accessibility
Active and recently moved tokens use identity plus label, halo, and `+N`. Reduced motion changes position directly, shows a static arrival halo, and preserves legible score change.

### Visual Reference
No dedicated reference yet; validate against the board layouts defined in `07-progress-board.md`.

### Acceptance Criteria
All 25 nodes remain visible, no constellation invades the central safe area, and each score change is understandable without relying on motion or color.

## 7. End Of Game And Tie

### Purpose
Present the resolved winner or communicate that a final tiebreaker round is required.

### User And Surface
All players view TV; host alone starts the tiebreaker or returns the room to a new game flow.

### Visible Information
Final ranking, totals, target reached status, winner when resolved, or explicit tied-first-place message and tied players for a required tiebreaker. Portal and finalist constellation may be shown.

### Prohibited Information
A winner declaration before tie resolution, private hands, next-round card data, or private player choices.

### Primary Action
Host selects **Start tiebreaker** when required, or **Play again** after a resolved game, each with confirmation.

### States And Transitions
Resolved winner, tied-first-place, and host-confirmation states. Tiebreaker transitions to **Waiting for storyteller**; play again returns to **Room lobby** or the agreed new-game setup.

### Visual Composition
Use the portal as the ceremonial focus while preserving finalist token positions outside it. Ranking stays on a high-contrast Paper Warm panel and does not cover the portal silhouette.

### Accessibility
State the outcome in literal text, not celebration color alone. In a tie, say “Final tiebreaker round required”; portal light remains subtle and reduced-motion safe.

### Visual Reference
No dedicated reference yet; use the final-state constraints in `07-progress-board.md`.

### Acceptance Criteria
The screen never announces a winner for a first-place tie, clearly exposes the host-only continuation, and preserves readable finalist constellations and portal.

# Private Mobile

## 8. Join By Code Or QR

### Purpose
Let a player enter the room and establish a private display name.

### User And Surface
Prospective player on mobile; no TV or host action is required.

### Visible Information
Room-code entry or QR deep-link result, display-name field, join status, and concise privacy reassurance.

### Prohibited Information
Other players' hands, room-private administrative data, board view, or a full TV replica.

### Primary Action
Enter code/name and select **Join room**.

### States And Transitions
Code entry, deep-link prefilled, validation error, joining, and joined states; successful join transitions to **Lobby waiting**.

### Visual Composition
Paper Warm textured background; one compact form column, large code field, and full-width primary action above the safe bottom inset.

### Accessibility
Manual code is always available alongside QR; fields have visible labels and errors; keyboard focus and touch targets meet system requirements.

### Visual Reference
`docs/assets/visual-references/draft/hoja-referencia-lobby-imaginarte.png`.

### Acceptance Criteria
A player can join without scanning QR, receives a clear validation error for an invalid code, and sees no board or other private player data.

## 9. Lobby Waiting

### Purpose
Confirm membership and explain that the host controls game start.

### User And Surface
Joined player on mobile; host acts on TV.

### Visible Information
Player display name, room identity, joined roster if permitted publicly, host waiting message, and reconnecting status when relevant.

### Prohibited Information

### Primary Action
No primary game action; allow a reversible display-name correction only before game start if supported.

### States And Transitions
Waiting, host-starting, and reconnecting states; transitions to **Narrator: select card and write clue** or **Player: select a card for the clue** based on assigned role.

### Visual Composition
One reassurance message and identity block; no dashboard density. Maintain clear empty space and reserve the bottom inset.

### Accessibility
Use plain language such as “Waiting for the host to start”; status is text plus icon; no timed expiry.

### Visual Reference
`docs/assets/visual-references/draft/hoja-referencia-lobby-imaginarte.png` for surface and vertical rhythm.

### Acceptance Criteria
The player knows they joined successfully and cannot accidentally start or advance the shared game.

## 10. Narrator: Select Card And Write Clue

### Purpose
Allow the narrator to privately choose one hand card and compose the round clue.

### User And Surface
Narrator on mobile.

### Visible Information
Private hand, selected-card state, clue field, concise instruction, and one submit action enabled only when both inputs are valid.

### Prohibited Information

### Primary Action
Select a card, write the clue, then explicitly select **Send clue**.

### States And Transitions
Browsing, selected, editing, validation, submitting, and sent states. Before submission, card and clue are reversible. Successful submission transitions to **Waiting after sending a card**.

### Visual Composition
Hand uses a scrollable card area with `2:3` cards; selection treatment sits outside art. Keep clue and action in a stable lower control region above the viewport edge.

### Accessibility
Cards have accessible position labels; selection uses outline and text; clue field has label and error feedback; submit target is at least 44 px.

### Visual Reference
`docs/assets/visual-references/draft/hoja-referencia-movil-seleccion.png`.

### Acceptance Criteria
The narrator can revise both card and clue before sending; no selection commits implicitly; the selected illustration is never covered by text.

## 11. Player: Select A Card For The Clue

### Purpose
Let a guesser privately choose one hand card that fits the published clue.

### User And Surface
Guesser/player on mobile; narrator waits.

### Visible Information
Persistent clue, private hand, selected-card state, concise instruction, and explicit **Send card** action.

### Prohibited Information

### Primary Action
Select one card and explicitly select **Send card**.

### States And Transitions
Browsing, selected, validation, submitting, and sent. Selection remains reversible before sending; successful sending transitions to **Waiting after sending a card**.

### Visual Composition
Place clue at the top and keep it persistent during hand browsing. Cards dominate the scroll area; fixed action region stays clear of the bottom viewport edge.

### Accessibility
Clue is readable without relying on memory; card selection has focus, text, and outline; action is not blocked by browser or device chrome.

### Visual Reference
`docs/assets/visual-references/draft/hoja-referencia-movil-seleccion.png`.

### Acceptance Criteria
The clue remains visible, the player can change their selected card before confirmation, and no information identifies another submitted card.

## 12. Waiting After Sending A Card

### Purpose
Confirm that the private submission was received and prevent duplicate action.

### User And Surface
Narrator or guesser on mobile.

### Visible Information
Neutral success confirmation, current waiting message, clue when useful for phase context, and reconnecting state.

### Prohibited Information

### Primary Action
No game action.

### States And Transitions
Submission confirmed, waiting for others, voting-ready, and reconnecting. Transition to **Vote for a card** for guessers; narrator stays waiting through voting and then transitions to **Personal state after revelation**.

### Visual Composition
Use a compact confirmation panel on Paper Warm; do not show the submitted card again. Keep the screen calm rather than celebratory.

### Accessibility
Confirmation pairs Moss Signal with literal text and icon. The screen communicates that no further action is needed.

### Visual Reference
No dedicated reference yet; use `hoja-referencia-movil-seleccion.png` confirmation treatment without exposed card art.

### Acceptance Criteria
The player receives immediate confirmation, cannot submit twice, and the screen does not expose the private selected card.

## 13. Vote For A Card

### Purpose
Enable each guesser to privately identify the narrator card from the public submitted-card set.

### User And Surface
Guesser/player on mobile; narrator does not vote.

### Visible Information
Persistent clue, randomized card set with stable numbers matching TV, own submitted card visible but disabled with explanation, chosen-vote state, and explicit **Confirm vote** action.

### Prohibited Information

### Primary Action
Select an eligible card and explicitly select **Confirm vote**.

### States And Transitions
Browsing, selected vote, confirmation, submitted, and reconnecting. Vote selection remains reversible until confirmation; successful confirmation transitions to **Confirmation and waiting after voting**.

### Visual Composition
Cards occupy the main scroll area at `2:3`; numbers are adjacent and persistent. Own card retains recognizable art but has a muted veil, unavailable label, and blocked interaction. Keep action above bottom inset.

### Accessibility
Explain disabled own card in text, not color only. Card numbers are large and stable; selected vote uses focus/outline/text; confirmation is required and touch targets are at least 44 px.

### Visual Reference
`docs/assets/visual-references/draft/controlador-votacion-privado.png`.

### Acceptance Criteria
The player cannot vote for their own card, cannot cast more than one vote, can revise selection before confirming, and cannot infer the narrator card or other votes.

## 14. Confirmation And Waiting After Voting

### Purpose
Confirm the vote privately and wait for public revelation without exposing it.

### User And Surface
Guesser/player on mobile; narrator remains waiting.

### Visible Information
Neutral “Vote sent” confirmation and waiting status.

### Prohibited Information

### Primary Action
No game action.

### States And Transitions
Vote confirmed, waiting for reveal, reconnecting, and reveal-ready states; transition to **Personal state after revelation**.

### Visual Composition
Use a single calm confirmation panel; no card thumbnail, number, or visual cue that could reveal the user's vote to nearby players.

### Accessibility
Use literal confirmation text, icon, and non-color state cue. Do not use an auto-dismiss that could be missed.

### Visual Reference
No dedicated reference yet; derive the privacy-safe confirmation pattern from `controlador-votacion-privado.png`.

### Acceptance Criteria
The player knows the vote was received while anyone viewing the phone cannot identify its target.

## 15. Personal State After Revelation

### Purpose
Give each player a private, concise account of their round outcome after TV revelation.

### User And Surface
Narrator or player on mobile; host progresses the TV.

### Visible Information
Player's round points, updated total if available, role-aware explanation, and a waiting message for board progress or next round.

### Prohibited Information

### Primary Action
No game action; optional acknowledgement must not control room progression.

### States And Transitions
Results available, board-progress waiting, next-round waiting, final-result waiting, and reconnecting. Transition to the next role-specific mobile screen or **Lobby waiting** for a new game.

### Visual Composition
One personal summary panel with high-contrast score hierarchy. Do not recreate the TV vote diagram or full board.

### Accessibility
State points and reason in plain language; do not make correctness depend only on decorative badges or color; support reduced-motion with no required animation.

### Visual Reference
No dedicated reference yet; use the score clarity of `pantalla-revelacion-puntuacion-imaginarte.png` at mobile density.

### Acceptance Criteria
The player can understand their own points and role outcome without seeing private information belonging to another player or receiving a phase-control action.

## 16. Errors, Disconnection, And Reconnection

### Purpose
Explain recoverable failures, preserve player confidence, and return the mobile controller to its authoritative current screen.

### User And Surface
Any mobile player; TV may show only a public neutral reconnecting status when necessary; host may receive a room-level recovery instruction.

### Visible Information
Plain-language error, retry or reconnect action, connection status, preserved local draft/selection only when safely available, and post-reconnect current phase.

### Prohibited Information

### Primary Action
Select **Retry** or **Reconnect**; retrying a non-idempotent submission must first resolve against the authoritative phase rather than duplicate it.

### States And Transitions
Offline, reconnecting, recovered, invalid/expired room, and unrecoverable action states. On recovery, transition to the current role- and phase-appropriate screen; if the action already completed, show its privacy-safe waiting confirmation.

### Visual Composition
Use an inline or modal Paper Deep error panel with Coral Spark as supporting accent, never as the sole meaning. Keep controls comfortably above the bottom edge.

### Accessibility
Errors name the problem and next step; status is text plus icon; focus moves to the error; retry targets meet 44 px; no essential message is transient.

### Visual Reference
No dedicated reference yet; all references must reject edge-clipped controls and content outside the viewport.

### Acceptance Criteria
Recovery never duplicates a vote or submission, returns the player to the correct current state, and never reveals another player's private information.

# Visual Reference Register

Reference paths use `docs/assets/visual-references/draft/` and `docs/assets/visual-references/approved/`. Files may be absent until created; `draft` references guide review and are not implementation approval.

| File | State | Screen represented | Decisions validated | Known errors not to implement |
| --- | --- | --- | --- | --- |
| `hoja-referencia-lobby-imaginarte.png` | draft | TV room lobby and mobile join | Room-code/QR hierarchy, public roster, spacious board context, code/name flow, primary-action placement | Host control and supporting labels overlap at the TV bottom edge; QR remains a visual placeholder; validate mobile safe-bottom spacing |
| `votacion con cartas y pista.png` | draft | TV voting | Central card field, stable numbers, clue/progress bands, board periphery | Validate card-number and route legibility from 2-3 m; do not infer any player identity or vote choice from the reference |
| `pantalla-revelacion-puntuacion-imaginarte.png` | draft | TV vote reveal and round points | Narrator-card marking, public distribution, round points, frozen tokens | It maps named players to individual vote targets, which violates the privacy rule; cumulative totals are absent; do not implement either behavior as shown |
| `controlador-votacion-privado.png` | draft | Mobile voting | Disabled own card, stable numbers, explicit confirmation, private controller density | Selection labels overlap cards; validate scroll and safe-bottom spacing; hide the chosen card after confirmation |
| `hoja-referencia-movil-seleccion.png` | draft | Mobile narrator/guesser card selection | `2:3` hand, reversible selection, persistent clue, submit placement | Selection labels overlap cards; reserve distinct space for the clue field and avoid controls against the bottom edge |

# Visibility Matrix

| Information or action | Public TV | Private mobile | Host only |
| --- | --- | --- | --- |
| Room code and QR | Yes | Join input/deep link only | No special restriction |
| Joined-player roster | Yes | Yes, if roster is enabled | No special restriction |
| Start game and phase continuation | Visible only as host control | No | Yes |
| Narrator identity after assignment | Yes | Yes | No special restriction |
| Private hand and selected card | Never | Owner only | Never unless host is that player on their own mobile |
| Narrator clue before send | Never | Narrator only | Never |
| Published clue | Yes | Yes | No special restriction |
| Aggregate send/vote progress | Yes | Optional neutral waiting only | No special restriction |
| Pending player identity | Never | Never | Never |
| Submitted-card ownership before reveal | Never | Never | Never |
| Own submitted card during voting | Never | Owner only, disabled | Never |
| Individual vote and voter identity | Never | Voter only until confirmation; then hidden | Never |
| Narrator card and public vote distribution after reveal | Yes | Public result may be summarized personally | No special restriction |
| Round points and total scores | Yes | Own outcome and total | No special restriction |
| Board positions and token progress | Yes | Never as full board | No special restriction |

# Phase Transition Table

| Game phase | TV screen | Narrator mobile | Guesser mobile | Host action |
| --- | --- | --- | --- | --- |
| Room setup | Room lobby | Join or lobby waiting | Join or lobby waiting | Start game |
| Narrator choosing | Waiting for storyteller | Select card and write clue | Lobby waiting | None |
| Guesser submitting | Clue and submission progress | Waiting after sending a card | Select a card for the clue, then waiting | None |
| Voting | Voting | Waiting after sending a card | Vote for a card, then confirmation/waiting | None |
| Reveal and scoring | Vote reveal and round points | Personal state after revelation | Personal state after revelation | Show board progress |
| Score progress | Board progress after scoring | Personal state after revelation | Personal state after revelation | Next round |
| End condition resolved | End of game and tie | Personal state after revelation | Personal state after revelation | Start tiebreaker or play again |
| Tiebreaker required | End of game and tie | Lobby/waiting then role screen | Lobby/waiting then role screen | Start tiebreaker |

# Prioritized Visual Corrections

1. Reject any generated reference with overlapping text, labels over card illustrations, controls against the bottom edge, or content outside the viewport.
2. Produce and approve TV voting and reveal references that prove privacy boundaries and preserve the board central safe area.
3. Validate TV typography, card numbers, node visibility, and active token recognition at 2-3 m.
4. Validate mobile one-action flows, `44 x 44 px` minimum targets, safe bottom spacing, and explicit confirmation on physical devices.
5. Validate board constellations, labels, and movement against the 25-node safe zones with 3-8 players and reduced motion.

# Glossary

| Term | Definition |
| --- | --- |
| Narrator | The active storyteller who chooses a card and supplies the clue. |
| Player | A participant; outside their narrator turn, they are a guesser. |
| Hand | The private set of playable cards held by one player. |
| Submission | A narrator's chosen card and clue, or a guesser's chosen matching card, sent for the round. |
| Vote | One guesser's private selection of the card believed to be the narrator's. |
| Reveal | The public phase showing the narrator card, vote distribution, and round points. |
| Node | One of the 25 visible anchors on the TV progress route. |
| Token | A player's seed-comet marker, linked outside a node by a short thread. |
| Constellation | The outward arrangement of multiple tokens sharing one node. |

# Relationship To Existing Specifications

`06-design-system.md` governs palette, typography, spacing, card geometry, control behavior, contrast, focus, motion, and the public-TV/private-mobile visual distinction used here. This document applies those rules screen by screen without defining implementation components.

`07-progress-board.md` governs the 25-node “Between Sky and Earth” route, central overlay safe zones, token identities, anchored constellations, state priorities, motion, and final portal behavior. This document determines when the board is contextual, when cards occupy its centre, and that token advancement occurs only on the post-scoring board-progress screen.
