# ImaginArte Design System

## Purpose

This guide defines the visual language for ImaginArte: a family storytelling game played together on a TV and controlled privately from phones. It guides future design and implementation work; it does not prescribe application components or game behavior.

All choices marked **Initial proposal** are pragmatic defaults that can be validated in playtesting.

## Reference Reading

This version is grounded in the available card reference set at `docs/assets/cartas/` (98 portrait illustrations). The references establish the visual baseline rather than a requirement to reproduce any individual image:

- Storybook surrealism: ordinary objects become vessels, observatories, gardens, libraries, clocks, doors, and landscapes.
- A central impossible transformation is supported by smaller narrative details; the images are not sterile single-object icons.
- Warm, fibrous paper remains visible at the edges and between pigment blooms. Washes may become dense in the focal area and dissolve toward the margin.
- Fine blue-black or brown ink contours define silhouettes and objects. Avoid flat vector outlines and pure black fills.
- The recurring chromatic language is deep blue/teal, dusty violet, moss and olive, warm ochre, copper/terracotta, and paper cream, with small luminous gold or coral notes.
- Characters and creatures are elegant, curious, and expressive. Even shadowed subjects remain inviting and family-safe.

When a future reference folder is supplied, compare it against these observations before changing the tokens below. The cards remain the source of truth for illustration direction; interface surfaces must stay quieter.

## Visual And Emotional Principles

- Make each card an invitation to tell a different story. Illustration is always the emotional center of the experience.
- Balance poetry with clarity. The interface must be immediately understandable for children and adults sharing a room.
- Use a warm, quiet paper world around vivid imaginative moments. Negative space is an intentional part of the composition.
- Build mystery through metaphor, contrast, and suggestion, never through fear, threat, gore, or visual aggression.
- Favor handcrafted imperfection: translucent watercolor, visible paper grain, granulating pigment, organic blooms, dry-brush edges, and fine irregular ink contours.
- Give each illustration a controlled palette with a dominant temperature and one or two saturated narrative accents. Rich scenes are allowed, but color must have hierarchy rather than becoming rainbow noise.
- Use light versus dark, warm versus cool, and ordinary versus impossible as visual story devices.
- Keep public TV information calm and theatrical; keep private mobile controls direct, compact, and reassuring.
- Preserve privacy by never exposing a player's hand or pending choice on the shared screen.

## Base Palette

**Initial proposal.** The base palette is a warm paper-and-ink system sampled from the reference cards. Thematic illustration palettes supply most expressive color; UI surfaces deliberately remain desaturated.

| Semantic name | Hex | Use rule |
| --- | --- | --- |
| Paper Warm | `#FBF4E2` | Card faces, input fields, clue panels, and light text surfaces. |
| Paper Deep | `#E9D9B9` | Secondary paper panels, inactive surfaces, and quiet separators. |
| Ink Primary | `#10243A` | Primary text on paper, essential icons, and cool contour reference. |
| Ink Soft | `#D8D8D0` | Secondary text on Night Wash; use a dark equivalent on paper. |
| Mist | `#B7B9B3` | Disabled outlines and restrained separation. |
| Ochre Gold | `#F2B632` | Primary action, focus, progress, frames, and selected emphasis. |
| Copper Spark | `#B95F4B` | Errors, destructive choices, or a warm narrative accent. Never use alone. |
| Teal Signal | `#397C82` | Information, links, and cool interactive emphasis. |
| Moss Signal | `#607653` | Confirmed, ready, and completed states. Never use alone. |
| Violet Wash | `#756A91` | Optional poetic emphasis for stage or illustration-adjacent moments. |
| Night Wash | `#08263E` | Dominant TV/mobile stage, card-back field, and inverse surface. |

### Palette Rules

- Use Night Wash as the dominant application surface in the reference direction. Use Paper Warm for content panels, fields, clues, and cards.
- Use Ink Primary for reading on paper and Paper Warm for reading on Night Wash; do not replace either with decorative watercolor hues.
- Reserve Ochre Gold, Copper Spark, Teal Signal, and Moss Signal for meaning. Limit a single screen to one dominant accent plus state colors where needed.
- Use color to reinforce state, not to encode a rule by itself. Pair it with text, an icon, border treatment, or position.
- Keep interface surfaces less saturated than cards and board artwork so game art remains dominant.
- Verify every text/background pairing against its intended size and weight before adoption.

