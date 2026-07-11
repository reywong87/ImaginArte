# ImaginArte Design System

## Purpose

This guide defines the visual language for ImaginArte: a family storytelling game played together on a TV and controlled privately from phones. It guides future design and implementation work; it does not prescribe application components or game behavior.

All choices marked **Initial proposal** are pragmatic defaults that can be validated in playtesting.

## Visual And Emotional Principles

- Make each card an invitation to tell a different story. Illustration is always the emotional center of the experience.
- Balance poetry with clarity. The interface must be immediately understandable for children and adults sharing a room.
- Use a warm, quiet paper world around vivid imaginative moments. Negative space is an intentional part of the composition.
- Build mystery through metaphor, contrast, and suggestion, never through fear, threat, gore, or visual aggression.
- Favor handcrafted imperfection: translucent watercolor, organic pigment blooms, light paper grain, and fine irregular ink contours.
- Give each illustration a limited palette with one or two saturated narrative accents. Avoid uniformly colorful or rainbow-like scenes.
- Use light versus dark, warm versus cool, and ordinary versus impossible as visual story devices.
- Keep public TV information calm and theatrical; keep private mobile controls direct, compact, and reassuring.
- Preserve privacy by never exposing a player's hand or pending choice on the shared screen.

## Base Palette

**Initial proposal.** The base palette is a neutral paper-and-ink system. The thematic illustration palette supplies most expressive color.

| Semantic name | Hex | Use rule |
| --- | --- | --- |
| Paper Warm | `#FBF7EE` | Default page and TV canvas. Use as the primary field around art. |
| Paper Deep | `#F0E7D6` | Quiet panels, card backs, large grouped areas, and subtle separation from the canvas. |
| Ink Primary | `#27262B` | Primary text, essential icons, and illustration contour reference. |
| Ink Soft | `#5E5A61` | Secondary text and non-critical metadata. Do not use for small text on Paper Deep without contrast verification. |
| Mist | `#DED9D4` | Dividers, inactive outlines, and restrained surface separation. |
| Moon Gold | `#E5A83B` | Positive progress, selected emphasis, and one high-attention accent per view. |
| Coral Spark | `#D9685D` | Errors, destructive choices, or a warm narrative accent. Never use as the only error indicator. |
| Sky Signal | `#4F82A5` | Informational state, links, and cool interactive emphasis. |
| Moss Signal | `#5C8065` | Confirmed, ready, and completed states. Never use as the only confirmation signal. |
| Night Wash | `#33364D` | Dark public stage, dark card-back field, or high-contrast inverse surface. |

### Palette Rules

- Use Paper Warm as the dominant UI background. Avoid pure white except where a technical QR code requires it.
- Use Ink Primary for all primary text and essential controls; do not replace it with decorative watercolor hues.
- Reserve Moon Gold, Coral Spark, Sky Signal, and Moss Signal for meaning. Limit a single screen to one dominant accent plus state colors where needed.
- Use color to reinforce state, not to encode a rule by itself. Pair it with text, an icon, border treatment, or position.
- Keep interface surfaces less saturated than cards and board artwork so game art remains dominant.
- Verify every text/background pairing against its intended size and weight before adoption.

## Thematic Card Palettes

Each card should select one thematic palette, use three to five colors in the illustration, and reserve the accent colors for focal points. Exact pigment variation is welcome; color hierarchy is not optional.

