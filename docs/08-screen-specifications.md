# Screen Specifications

## Scope And Non-Negotiable Boundaries

This document is the screen source of truth for the shared TV and private mobile controllers. It specifies presentation and interaction only. Game rules, scoring, role rotation, and phase authority remain outside the interface.

- TV is public, spacious, and readable from 2-3 metres. After the server confirms the game start, it shows the progress route as board context. The upper band is available to the route; phase/storyteller information appears in the central floating window, while live connection status appears in the lower-right corner. Central safe areas remain available for game content.
- Mobile is private and uses a Night Wash stage with a restrained ornamental gold frame; Paper Warm is reserved for readable cards, clues, fields, and content panels. It never shows the full board and presents one clear primary action. A selection remains reversible until explicit confirmation.
- Playable cards remain portrait `2:3`; no title or text overlays the illustration. Voting numbers sit outside the art.
- While voting remains open, TV never shows hands, any selected or submitted-card owner, the storyteller card identity, individual votes, or which players made pending choices. After the server locks all votes, the dedicated vote-reveal state may show who voted for each card while keeping the storyteller card hidden until a separate TV action.
- During voting and revelation, cards occupy the board's central free area. During revelation, tokens do not move; token movement belongs only to board progress.
- Major ceremonial windows follow the layered construction in `06-design-system.md`: CSS Night Wash surface, independent transparent texture PNG, reusable corner ornament, and CSS frame rules. The complete window mockup is a reference, not a stretchable runtime image.
- The screen mockups in `docs/assets/visual-references/mockups/` guide composition and visual tone; they do not override this document's behavior, privacy, accessibility, or safety rules, and must not be rendered as complete screen images.

# Shared TV

## TV Home: Create A Room

### Purpose
Give the host a clear starting point to create a room and continue into the room lobby.

### User And Surface
Host at the shared TV before a room has been created.

### Visible Information
The **ImaginArte** title, one **Crear una sala** action, and the supporting line **Prepara una nueva experiencia**.

### Prohibited Information
An **Open a room** option, a choice between multiple experience types, lobby roster or QR content before a room exists, and any game-private information.

### Primary Action
Host selects **Crear una sala**. Creation and saved-room recovery follow `specs/02-tv-home-room-creation.md`; a recoverable network failure offers retry rather than silently creating another room.

### States And Transitions
Ready, creating, conflict/error, recovering a saved room, and retry states. A successfully created or recovered room transitions to **Room lobby**. Creation errors remain accessible and allow retry.

### Visual Composition
Use the scenic TV background as a full-bleed stage, with the title and a single centered gold-framed action. Build the frame from the reusable Night Wash surface, texture, ornament, and CSS rules in `06-design-system.md`; do not render the complete mockup as a screen image.

### Accessibility
The single action is keyboard operable with persistent visible focus. Loading and error states use literal text and accessible status announcements; the action remains legible at TV distance.

### Visual Reference
`docs/assets/visual-references/mockups/01-inicio-seleccion-experiencia-tv.png`. Screen copy and interaction are governed by `specs/02-tv-home-room-creation.md`: omit **Abrir una sala**, **¿Cómo quieres comenzar?**, and **Entre cielo y tierra** as the title.

### Acceptance Criteria
The TV home presents **ImaginArte** and exactly one room-creation action, with no open-room choice; room creation or recovery leads to the room lobby without duplicating a room after a recoverable failure.

## 1. Room Lobby

### Purpose
Bring players into a room and let the host begin when the room is ready.

### User And Surface
All players view the shared TV; only the host acts on the TV.

### Visible Information
Room code, QR code, joined-player names and public avatars from the authoritative room snapshot, player count, configured target score, and host-ready status. The TV maps each player's stable `avatarId` to its portrait in the atlas; it does not assign or persist avatar identities locally. Mobile avatar selection follows SPEC 06. The progress route is hidden while the room is waiting for players.

### Prohibited Information
Hands, player card identities, future storyteller, private session details, or a start control for non-hosts.

### Primary Action
Host selects **Start game** and explicitly confirms it.

### States And Transitions
Empty, joining, ready, and host-confirmation states; after the server confirms the start, transition to **Waiting for storyteller** and reveal the progress route. If a player disconnects, retain their name with a public neutral reconnecting status. After the TV reconnects, load a fresh room snapshot and render each avatar from that snapshot rather than inventing or replaying local assignments.