## Interface Visual Language

The mockups in `docs/assets/visual-references/mockups/` define a more ceremonial interface than the card-only palette suggests:

- **Night stage:** TV and mobile screens use a deep blue, subtly patterned field as the main surface. It creates a quiet stage for the warm art and gold controls.
- **Ornamental frame:** Screens and major panels use a double gold border with rounded, softly stepped corners and small botanical or star ornaments. The frame is a signature motif, not decoration to repeat around every minor element.
- **Paper windows:** Inputs, clues, room codes, and supporting messages sit in warm paper panels with a thin gold outline. They should feel printed, not like white software cards.
- **Gold action:** The primary action is a large warm-gold button with a lightly textured paper/pigment finish, dark text, and generous serif lettering. It must remain readable without relying on glow.
- **Luminous selection:** Selected cards, focused choices, and the current board moment may use a restrained gold halo plus an explicit label or check. Do not use glow for ordinary decoration.
- **Scenic TV backdrop:** Public TV screens may use a full-bleed illustrated landscape—waterfall, floating islands, moon, doorway, clouds—with a dark central panel to protect text contrast. The backdrop must not compete with the current event.
- **Private mobile stage:** Mobile screens keep the same Night Wash, border, ornaments, and paper/gold vocabulary, but use a single vertical flow and one primary action at the bottom.

The interface should feel like a storybook portal: dark blue for depth, cream for readable paper, and gold for invitation and progress. Avoid generic dashboards, glassmorphism, flat white pages, or untextured neon controls.

### Reusable Ceremonial Window

`ventana-flotante-azul-dorado-vacia.png` is a visual reference for a floating blue-and-gold window, not a stretchable production background. Implement this window as separate layers so it can adapt to TV and mobile proportions without deforming its ornaments:

- **Surface:** use the Night Wash color `#08263E` as a CSS background. CSS may add restrained gradients, vignette, or shadow for depth.
- **Texture:** use `docs/assets/ui/ventana-textura-botanica.png`, a separate transparent PNG containing the authored botanical/pigment texture, positioned or repeated inside the window at restrained opacity. CSS gradients can supply broad color variation, but should not replace this fine illustrated texture.
- **Corners:** use one independent reusable corner ornament, preferably an SVG for clean scaling, and rotate or mirror it for the four corners. If the painted gold grain cannot be reproduced as vector art, a transparent PNG fallback is acceptable.
- **Frame rules:** draw the straight gold edges and simple separators with CSS borders or pseudo-elements. Do not stretch the complete mockup PNG around the window.
- **Content:** render text, cards, and controls in a layer above the texture. Essential content must have verified contrast and must not depend on the texture for separation.

The corner ornament, texture, and CSS surface are decorative assets. They must not contain text, room codes, scores, controls, or other stateful content. Every floating/modal window and major lobby panel uses this same layered treatment: a textured Night Wash surface, a double frame, and independent botanical corner ornaments. Reuse the decoration layer from `TvHomeScreen` rather than creating unadorned software-card variants; keep ordinary controls visually simpler.

### Progress Board Reference Pattern

The board mockups establish a specific visual grammar for progress and scoring:

- Use an illustrated world as the full background, with a readable route layered above it rather than a separate flat board panel.
- Draw the route as a warm-gold chain between circular medallions. Use `medallon-vacio.png` and `conector-dorado.png` as separate transparent background assets; render each number as live, centered text over its medallion, never baked into an image. The active node may get a restrained gold emphasis.
- Treat each node and connector as a reusable HTML `<span>` with a background image. Keep the route as an ordered set of coordinates on a shared responsive 16:9 plane; orient each connector span between consecutive node centers and layer nodes above connectors. A different route can reuse the same spans with a different coordinate list and number of positions.
- Represent players with circular illustrated avatar medallions attached to dark nameplates. Keep names and positions readable before decorative detail.
- Place the score panel in a dark Night Wash window with a gold frame, cream player names/numbers, and gold round-point changes. Highlight the active storyteller with a clear row treatment and icon, not color alone.
- Keep the board scenic but subordinate to the current round result. On smaller TV layouts, reduce background detail before reducing route, player, or score legibility.