| Theme | Core colors | Accent colors | Narrative use |
| --- | --- | --- | --- |
| Light | Butter `#F4D77B`, Peach `#F3B38E`, Cloud `#F7F0DE`, Soft Lilac `#C9B9D8` | Sun `#E99828`, Rose `#D95E72` | Discovery, hope, warmth, revelation, beginnings. |
| Water | Mist Blue `#B9D5DF`, Lagoon `#72AFC0`, Sea Glass `#A8C9B4`, Foam `#F3F2E8` | Deep Teal `#277C88`, Coral `#D96E61` | Drift, memory, emotion, reflection, transformation. |
| Nature | Sage `#B7C59B`, Moss `#789267`, Petal `#D7A4A8`, Soil `#B58E69` | Poppy `#D55748`, Leaf Dark `#3E6647` | Growth, cycles, gentleness, wildness, connection. |
| Mystery | Dust Lilac `#AAA0C3`, Fog Blue `#839AB4`, Ash `#B7AFA9`, Moon `#E7D7A4` | Violet Ink `#5D4A85`, Gold `#D99A31` | Riddles, portals, hidden meaning, dream logic, duality. |
| Shadow | Blue Gray `#78849A`, Mauve Smoke `#9A7C91`, Charcoal Wash `#454655`, Pale Smoke `#D7D3CF` | Ember `#C95A43`, Electric Blue `#467EAA` | Tension, contrast, uncertainty, night, change. Keep imagery safe and non-threatening. |

## Typography

**Initial proposal.** Use a two-family system that feels literary but remains highly legible in shared living-room conditions.

| Role | Recommended family | Safe web fallback | Use rule |
| --- | --- | --- | --- |
| Interface and reading | Atkinson Hyperlegible | Arial, sans-serif | Names, scores, instructions, buttons, room codes, card labels, and all critical reading. |
| Display titles and clues | Fraunces | Georgia, serif | Round titles, clue presentation, celebratory moments, and short headings only. |
| Numeric fallback | Atkinson Hyperlegible | Arial, sans-serif | Scores, timers if added later, card numbers, and room codes. Use tabular figures where available. |

- Do not use display type for room codes, votes, scores, action labels, or long instructions.
- Keep decorative text to short phrases. A clue remains content, not decoration; use the display family only when it stays readable.
- Avoid all caps for sentences. All caps is allowed only for short status labels with generous tracking.
- Avoid thin weights, condensed families, script fonts, and handwritten fonts in the UI.

## Type Scale And Legibility

**Initial proposal.** Sizes below are CSS-pixel targets for a 16:9 TV layout and a standard phone layout. Increase rather than decrease if the actual viewing distance demands it.

| Token | TV target | Mobile target | Use |
| --- | --- | --- | --- |
| Display | 72 px / 1.05 | 40 px / 1.1 | Round reveal, major celebratory title. |
| Heading 1 | 52 px / 1.1 | 32 px / 1.15 | Screen title, clue headline. |
| Heading 2 | 36 px / 1.15 | 24 px / 1.2 | Phase heading, major group title. |
| Body Large | 28 px / 1.3 | 20 px / 1.35 | TV instructions, prominent mobile guidance. |
| Body | 22 px / 1.35 | 16 px / 1.45 | Supporting information and normal mobile copy. |
| Label | 18 px / 1.25 | 14 px / 1.3 | Statuses, metadata, compact labels. |
| Card Number | 32 px / 1 | 24 px / 1 | Voting labels. Must be bold and unambiguous. |

### Legibility Rules

- TV: primary instructions, clue text, player names, and score changes must use Body Large or larger. Never rely on Label size for room-wide information.
- TV: use no more than two text hierarchy levels in one visual cluster; a couch viewer must understand it at a glance.
- Mobile: Body is the minimum for ordinary reading. Labels may be smaller only when secondary and not required to complete an action.
- Keep line length between 35 and 65 characters on TV copy blocks and between 25 and 45 characters on phones where possible.
- Use left-aligned prose. Center text only for short, ceremonial content such as a clue or phase announcement.
- Provide large, persistent card numbers during voting; do not require players to identify a card only by its artwork.

## Spatial System

**Initial proposal.** Use a 4 px base unit. Favor open spacing over dense information packing.

| Token | Value | Use |
| --- | --- | --- |
| Space 1 | 4 px | Fine alignment and icon gaps. |
| Space 2 | 8 px | Compact internal spacing. |
| Space 3 | 12 px | Small control and label separation. |
| Space 4 | 16 px | Default mobile padding and related-item gaps. |
| Space 5 | 24 px | Standard section spacing. |
| Space 6 | 32 px | Generous mobile section spacing. |
| Space 7 | 48 px | Standard TV panel and section spacing. |
| Space 8 | 64 px | Major TV separation and stage breathing room. |
| Space 9 | 96 px | Ceremonial TV spacing around key reveals. |