### Visual Composition
Use the scenic illustrated TV background with a dark, high-contrast content window. Room code and QR form the dominant central stage; roster sits in a quiet side or lower band. Keep the progress route hidden until the server confirms the start.

### Accessibility
Room code uses large tabular figures; QR has the manual-code alternative; roster status uses text and icon, not color alone; host control has a persistent focus state.

### Visual Reference
`docs/assets/visual-references/mockups/02-lobby-sala-tv.png`.

### Acceptance Criteria
From 2-3 m, a guest can read the room code, see how many players joined, and identify the host-only start action without seeing any private game data. The progress route remains hidden until the server confirms the start. Each roster portrait matches its `avatarId` in the authoritative room snapshot and remains consistent after room updates or TV reconnection.

## 2. Waiting For Storyteller

### Purpose
Announce the active narrator and hold the room while they privately choose a card and clue.

### User And Surface
All players view TV; the narrator acts only on their mobile; host has no phase-advance action.

### Visible Information
Narrator name, role label, concise waiting instruction in the central floating window, and the numbered route as a persistent background layer after the start is confirmed. Live connection status appears in the lower-right corner. Player tokens and their state effects are separate from the route-only implementation slice in SPEC 07.

### Prohibited Information
Narrator hand, selected card, clue before submission, selection progress, and any other player's private state.

### Primary Action
No TV action; the narrator completes the private mobile action.

### States And Transitions
Waiting and narrator-reconnecting states; after valid narrator submission, transition to **Clue and submission progress**.

### Visual Composition
Remove the top eyebrow **ImaginArte · Sala familiar** and the screen heading **El narrador prepara la historia** after the start is confirmed. Present phase, narrator identity, and waiting instruction in the central floating window. The upper band is available to the peripheral route; keep nodes and connectors outside the central safe zones. Place the live connection status at the lower-right corner without covering a route node.

### Accessibility
Use a literal message such as “Ana is choosing a card and clue” in the central floating window; role is conveyed by text, label, halo, and token identity. The lower-right connection status remains textual and accessible.

### Visual Reference
`docs/assets/visual-references/mockups/03-esperando-al-narrador-recorrido-v4-avatares-tv.png`.

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
`docs/assets/visual-references/mockups/04-pista-progreso-envios-ruta-tv-ventana-oscura.png`.

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
Voting, aggregate-progress, all-voted, and reconnecting states. Card order and numbers remain stable. When all eligible votes exist, the server locks voting and transitions to **Vote reveal and round points**. Individual votes are not sent in public snapshots before this transition.

### Visual Composition
Cards occupy the central wide safe area; labels sit beside or below art. Clue uses the upper band and progress the lower band. Board nodes and constellations remain peripheral.

### Accessibility
Card numbers use the Card Number token, remain visible throughout, and do not rely on art recognition. Do not animate or reorder cards while voting. Reduced motion keeps the set static.

### Visual Reference
`docs/assets/visual-references/mockups/05-votacion-cartas-pista-recorrido-tv.png`.

### Acceptance Criteria
Every card is identifiable by a readable stable number; no prohibited private information is visible; a couch viewer can understand that voting is still in progress.

## 5. Vote Reveal And Round Points

### Purpose
Reveal the voter-to-card mapping first, then reveal the narrator card and points earned in the completed round.

### User And Surface
All players view TV. After votes are locked, the host uses a clearly labelled TV action to reveal the storyteller card; no guesser can act on or change a vote.

### Visible Information
Before the narrator card is revealed: clue, card set, and the name of each guesser next to the card they voted for. Keep the narrator card unmarked. After the explicit reveal action: narrator-card label, public vote distribution, each player's round-point change, total-score scoreboard, and neutral result summary.

### Prohibited Information
Private hands, private mobile confirmations, token movement or updated board positions before the next screen, and the narrator-card identity before the explicit reveal action.

### Primary Action
In the first reveal state, host selects **Reveal storyteller card**. The server rejects this action until all votes are locked. After the card reveal and score resolution, host selects **Show board progress**; no automatic advance.

### States And Transitions
The vote-reveal state shows the voter-to-card mapping while withholding the storyteller-card marker. The explicit reveal action transitions to the card-revealed state; score information appears only after that transition. Result-ready waits for host. Transition to **Board progress after scoring**, or to **End of game and tie** when the resolved phase says the game has ended.