## Thematic Card Palettes

Each card should select one thematic palette, use three to five colors in the illustration, and reserve the accent colors for focal points. Exact pigment variation is welcome; color hierarchy is not optional.

| Theme | Core colors | Accent colors | Narrative use |
| --- | --- | --- | --- |
| Light | Butter `#E9C96B`, Peach `#DFA080`, Cloud `#F4EBDD`, Soft Lilac `#BBAAC5` | Ochre `#C88A32`, Rose `#BE5960` | Discovery, hope, warmth, revelation, beginnings. |
| Water | Mist Blue `#AFCBD0`, Lagoon `#5F9DA7`, Sea Glass `#9DBDAA`, Foam `#EDE9DA` | Deep Teal `#286B76`, Copper `#B95F4B` | Drift, memory, emotion, reflection, transformation. |
| Nature | Sage `#AEBB91`, Moss `#70835B`, Petal `#C99398`, Soil `#A77E5F` | Poppy `#B95143`, Leaf Dark `#3F5E45` | Growth, cycles, gentleness, wildness, connection. |
| Mystery | Dust Lilac `#A49AB7`, Fog Blue `#7D91A9`, Ash `#AAA39A`, Moon `#D9C789` | Violet Ink `#62557D`, Gold `#C88A32` | Riddles, portals, hidden meaning, dream logic, duality. |
| Shadow | Blue Gray `#68778D`, Mauve Smoke `#90778A`, Charcoal Wash `#40495A`, Pale Smoke `#D1CBC1` | Ember `#B95F4B`, Teal Light `#4B8796` | Tension, contrast, uncertainty, night, change. Keep imagery safe and non-threatening. |

## Typography

**Initial proposal.** Use a literary serif-led system that matches the mockups' storybook atmosphere, with a highly legible sans-serif reserved for dense operational data. Typography should feel ceremonial in headings and immediately clear in actions.

| Role | Recommended family | Safe web fallback | Use rule |
| --- | --- | --- | --- |
| Interface and reading | Fraunces | Georgia, serif | Headings, clues, buttons, room instructions, and short narrative copy. Use generous line spacing and avoid overly thin weights. |
| Display titles and clues | Fraunces | Georgia, serif | Game title, phase announcements, clues, celebratory moments, and major headings. The reference mood is engraved storybook, not playful cartoon. |
| Operational and numeric | Atkinson Hyperlegible | Arial, sans-serif | Room codes, scores, progress counts, player labels, compact metadata, and any dense or time-sensitive reading. Use tabular figures where available. |

- Do not use display type for room codes, dense score tables, or compact metadata. It may be used for a short action label when contrast and legibility remain strong.
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

**Initial proposal.** Use a 4 px base unit. Favor open spacing over dense information packing in the UI, while allowing card artwork to use layered detail and overlapping motifs.

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

- Use `1 px` Mist borders for quiet separation, `2 px` Ochre Gold borders for major paper panels, and `2–3 px` Ink Primary borders for selected, focused, or rule-critical objects.
- Use organic but restrained corner radii: `10 px` for controls, `16 px` for small panels, `20 px` for large surfaces, and `18–24 px` for cards. Major TV/mobile frames may use `28 px` outer corners with a stepped ornamental profile.
- Use soft, low-opacity shadows that suggest paper lifted from a table, not floating glass: `0 4px 12px rgba(40, 49, 58, 0.12)` for low elevation and `0 12px 28px rgba(40, 49, 58, 0.16)` for high elevation.
- Gold texture may be used on a primary button or ceremonial frame as a subtle pigment grain. Do not turn it into a glossy metallic gradient.
- Elevation communicates temporary priority only: a selected card, modal decision, or current board moment. Most surfaces should remain visually flat.
- A ceremonial frame is an intentional exception: combine its double border with restrained texture and a soft shadow, but keep ordinary controls simpler. Never stack all effects on every surface.