### Borders, Corners, Shadows, And Elevation

- Use `1 px` Mist borders for quiet separation and `2 px` Ink Primary borders for a selected, focused, or rule-critical object.
- Use organic but restrained corner radii: `12 px` for controls and small panels, `20 px` for large surfaces, and `24 px` for cards. Do not use exaggerated pill shapes for most UI.
- Use soft, low-opacity shadows that suggest paper lifted from a table, not floating glass: `0 4px 12px rgba(39, 38, 43, 0.12)` for low elevation and `0 12px 28px rgba(39, 38, 43, 0.16)` for high elevation.
- Elevation communicates temporary priority only: a selected card, modal decision, or current board moment. Most surfaces should remain visually flat.
- Never stack borders, strong shadows, gradients, and texture on the same UI surface. Choose one primary separation cue.

## Game Cards

### Card Geometry

- Use a portrait `2:3` aspect ratio for all playable cards.
- Maintain a consistent printed-art margin inside every card. **Initial proposal:** `6%` of the short edge for the outer frame and `3%` for the inner breathing margin.
- Keep illustration visible at all relevant sizes. Crop only after confirming that the core silhouette, focal accent, and narrative ambiguity survive.
- Preserve a clean outer frame in Paper Warm or Paper Deep with a fine Ink Primary line. The frame supports the art; it must not become ornamental competition.

### Card Hierarchy

- Illustration occupies nearly all of the card face. No title, description, rarity, category, or decorative badge belongs on the playable face.
- A card number is an interaction label, not an art label. During voting, show it in a separate, high-contrast tab or adjacent label that does not cover the artwork.
- Selection state may add a controlled Ink Primary outline, a subtle lift, and a concise text confirmation. Never obscure the illustration with a full-color overlay.
- Disabled cards, including a player's own card during voting, must retain enough artwork to be recognized while clearly indicating unavailability through label, reduced contrast, and blocked interaction.

### Card Backs

- Use an original ImaginArte pattern built from portals, spirals, droplets, flowers, moons, and mirrored transformations.
- Use a centered, symmetrical or near-symmetrical composition that reads correctly when rotated.
- Keep the back quieter than a face: one dark wash field, one warm accent, fine ink marks, and ample paper detail are sufficient.
- Do not use imagery, text, edge wear, or color variations that can reveal card identity.

### Card States

| State | Required treatment |
| --- | --- |
| Default | Paper frame, fine ink contour, no intrusive UI decoration. |
| Focused | High-contrast Ink Primary focus ring outside the card frame; never rely on shadow alone. |
| Selected | Ink Primary outline plus light lift and explicit selected text or check indicator outside the art. |
| Submitted | Keep selected card visible to its owner with a concise confirmation. Do not reveal it publicly before the reveal phase. |
| Disabled | Muted surface veil, unavailable label, and no interactive affordance. Preserve recognizability. |
| Revealed | Highlight storyteller and vote relationships with labels and connectors outside art whenever possible. |

## Shared TV And Mobile Controls

### Shared TV

- Treat the TV as a public stage. It communicates phase, clue, submitted-card set, score outcome, and board progress.
- Use low density: one dominant event, one supporting status area, and one optional progress element per view.
- Prioritize phase, storyteller identity, clue, card labels, submission/voting progress, score changes, and board movement in that order.
- Keep private information absent: hands, selected card identity before reveal, pending votes, and player-only prompts never appear here.
- Use large, stable composition. Avoid moving core labels or rearranging cards while people are reading or deciding.
- The original progress board should be map-like and poetic, but its path, positions, names, and score changes must remain readable from a couch.
- Host-only actions must be visually distinct and require a clear confirmation when they advance the room state.

### Mobile Controllers