### Visual Composition
Keep cards in the central wide safe area. Use labels and connectors outside the illustrations for narrator and vote relationships. Scores occupy the lower safe band; board tokens remain frozen.

### Accessibility
Narrator identification, vote counts, and points use text plus connectors or position, never color only. Reveal changes respect reduced motion through instant labels or brief fades.

### Visual Reference
`docs/assets/visual-references/mockups/06-revelacion-puntuacion-recorrido-tv.png`.

### Acceptance Criteria
Before all votes are locked, no individual vote or voter identity is public. After the lock, the vote-reveal state shows the voter-to-card mapping without marking the narrator card. Only the explicit **Reveal storyteller card** action marks that card; scoring remains unavailable until then. No token moves during this screen.

## 6. Board Progress After Scoring

### Purpose
Make the resolved score changes a shared board moment before the next round.

### User And Surface
All players view TV; no player acts; host proceeds only after progress completes or is skipped through a clear confirmation.

### Visible Information
Numbered route whose node count matches the room's target score (15, 25, or 35), with visible labels `1` through the target, token constellations, recent `+N` markers, movement trails, updated positions, active/leader context where applicable, and a concise next-round status.

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
`docs/assets/visual-references/mockups/07-progreso-tablero-tv.png`; validate node, token, route, and portal placement against `07-progress-board.md`.

### Acceptance Criteria
All route nodes for the selected target score and their visible numbers remain legible, no constellation invades the central safe area, and each score change is understandable without relying on motion or color.

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
`docs/assets/visual-references/mockups/08-final-partida-ganador-tv.png` for a resolved winner only. There is no tie-state mockup; follow the tie and portal constraints in `07-progress-board.md`.

### Acceptance Criteria
The screen never announces a winner for a first-place tie, clearly exposes the host-only continuation, and preserves readable finalist constellations and portal.

# Private Mobile

## 8. Join By Code Or QR

### Purpose
Let a player join by room code, choose an available avatar, and establish a private display name.

### User And Surface
Prospective player on mobile; no TV or host action is required.

### Visible Information
Room-code entry or QR deep-link result, display-name field, the 16 avatar options with Spanish accessible names and availability status, a large preview of the player's own selection, join status, and concise privacy reassurance. After a successful join, confirm the player's own server-saved avatar, display name, room code, and waiting message.

### Prohibited Information
Other players' names or roster, which player chose an unavailable avatar, other players' hands, room-private administrative data, board view, or a full TV replica. Use other players' avatar IDs only to disable those choices; do not render a roster or attribute an avatar to a person.

### Primary Action
Enter a valid code and display name, explicitly choose an available avatar, and select **Join room**. Do not submit until an available avatar is selected.

### States And Transitions
Code entry or deep-link prefilled; room availability loading, loaded, and error states; avatar selection and validation; joining; and joined confirmation. A public room snapshot determines which avatar choices are unavailable without exposing the roster, while the server enforces avatar uniqueness within the room. If a simultaneous join takes the selected avatar, handle HTTP 409 by refreshing availability, preserving the entered code and name, and asking the player to choose another. If a join response is uncertain, query the saved player identity first and reuse the same `playerId` and `avatarId` when retrying. A successful join confirms the avatar from the authoritative server snapshot and transitions to **Lobby waiting**; avatar editing is not offered after joining.

### Visual Composition
Night Wash stage with a restrained gold frame; place the room-code and display-name fields on clear Paper Warm surfaces in one compact column. Follow them with a responsive grid of 16 atlas portraits and a large preview of the selected portrait. Keep the full-width primary action and all avatar choices within the scrollable, safe-area-aware stage.

### Accessibility
Manual code is always available alongside QR. Use a labelled radio group with Spanish avatar names, visible keyboard focus, and text states for selected, available, and unavailable choices; do not rely on color alone. Announce loading, errors, conflicts, and join confirmation. Avatar touch targets are at least 44 × 44 px, and the scrollable content remains usable above the safe-area inset.

### Visual Reference
`docs/assets/visual-references/mockups/09-unirse-sala-movil-avatar-selector-sin-qr.png`. The mockup is a visual reference; this screen contract defines the required 16-option selector, privacy boundary, availability, conflict recovery, and server-confirmed identity.