## Game Cards

### Card Geometry

- Use a portrait `2:3` aspect ratio for all playable cards.
- Maintain a consistent printed-art margin inside every card. **Initial proposal:** `5%` of the short edge for the outer frame and `3%` for the inner breathing margin.
- Keep the central transformation, the main silhouette, and at least one secondary narrative clue visible at all relevant sizes. Crop only after checking the card at hand size and TV size.
- Preserve a warm paper frame or softly dissolved paper edge with a fine Ink Primary line. The frame supports the art; it must not become ornamental competition.
- In shared-screen layouts, cards use a warm paper face, a fine gold outer keyline, and a dark navy number tab with a gold outline. Keep the tab outside the illustration's focal area.

### Card Hierarchy

- Illustration occupies nearly all of the card face. The preferred composition is one dominant impossible idea plus two or three supporting motifs—not a literal icon and not an overcrowded scene. No title, description, rarity, category, or decorative badge belongs on the playable face.
- A card number is an interaction label, not an art label. During voting, show it in a separate, high-contrast tab or adjacent label that does not cover the artwork.
- Selection state may add a controlled Ink Primary outline, a subtle lift, and a concise text confirmation. Never obscure the illustration with a full-color overlay.
- Disabled cards, including a player's own card during voting, must retain enough artwork to be recognized while clearly indicating unavailability through label, reduced contrast, and blocked interaction.

### Card Backs

- Use an original ImaginArte pattern built from portals, spirals, droplets, flowers, moons, keys, windows, and mirrored transformations. Draw it in fine ink over a quiet Night Wash or muted paper field.
- Use a centered, symmetrical or near-symmetrical composition that reads correctly when rotated.
- Keep the back quieter than a face: one dark blue-violet wash field, one ochre or copper accent, fine ink marks, and ample paper detail are sufficient.
- Do not use imagery, text, edge wear, or color variations that can reveal card identity.

### Card States

| State | Required treatment |
| --- | --- |
| Default | Warm paper frame, fine Ink Primary contour, thin gold keyline when shown on Night Wash, and no intrusive decoration. |
| Focused | High-contrast Ink Primary or Ochre Gold focus ring outside the card frame; never rely on glow alone. |
| Selected | Gold outline or restrained gold halo plus an explicit “Seleccionada” label and check indicator outside the art. |
| Submitted | Keep selected card visible to its owner with a concise confirmation. Do not reveal it publicly before the reveal phase. |
| Disabled | Muted gray veil, unavailable label such as “Tu carta,” and a prohibition icon where useful. Preserve recognizability. |
| Revealed | Highlight storyteller and vote relationships with labels and connectors outside art whenever possible. |

## Shared TV And Mobile Controls

### Shared TV

- Treat the TV as a public stage. It communicates phase, clue, submitted-card set, score outcome, and board progress.
- Use a scenic full-bleed background with a clear dark-blue content window. The window may be centered or offset, but it must preserve the landscape and remain stable while people read.
- Use gold ornaments, botanical corners, stars, and path markers as a coherent visual vocabulary. Reserve the strongest frame treatment for the active event, not every informational label.
- Use low density: one dominant event, one supporting status area, and one optional progress element per view.
- Prioritize phase, storyteller identity, clue, card labels, submission/voting progress, score changes, and board movement in that order.
- Keep private information absent: hands, selected card identity before reveal, pending votes, and player-only prompts never appear here.
- Use large, stable composition. Avoid moving core labels or rearranging cards while people are reading or deciding.
- The original progress board should be map-like and poetic, but its straight route segments, positions, node numbers, names, and score changes must remain readable from a couch.
- Host-only actions must be visually distinct and require a clear confirmation when they advance the room state.

### Mobile Controllers