- Treat each phone as a private controller. Present the current required action first, then only the information necessary to complete it.
- Use higher density than TV only where it helps a player browse their private hand; do not turn mobile views into dashboards.
- Provide one primary action per step: choose a card, enter a clue, submit, or cast a vote.
- Keep the clue persistent while a guesser chooses a card or votes.
- Confirm completed actions immediately, then show a concise waiting state. Do not expose private choices in that confirmation.
- Use comfortable touch targets. **Initial proposal:** minimum `44 x 44 px`, with larger full-width primary actions where space allows.
- Require explicit submit/confirm actions for card selection and voting. Selection alone must not commit an irreversible action.
- For voting, visibly disable the player's own submitted card and explain why in text or iconography.

## Accessibility

- Meet WCAG AA contrast as a minimum: `4.5:1` for normal text and `3:1` for large text and meaningful UI boundaries. Aim for stronger contrast for TV content.
- Use Ink Primary on Paper Warm or Paper Deep for primary reading. Do not place essential text directly on watercolor art unless a verified opaque backing is present.
- Do not communicate selection, readiness, correctness, vote ownership, or errors through color alone. Pair color with words, icons, outlines, patterns, or placement.
- Keep interactive mobile targets at least `44 x 44 px`. Keep TV controls and card labels visibly larger than this functional minimum.
- Support keyboard focus with a persistent, high-contrast focus indicator that is not hidden by card frames or shadows.
- Avoid time pressure in the first version. If any timed behavior is introduced later, make it optional, clearly visible, and non-punitive.
- Respect reduced-motion preferences. Replace board travel, card dealing, pigment movement, and reveal flourishes with instant state changes or brief fades.
- Avoid flashing, rapid strobing, and high-frequency particle effects. Decorative motion must never block reading, selection, or voting.
- Use clear language and short instructions. Pair abstract art with literal interaction labels such as “Select card 3” and “Vote submitted.”

## Illustration Direction

### Art Rules

- Paint in dreamlike watercolor with transparent washes, organic blooms, paper texture, sparse splatters, and fine expressive dark ink contours.
- Build every card around one striking, interpretable visual idea rather than a scene full of objects.
- Prioritize silhouette, gesture, emotion, and metaphor over realistic anatomy or environmental detail.
- Suggest backgrounds through halos, pigment clouds, atmospheric stains, distant moons, or soft gradients. Do not build detailed landscapes or busy rooms.
- Use recurring motifs naturally: portals, spirals, droplets, flowers, flame, water, skies, moons, transformation, and duality.
- Establish one clear focal point and leave enough quiet paper around it for the idea to breathe.
- Keep characters elegant and expressive. They may be strange, but they must not be grotesque, threatening, or emotionally distressing.
- Use darkness as a safe poetic contrast: night water, a silhouette, an eclipse, or a quiet doorway. Never use horror iconography, violence, injury, or predatory threat.

### Reusable Prompt Template

```text
Dreamlike surreal watercolor illustration for a family storytelling card: [SINGLE VISUAL IDEA].
Create an elegant, expressive [CHARACTER OR OBJECT] with a clear silhouette and metaphorical emotion.
Use [THEMATIC PALETTE] with translucent washes, organic pigment blooms, subtle warm paper texture,
fine slightly irregular dark ink contours, and one or two saturated focal accents: [ACCENTS].
Suggest the background with [HALO / PIGMENT CLOUD / MOON / SKY WASH], leaving generous warm-paper negative space.
Composition: portrait 2:3, one central focal point, readable at small size, open to multiple interpretations.
Mood: poetic, mysterious, gentle, family-safe, visually calm.
```

### Prompt Constraints

```text
Avoid generic fantasy stock imagery, photorealism, hyperrealism, 3D render, CGI, glossy plastic,
video-game concept art, heavy digital airbrushing, dense scenery, clutter, text, logos, frames,
weapons, gore, horror, monsters, threatening faces, body distortion, sexualization, babyish cartoon style,
neon overload, rainbow palettes, harsh black fills, aggressive contrast, and copied visual language from existing games.
```