### Acceptance Criteria
A player can join without scanning QR, receives a clear validation error for an invalid code, and cannot submit without choosing an available avatar. Each avatar is unique within a room, with the server as the final authority. The screen shows availability without exposing a roster or names, refreshes after an avatar conflict while preserving the code and name, and confirms the server-saved avatar with the player's name and room code. An uncertain join result recovers the same identity before retrying. No board or other private player data is shown.

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
Night Wash stage with the reusable gold frame and a quiet identity/waiting panel. Keep one reassurance message and identity block; avoid dashboard density, retain clear empty space, and reserve the bottom inset. Do not show the full board.

### Accessibility
Use plain language such as “Waiting for the host to start”; status is text plus icon; no timed expiry.

### Visual Reference
`docs/assets/visual-references/mockups/10-lobby-jugador-espera-anfitrion.png` for identity, roster, and waiting hierarchy.

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
Use the Night Wash framed mobile stage. The hand uses a scrollable card area with `2:3` cards; selection treatment sits outside the illustration. Place the clue field on Paper Warm and keep clue and action in a stable lower control region above the viewport edge.

### Accessibility
Cards have accessible position labels; selection uses outline and text; clue field has label and error feedback; submit target is at least 44 px.

### Visual Reference
`docs/assets/visual-references/mockups/11-turno-narrador-movil.png`. Do not reproduce selection labels that overlap a card or its illustration.

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
Use the Night Wash framed mobile stage. Place the clue in a persistent Paper Warm panel at the top; cards dominate the scroll area, and the full-width gold action stays above the bottom safe inset.

### Accessibility
Clue is readable without relying on memory; card selection has focus, text, and outline; action is not blocked by browser or device chrome.

### Visual Reference
`docs/assets/visual-references/mockups/12-turno-jugador-movil.png`. Keep selection labels outside card illustrations and do not copy any label overlap.

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
Use a compact Paper Warm confirmation panel on the Night Wash framed stage; do not show the submitted card again. Keep the screen calm rather than celebratory.

### Accessibility
Confirmation pairs Moss Signal with literal text and icon. The screen communicates that no further action is needed.

### Visual Reference
`docs/assets/visual-references/mockups/13-esperando-tras-enviar-carta-movil.png`; use its confirmation/waiting hierarchy without exposed card art.

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
Use the Night Wash framed mobile stage. Cards occupy the main scroll area at `2:3`; numbers are adjacent and persistent. Own card retains recognizable art but has a muted veil and blocked interaction; place its unavailable explanation outside the illustration. Keep the selected state and action clear of the art and above the bottom inset.

### Accessibility
Explain disabled own card in text, not color only. Card numbers are large and stable; selected vote uses focus/outline/text; confirmation is required and touch targets are at least 44 px.

### Visual Reference
`docs/assets/visual-references/mockups/14-votar-carta-movil.png`. Do not copy the overlapping own-card or selection labels; keep all labels outside the illustrations.

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
Use a single calm Paper Warm confirmation panel on the Night Wash framed stage; no card thumbnail, number, or visual cue that could reveal the user's vote to nearby players.

### Accessibility
Use literal confirmation text, icon, and non-color state cue. Do not use an auto-dismiss that could be missed.

### Visual Reference
`docs/assets/visual-references/mockups/15-esperando-tras-votar-movil.png`; preserve the privacy-safe waiting state and keep the vote target hidden after confirmation.

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
One Paper Warm personal-summary panel on the Night Wash framed stage, with a high-contrast score hierarchy. Do not recreate the TV vote diagram or full board.

### Accessibility
State points and reason in plain language; do not make correctness depend only on decorative badges or color; support reduced-motion with no required animation.

### Visual Reference
`docs/assets/visual-references/mockups/16-resultado-personal-movil.png` for personal score hierarchy only; do not carry over public vote mappings or the full TV board.

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
Use the Night Wash framed mobile stage with an inline or modal Paper Deep error panel. Use Copper Spark as a supporting accent, never as the sole meaning; keep controls comfortably above the bottom safe inset.

### Accessibility
Errors name the problem and next step; status is text plus icon; focus moves to the error; retry targets meet 44 px; no essential message is transient.

### Visual Reference
No dedicated error/reconnection mockup. Use the mobile shell and calm confirmation treatment established by `06-design-system.md` and mockups 13 and 15; reject edge-clipped controls and content outside the viewport.