- Treat each phone as a private controller. Present the current required action first, then only the information necessary to complete it.
- Use a dark blue full-screen stage with a consistent inner gold frame. Place the title and instruction in the upper third, the task in the middle, and one full-width gold primary action near the bottom.
- Use warm paper for clues, room-code fields, and other content that users must scan or edit. Use gold borders and dark text rather than white software panels.
- Use higher density than TV only where it helps a player browse their private hand; do not turn mobile views into dashboards.
- Provide one primary action per step: choose a card, enter a clue, submit, or cast a vote.
- Keep the clue persistent while a guesser chooses a card or votes.
- Confirm completed actions immediately, then show a concise waiting state. Do not expose private choices in that confirmation.
- Use comfortable touch targets. **Initial proposal:** minimum `44 x 44 px`, with larger full-width primary actions where space allows.
- Require explicit submit/confirm actions for card selection and voting. Selection alone must not commit an irreversible action.
- For voting, visibly disable the player's own submitted card and explain why in text or iconography.

## Accessibility

- Meet WCAG AA contrast as a minimum: `4.5:1` for normal text and `3:1` for large text and meaningful UI boundaries. Aim for stronger contrast for TV content.
- Use Ink Primary on Paper Warm or Paper Deep, and Paper Warm on Night Wash, for primary reading. Use dark Ink Primary text on Ochre Gold buttons. Do not place essential text directly on watercolor art unless a verified opaque backing is present.
- Gold glow is a reinforcement, not a focus indicator by itself. Pair selected/focused states with a solid outline, label, check, or icon.
- Do not communicate selection, readiness, correctness, vote ownership, or errors through color alone. Pair color with words, icons, outlines, patterns, or placement.
- Keep interactive mobile targets at least `44 x 44 px`. Keep TV controls and card labels visibly larger than this functional minimum.
- Support keyboard focus with a persistent, high-contrast focus indicator that is not hidden by card frames or shadows.
- Avoid time pressure in the first version. If any timed behavior is introduced later, make it optional, clearly visible, and non-punitive.
- Respect reduced-motion preferences. Replace board travel, card dealing, pigment movement, and reveal flourishes with instant state changes or brief fades.
- Avoid flashing, rapid strobing, and high-frequency particle effects. Decorative motion must never block reading, selection, or voting.
- Use clear language and short instructions. Pair abstract art with literal interaction labels such as “Select card 3” and “Vote submitted.”

## Illustration Direction

### Art Rules

- Paint in dreamlike watercolor with transparent washes, granulating pigment, visible paper texture, controlled splatters, dry-brush edges, and fine expressive blue-black or brown ink contours.
- Build every card around one striking, interpretable visual idea, then support it with a small cast of secondary motifs that reward looking twice. Detail is welcome when it reinforces the metaphor.
- Prioritize silhouette, gesture, emotion, and metaphor over realistic anatomy or environmental detail.
- Suggest backgrounds through halos, pigment clouds, atmospheric stains, distant moons, windows, shelves, gardens, skies, or soft gradients. Detailed environments are allowed when they remain subordinate to the focal transformation.
- Use recurring motifs naturally: portals, spirals, droplets, flowers, flame, water, skies, moons, transformation, and duality.
- Establish one clear focal point, use a readable silhouette, and let pigment dissolve into warm paper at the edges. Quiet paper should frame the idea, not force every card to be mostly empty.
- Keep characters elegant and expressive. They may be strange, but they must not be grotesque, threatening, or emotionally distressing.
- Use darkness as a safe poetic contrast: night water, a silhouette, an eclipse, or a quiet doorway. Never use horror iconography, violence, injury, or predatory threat.

### Reusable Prompt Template

```text
Dreamlike surreal watercolor illustration for a family storytelling card: [SINGLE VISUAL IDEA].
Create an elegant, expressive [CHARACTER OR OBJECT] with a clear silhouette and metaphorical emotion.
Use [THEMATIC PALETTE] with translucent washes, granulating pigment, visible warm paper texture,
fine slightly irregular blue-black or brown ink contours, and one or two saturated focal accents: [ACCENTS].
Create one dominant focal transformation with two or three supporting motifs, allowing the washes to dissolve into warm-paper breathing room near the edges.
Composition: portrait 2:3, readable silhouette, clear value hierarchy, open to multiple interpretations.
Mood: poetic, mysterious, gentle, family-safe, visually calm.
```

### Prompt Constraints