### Example Prompts

```text
Dreamlike surreal watercolor illustration for a family storytelling card: a small figure carrying a doorway like an umbrella.
Create an elegant, expressive traveler with a clear silhouette and quiet curiosity.
Use the Mystery palette with translucent washes, organic pigment blooms, subtle warm paper texture,
fine slightly irregular dark ink contours, and gold plus violet focal accents.
Suggest the background with a pale moon halo and fog-blue pigment cloud, leaving generous warm-paper negative space.
Composition: portrait 2:3, one central focal point, readable at small size, open to multiple interpretations.
Mood: poetic, mysterious, gentle, family-safe, visually calm.
```

```text
Dreamlike surreal watercolor illustration for a family storytelling card: a sleeping whale releasing glowing flowers into the sky.
Create an elegant, expressive whale with a clear silhouette and a feeling of gentle transformation.
Use the Water palette with translucent washes, organic pigment blooms, subtle warm paper texture,
fine slightly irregular dark ink contours, and coral plus deep teal focal accents.
Suggest the background with a sea-glass halo and faint moon wash, leaving generous warm-paper negative space.
Composition: portrait 2:3, one central focal point, readable at small size, open to multiple interpretations.
Mood: poetic, mysterious, gentle, family-safe, visually calm.
```

## Do / Don't

| Do | Don't |
| --- | --- |
| Let the illustration be the largest and most memorable element. | Cover card art with titles, badges, dense labels, or opaque selection overlays. |
| Use warm paper, ink, and controlled pigment to create a handmade world. | Use glossy gradients, glassmorphism, chrome effects, or generic game-dashboard styling. |
| Create one strong metaphor per card with open interpretation. | Combine many unrelated objects into a literal, noisy scene. |
| Use color contrast to support a story and an interaction state. | Use color as the only indication of state, correctness, or action. |
| Keep TV layouts spacious, public, and readable from a couch. | Place private hands, tiny metadata, or multiple competing actions on TV. |
| Keep phone flows private and action-first. | Make players navigate through menus to submit a card or vote. |
| Use fine, irregular ink lines with calm watercolor washes. | Use hyperrealism, 3D rendering, hard vector geometry, or aggressive black outlines. |
| Make mystery gentle and family-safe. | Use horror, grotesque anatomy, violence, fear cues, or infantilized cartoon clichés. |
| Use an original imaginative progress map. | Copy the board, card layout, symbols, or recognizable visual language of another game. |

## Initial Design Tokens

These tokens are a naming baseline for future CSS variables or another token format.

| Token | Value |
| --- | --- |
| `color.canvas` | `#FBF7EE` |
| `color.surface` | `#F0E7D6` |
| `color.text.primary` | `#27262B` |
| `color.text.secondary` | `#5E5A61` |
| `color.border.subtle` | `#DED9D4` |
| `color.accent.gold` | `#E5A83B` |
| `color.state.info` | `#4F82A5` |
| `color.state.success` | `#5C8065` |
| `color.state.danger` | `#D9685D` |
| `color.surface.night` | `#33364D` |
| `font.ui` | `Atkinson Hyperlegible, Arial, sans-serif` |
| `font.display` | `Fraunces, Georgia, serif` |
| `space.1` | `4px` |
| `space.2` | `8px` |
| `space.4` | `16px` |
| `space.5` | `24px` |
| `space.7` | `48px` |
| `space.8` | `64px` |
| `radius.control` | `12px` |
| `radius.surface` | `20px` |
| `radius.card` | `24px` |
| `border.subtle` | `1px solid #DED9D4` |
| `border.strong` | `2px solid #27262B` |
| `shadow.low` | `0 4px 12px rgba(39, 38, 43, 0.12)` |
| `shadow.high` | `0 12px 28px rgba(39, 38, 43, 0.16)` |
| `card.aspect-ratio` | `2 / 3` |
| `touch.target.min` | `44px` |