### Acceptance Criteria
Recovery never duplicates a vote or submission, returns the player to the correct current state, and never reveals another player's private information.

# Visual Reference Register

The screen mockups live in `docs/assets/visual-references/mockups/`. They are visual guidance, not implementation approval or a source of game behavior. The constraints in this document and the visual rules in `06-design-system.md` and `07-progress-board.md` take precedence.

| File | Screen represented | Visual direction | Constraints or deviations not to reproduce |
| --- | --- | --- | --- |
| `01-inicio-seleccion-experiencia-tv.png` | TV home before room creation | Scenic stage, ImaginArte title, one prominent framed action | `specs/02-tv-home-room-creation.md` governs the screen: show only **Crear una sala**; omit **Abrir una sala**, **¿Cómo quieres comenzar?**, and **Entre cielo y tierra** as title. |
| `02-lobby-sala-tv.png` | TV room lobby | Room-code/QR hierarchy, public roster, spacious scenic backdrop, host action | Replace sample values and QR artwork with live data. Keep manual code entry available, reconnect status textual and icon-supported, and host start explicitly confirmed. Do not show the progress route before the server confirms the start. |
| `03-esperando-al-narrador-recorrido-v4-avatares-tv.png` | TV waiting for storyteller | Narrator identity, waiting message, contextual active token | Do not show the narrator's hand, card, clue, or private selection progress; keep the central stage clear. |
| `04-pista-progreso-envios-ruta-tv-ventana-oscura.png` | TV clue and submission progress | Framed clue window, aggregate submission count, quiet board periphery | Progress must remain aggregate; do not mark or identify pending players or expose submitted cards. |
| `05-votacion-cartas-pista-recorrido-tv.png` | TV voting | Central card set, stable numbers, clue and aggregate vote progress | Keep order/numbers fixed; do not reveal ownership, votes, voter identity, pending-voter identity, or the narrator card. Validate legibility from 2-3 m. |
| `06-revelacion-puntuacion-recorrido-tv.png` | TV vote reveal and round points | Narrator-card marking, public per-card distribution, round points, cumulative scoreboard | Names under cards map voters to vote targets and violate privacy; replace them with anonymous counts per card. Cumulative totals are present in the mockup and remain part of the required scoreboard. Freeze tokens during this screen. |
| `07-progreso-tablero-tv.png` | TV board progress after scoring | Scenic board, target-sized numbered route, focused movement summary, next-round action | Compose the route in code using reusable SVG primitives; do not render the mockup as a complete screen. Apply route, node, constellation, central-safe-zone, and reduced-motion rules from `07-progress-board.md`; do not move tokens during reveal. |
| `08-final-partida-ganador-tv.png` | TV resolved winner | Ceremonial winner panel, final ranking, portal context | This represents a resolved winner, not a first-place tie. Preserve portal and finalist positions per `07-progress-board.md`; keep host-only **Play again** and its confirmation. |
| `09-unirse-sala-movil-avatar-selector-sin-qr.png` | Mobile join by code | Compact code/name flow and framed mobile stage | QR is not required for manual join. The mockup is visual reference only; SPEC 06 defines avatar selection, availability, privacy, and confirmation behavior. |
| `10-lobby-jugador-espera-anfitrion.png` | Mobile lobby waiting | Player identity, optional roster, host-waiting status | Show the roster only if enabled; do not add a full board or a player action that starts/advances the room. |
| `11-turno-narrador-movil.png` | Mobile narrator card and clue selection | Private hand, clue field, explicit submission action | Selection labels intrude on card edges/art; place them outside the illustration. Keep the clue/action controls clear of the bottom inset. |
| `12-turno-jugador-movil.png` | Mobile guesser card submission | Persistent clue, private hand, explicit send action | Keep selection labels outside card art and preserve a clear safe-bottom area for the action. |
| `13-esperando-tras-enviar-carta-movil.png` | Mobile waiting after card submission | Calm confirmation and waiting status | Do not show the submitted card or imply that another action is required. |
| `14-votar-carta-movil.png` | Mobile private voting | Stable card numbers, disabled own card, explicit confirmation | Labels cover card edges/art in the mockup; move them outside the illustrations. Hide the selected target after confirmation. |
| `15-esperando-tras-votar-movil.png` | Mobile waiting after voting | Neutral vote confirmation and waiting status | Do not show the vote target, card thumbnail, or number after confirmation. |
| `16-resultado-personal-movil.png` | Mobile personal result | Personal score and role-aware explanation | Keep results limited to the current player; do not show the TV vote mapping or full board. |
| No dedicated mockup | Mobile errors, disconnection, and reconnection | Reuse the framed mobile stage and calm status-panel language | Preserve authoritative recovery, prevent duplicate submissions, and keep all errors/statuses readable and non-transient. |

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
| Individual vote and voter identity | Hidden while voting; public after all votes are locked | Voter only until confirmation; then hidden | TV may show after all votes are locked |
| Voter-to-card mapping | Hidden while voting; public during Vote reveal | Never after confirmation | TV may show after all votes are locked |
| Narrator card and anonymous aggregate vote counts per card after reveal | Yes | Public result may be summarized personally | No special restriction |
| Round points and total scores | Yes | Own outcome and total | No special restriction |
| Board positions and token progress | Yes | Never as full board | No special restriction |