```text
Avoid generic fantasy stock imagery, photorealism, hyperrealism, 3D render, CGI, glossy plastic,
video-game concept art, heavy digital airbrushing, uncontrolled clutter, text, logos, frames,
weapons, gore, horror, monsters, threatening faces, body distortion, sexualization, babyish cartoon style,
neon overload, rainbow palettes, harsh black fills, aggressive contrast, and copied visual language from existing games.
```

### Example Prompts

```text
Dreamlike surreal watercolor illustration for a family storytelling card: a small figure carrying a doorway like an umbrella.
Create an elegant, expressive traveler with a clear silhouette and quiet curiosity.
Use the Mystery palette with translucent washes, granulating pigment, visible warm paper texture,
fine slightly irregular blue-black ink contours, and ochre gold plus violet focal accents.
Suggest the background with a pale moon halo and fog-blue pigment cloud, leaving warm-paper breathing room around the outer silhouette.
Composition: portrait 2:3, one central focal point, readable at small size, open to multiple interpretations.
Mood: poetic, mysterious, gentle, family-safe, visually calm.
```

```text
Dreamlike surreal watercolor illustration for a family storytelling card: a sleeping whale releasing glowing flowers into the sky.
Create an elegant, expressive whale with a clear silhouette and a feeling of gentle transformation.
Use the Water palette with translucent washes, granulating pigment, visible warm paper texture,
fine slightly irregular blue-black ink contours, and copper plus deep teal focal accents.
Suggest the background with a sea-glass halo and faint moon wash, leaving warm-paper breathing room around the outer silhouette.
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
| Combine a dominant impossible transformation with a few discoverable details. | Reduce every card to a lone icon or fill every corner with unrelated fantasy objects. |
| Let paper show through and let washes dissolve at the edges. | Use hard rectangular crops, glossy digital gradients, or uniform full-bleed saturation. |
| Keep TV layouts spacious, public, and readable from a couch. | Place private hands, tiny metadata, or multiple competing actions on TV. |
| Keep phone flows private and action-first. | Make players navigate through menus to submit a card or vote. |
| Use fine, irregular ink lines with calm watercolor washes. | Use hyperrealism, 3D rendering, hard vector geometry, or aggressive black outlines. |
| Make mystery gentle and family-safe. | Use horror, grotesque anatomy, violence, fear cues, or infantilized cartoon clichés. |
| Use an original imaginative progress map. | Copy the board, card layout, symbols, or recognizable visual language of another game. |

## Initial Design Tokens

These tokens are a naming baseline for future CSS variables or another token format.

| Token | Value |
| --- | --- |
| `color.canvas` | `#08263E` |
| `color.surface` | `#FBF4E2` |
| `color.surface.deep` | `#E9D9B9` |
| `color.text.primary` | `#10243A` |
| `color.text.inverse` | `#FBF4E2` |
| `color.text.secondary` | `#D8D8D0` |
| `color.border.subtle` | `#B7B9B3` |
| `color.accent.gold` | `#F2B632` |
| `color.state.info` | `#397C82` |
| `color.state.success` | `#607653` |
| `color.state.danger` | `#B95F4B` |
| `color.accent.violet` | `#756A91` |
| `color.surface.night` | `#303A50` |
| `font.ui` | `Fraunces, Georgia, serif` |
| `font.display` | `Fraunces, Georgia, serif` |
| `font.operational` | `Atkinson Hyperlegible, Arial, sans-serif` |
| `space.1` | `4px` |
| `space.2` | `8px` |
| `space.4` | `16px` |
| `space.5` | `24px` |
| `space.7` | `48px` |
| `space.8` | `64px` |
| `radius.control` | `10px` |
| `radius.surface` | `20px` |
| `radius.card` | `20px` |
| `radius.frame` | `28px` |
| `border.subtle` | `1px solid #B7B9B3` |
| `border.strong` | `2px solid #10243A` |
| `border.gold` | `2px solid #F2B632` |
| `shadow.low` | `0 4px 12px rgba(40, 49, 58, 0.12)` |
| `shadow.high` | `0 12px 28px rgba(40, 49, 58, 0.16)` |
| `card.aspect-ratio` | `2 / 3` |
| `touch.target.min` | `44px` |