# Phase Transition Table

| Game phase | TV screen | Narrator mobile | Guesser mobile | Host action |
| --- | --- | --- | --- | --- |
| Room creation | TV home, then Room lobby | Join by code or QR once a room code is available | Join by code or QR once a room code is available | Create a room |
| Room setup | Room lobby | Lobby waiting | Lobby waiting | Start game |
| Narrator choosing | Waiting for storyteller | Select card and write clue | Lobby waiting | None |
| Guesser submitting | Clue and submission progress | Waiting after sending a card | Select a card for the clue, then waiting | None |
| Voting | Voting | Waiting after sending a card | Vote for a card, then confirmation/waiting | None |
| Reveal and scoring | Vote reveal and round points | Personal state after revelation | Personal state after revelation | Show board progress |
| Score progress | Board progress after scoring | Personal state after revelation | Personal state after revelation | Next round |
| End condition resolved | End of game and tie | Personal state after revelation | Personal state after revelation | Start tiebreaker or play again |
| Tiebreaker required | End of game and tie | Lobby/waiting then role screen | Lobby/waiting then role screen | Start tiebreaker |

# Prioritized Visual Corrections

1. Treat all files in `docs/assets/visual-references/mockups/` as visual guidance only; this specification and `06-design-system.md`/`07-progress-board.md` govern behavior and constraints.
2. Keep the narrator card hidden while the vote-reveal state shows voter-to-card mapping; reveal the narrator card only after the explicit TV action, while retaining round changes and cumulative totals.
3. Keep card selection and disabled-state labels outside illustrations; reject clipped controls, bottom-edge collisions, and content outside the viewport.
4. Validate TV typography, card numbers, node visibility, and active-token recognition at 2-3 m.
5. Validate mobile one-action flows, `44 x 44 px` minimum targets, safe-bottom spacing, and explicit confirmation on physical devices.
6. Validate board constellations, labels, and movement against the 15-, 25-, and 35-node safe zones with 3-8 players and reduced motion; do not infer a tie layout from the resolved-winner mockup.

# Glossary

| Term | Definition |
| --- | --- |
| Narrator | The active storyteller who chooses a card and supplies the clue. |
| Player | A participant; outside their narrator turn, they are a guesser. |
| Hand | The private set of playable cards held by one player. |
| Submission | A narrator's chosen card and clue, or a guesser's chosen matching card, sent for the round. |
| Vote | One guesser's private selection of the card believed to be the narrator's. |
| Reveal | The public phase showing the narrator card, vote distribution, and round points. |
| Node | One visible anchor on the TV progress route; the route has as many nodes as the room's target score. |
| Token | A player's seed-comet marker, linked outside a node by a short thread. |
| Constellation | The outward arrangement of multiple tokens sharing one node. |

# Relationship To Existing Specifications

`06-design-system.md` governs palette, typography, spacing, card geometry, control behavior, contrast, focus, motion, and the public-TV/private-mobile visual distinction used here. This document applies those rules screen by screen without defining implementation components.

`07-progress-board.md` governs the 15-, 25-, and 35-node “Between Sky and Earth” routes, central overlay safe zones, token identities, anchored constellations, state priorities, motion, and final portal behavior. This document determines when the board is contextual, when cards occupy its centre, and that token advancement occurs only on the post-scoring board-progress screen.
