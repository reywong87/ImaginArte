# Progress Board: Between Sky And Earth

## Purpose And Scope

The progress board is ImaginArte's persistent public stage after the TV confirms that the game has started. It represents a dreamlike ascent from earth toward imagination: the route begins among roots and flowers in the lower-left corner and ends before a golden portal in the upper-right corner. Its length matches the room's authoritative target score: 15, 25, or 35 positions.

The route remains visible during turns, voting, revelation, and scoring, but not in the waiting lobby. Cards, clues, votes, and results overlay its central area, which must remain quiet and free of progress elements.

This specification defines the board's visual layers. Game rules, scoring, and state transitions belong to the application layer, not the interface.

## References And Base Assets

- Horizontal `16:9` illustrated background: the “Between Sky and Earth” watercolor world.
- Functional route: a runtime SVG layer composed of a target-score number of circular progress nodes and one fewer straight connectors on top of the background.
- Production guide: `recorrido-numerado-transparente.png`, showing the intended node positions and order for the 25-position medium route.
- Player tokens: seed-comet medallions independent from the background.

Assets must remain separate, with transparency where appropriate. The background does not include nodes, tokens, text, or interface elements. The production guide is reference-only and is not used as a single raster route asset; React composes the runtime route from reusable SVG primitives.

## Progress Route

- The route contains exactly as many positions as the room's `TargetScore`: 15 for Corta, 25 for Media, and 35 for Larga.
- It begins at the lower-left roots and ends before the upper-right golden portal.
- Nodes are visual anchors of approximately 65 px in diameter on a `1920 x 1080` canvas. Keep every pair of node centers at least 100 reference units apart so circles do not overlap and retain a generous paper gap.
- Each connector is a straight line between the centers of two consecutive nodes. A route with `N` nodes has `N - 1` connectors.
- Each route length has an ordered list of node centers. All three arrangements of straight segments retain a continuous, organic, ascending reading and end before the portal.
- Nodes are not token containers and do not imply effects, bonuses, or penalties.
- Node numbers `1` through `N` are visible on the public board. React renders each number as SVG `<text>` centered inside its corresponding `<circle>`; numbers are not baked into the background, guide image, or a node image asset.

### Runtime Route Composition

- Each route layout is an ordered list of node centers in the reference `1920 x 1080` coordinate system. The numbered guide establishes and reviews the 25-position medium layout; the 15- and 35-position layouts preserve its visual language and safe zones.
- React renders one reusable circular node for each position and one reusable straight-line connector for each consecutive pair. Connectors render below nodes so their endpoints remain visually hidden by the node surfaces.
- The route is rendered in a responsive SVG with a matching `viewBox`, so the background, connectors, nodes, and labels scale together without losing alignment.
- The public room snapshot's `TargetScore` selects the matching coordinate list. This presentation mapping does not calculate scores or define game rules.
- Future route lengths may provide a different ordered coordinate list while reusing the same node, connector, and text-rendering components.

## Overlay Safe Zones

The board's central area is reserved for cards, clues, voting, results, and scoring. Tokens, threads, labels, and state effects must not enter it. After the game starts, the upper band is available to the static route; phase and storyteller information use the central floating window instead. The live connection status occupies the lower-right corner, which route nodes must leave clear.

- Primary central stage: `x 28%`, `y 30%`, width `44%`, height `40%`.
- Wide card or voting area: `x 24%`, `y 25%`, width `52%`, height `50%`.
- Lower status or scoring band: `x 30%`, `y 76%`, width `40%`, height `15%`.
- Lower-right connection status: reserve the corner for a readable text status; no route node may overlap it.

## Token System: Anchored Constellation

### Concept And Rules

Each board position is a 65 px visual anchor, never a container. The token or constellation sits outside the node and connects to it with a short, irregular, semi-transparent ink or pigment thread.

- The node remains 100% visible.
- Tokens open outward from the route, never toward the reserved central area.
- Distance from node center is `42-62 px`.
- Thread length is `18-34 px`, with `1-1.5 px` thickness.
- A constellation does not cross another position or the portal.
- Color is always complemented by icon and pattern.
- Names are not persistently visible except on focus, active turn, recent movement, or leadership.

### Individual Token

The token is a 28 px base seed-comet medallion. A `26-32 px` range is allowed.

- Silhouette: a slightly oval circle with a minimal `5-7 px` watercolor tail.
- Border: 2 px indigo-gray ink.
- Separation: 3 px warm-paper outer halo.
- Interior: soft watercolor wash, without plastic shine.
- Center: a `12-14 px` icon recognizable at 20 px total size.
- Pattern: visible in a secondary area with restrained opacity.
- Microtokens for groups of 5 to 8: `20-22 px`, preserving border, icon, and pattern.

### Accessible Player Identities

| Player | Color | Icon | Pattern |
| --- | --- | --- | --- |
| 1 | Clay coral `#C97968` | Sun | Short rays |
| 2 | Lagoon `#4F98A0` | Droplet | Waves |
| 3 | Sage `#79936B` | Leaf | Small leaves |
| 4 | Smoky lavender `#9284B5` | Moon | Rings |
| 5 | Honey ochre `#C89A45` | Spiral | Dots |
| 6 | Ink blue `#526F9A` | Star | Diagonal stripes |
| 7 | Petal pink `#B67D91` | Flower | Petals |
| 8 | Soft plum `#785F7D` | Door | Soft grid |

Do not use color as the only identifier. Maintain sufficient contrast between icon, pattern, and token interior.

### Shared Layouts

`o` represents a board node, `*` a token, and `~` a short thread. Opening direction adapts to the available outer side.

#### One Token

```text
o ~ *
```

The token sits beside the node, never on top of it.

#### Two Tokens

```text
  *
  ~
o
  ~
  *
```

Use a diagonal pair or short arc with balanced visual weight.

#### Four Tokens

```text
    *   *
     \ /
      o
     / \
    *   *
```

Use a compact outward fan. The node center remains fully clear.

#### Eight Tokens

```text
  *  *  *  *
    \ | | /
       o
    / | | \
  *  *  *  *
```

Use two curved microtoken arcs. If needed, place a 14 px auxiliary `8` badge beside the thread, never on the node or in place of tokens.

### Orientation By Board Area

| Board area | Opening direction |
| --- | --- |
| Roots and lower-left start | Up and right |
| Water and mist | Leftward or upward exterior, depending on route segment |
| Upper arc | Upward, while preserving distance from islands |
| Approach to the portal | Down and right, without entering the empty centre |
| Portal | Down and left of the portal |

General rule: calculate an opening vector from the node toward the useful canvas perimeter; block vectors that enter the central overlay area, leave the canvas, cross the route, or invade the portal. If the first vector is blocked, rotate in 30-degree increments.

### Contextual Name Label

Show a label only for the active token, focused token, recently moved token, or leader.

- Translucent warm-paper capsule, not glossy UI.
- Name in `15-17 px` indigo-gray ink.
- Optional score as a smaller second line.
- Fine connector from the label to the token, never to the node.
- Position behind the movement direction and outside the central area.
- Automatically hide it if it conflicts with another label; prioritize the active and focused token.

## Round State And Progress Layer

### General Principle

The state layer is temporary, localized, and quiet. It never redraws the board, route, or tokens: it only adds brief signals beside a token, constellation, or portal.

- The clean centre remains free for cards, clues, votes, and results.
- Each effect starts from the relevant token or node and opens toward the useful outer edge.
- At most one strong focus exists at a time: one primary label, trail, or portal response.
- Use restrained gold, irregular ink, and faint watercolor; no aggressive pulses or persistent UI.

### Priority And Coexistence

| Priority | State | Treatment |
| ---: | --- | --- |
| 1 | End of game | Portal, ceremonial label, and finalist tokens |
| 2 | Recent movement | Route trail, arrival halo, and `+N` |
| 3 | Active player | Moon Gold halo and role label |
| 4 | Leader | Restrained secondary sparkle |
| 5 | Normal | Token and constellation without label |

A higher-priority state softens, but does not hide, lower-priority states. For example, during movement the active halo is reduced; during the end game, `+N` markers temporarily disappear.

### Visual States

#### Active Player

- Moon Gold `#E5A83B` watercolor halo, `8-12 px` around the token.
- A second thin, irregular dark ink contour.
- Opaque warm-paper contextual label, for example: `Lucía · Narrator`.
- `1.5 px` connection line from label to token.
- In a shared constellation, only the active token receives the halo; the others retain their identity without excessive dimming.

#### Recent Movement And Points

- The trail follows the actual segments between nodes; it never cuts through the centre.
- A faint gold wash travels over the route thread for `700-1000 ms`; it dissolves from back to front.
- At the destination: a short arrival halo and a `+1`, `+2`, or `+3` marker.
- The marker uses Paper Warm, dark ink, and gold as an accent; place it outside the node, beside the destination token.
- After `1.2-1.8 s`, the trail and marker disappear; normal or active state remains as appropriate.

#### Current Leader

- A three-stroke sparkle, ink star, or small paper light of `10-12 px`.
- Position it opposite the active halo so it does not compete.
- In a tie, each leader receives their own sparkle; do not group them into a shared badge.
- Do not show it during recent movement or the end game.

#### Shared Constellation

- Two tokens: diagonal pair or short arc.
- Three or four tokens: outward fan by default.
- Five or six tokens: lateral half-moon where longitudinal space exists beside the route.
- Seven or eight tokens: compact orbit in two arcs; node remains visible in the centre.
- Show only the name of the focused or active token.

#### Goal And End Game

- The portal very gently increases its inner golden light by `10-15%` perceived brightness, without a flash.
- The arriving token occupies its assigned external position, down and left of the portal.
- Brief ceremonial label: `Has reached the threshold`.
- If multiple players arrive in the same round, open a finalist constellation before the portal; use the neutral plural label: `Have reached the threshold`.
- Do not visually declare a winner until game rules resolve the tie.

### Render Layers

| Order | Layer |
| ---: | --- |
| 1 | Illustrated background |
| 2 | Progress route and nodes |
| 3 | Constellation threads |
| 4 | Base tokens: color, icon, and pattern |
| 5 | Leader indicator |
| 6 | Active-player halo |
| 7 | Movement trail and arrival halo |
| 8 | Portal light response |
| 9 | Contextual label, `+N`, or ceremonial text |

### Reduced Motion

- The token changes directly to the destination anchor.
- A static arrival halo appears for `600 ms`.
- The `+N` marker remains for `1.5 s` and then disappears.
- There is no interpolation, particle, flashing, or travel animation.
- The portal uses one static, faint brightness increase without a pulse.

## Assets And Deliverables

Maintain the following layers with transparency and without including the board background:

1. `token-base`: silhouette, halo, and contour.
2. `token-identity`: color, icon, and pattern per player.
3. `token-tail`: minimal directional tail.
4. `anchor-thread`: thread between anchor and token.
5. `constellation-layout`: relative positions for 1, 2, 4, and 8 tokens.
6. `token-state`: active halo, trail, and leader marker.
7. `context-label`: on-demand name and score.

## Accessibility And TV Legibility

- At a distance of `2-3 m`, viewers must recognize which positions contain players and which token is active.
- The progress node must remain visible even with eight players.
- Two neighboring constellations need at least 12 px between their outer halos when they occupy nearby segments.
- Recognize the active player in less than one second through halo, contour, and label; do not depend only on text or color.
- The `+N` marker must be readable without hiding the node or token.
- The final position for the selected route must remain readable as arrival at the portal, not as a token embedded in it.
- No tokens, threads, labels, or state effects may appear in the central overlay area.
- With reduced motion enabled, every score change remains unambiguous.

For constellations of five to eight microtokens, the priority from a couch is to identify that a group exists at the position and distinguish the active token. Detailed individual identity must be available through focus or a contextual label.

## Acceptance Criteria

- The route contains exactly `TargetScore` nodes (15, 25, or 35) and visually ends unambiguously before the portal.
- Node labels `1` through `TargetScore` are visible, centered, and legible at the intended TV distance.
- Tokens do not cover the progress node or enter the reserved central area.
- Constellations of 1, 2, 4, and 8 tokens retain a clear association with their anchor node.
- Constellations and labels reorient near edges, the portal, route, or other nodes.
- Only one primary label or dominant movement effect exists at a time.
- During movement, the trail communicates a route traversal rather than a direct jump.
- During the end game, tokens and labels do not cover the portal silhouette or declare a winner before a possible tie is resolved.
- The design retains the warm-paper watercolor, irregular ink, restrained gold, and gentle mystery language defined in the design system.

## Appendix: Token Reference Prompt

```text
Create a clean reference sheet for a family storytelling game token system named "anchored constellation." On warm textured watercolor paper, show eight small traveler seed-comet tokens, each 28 px-scale in appearance, with fine irregular dark ink outlines and a warm paper halo. Use these accessible identity pairs: clay coral sun with short rays, lagoon droplet with waves, sage leaf with leaf pattern, smoky lavender moon with rings, honey ochre spiral with dots, ink blue star with diagonal stripes, petal pink flower with petals, soft plum door with subtle grid. Include compact visual demonstrations of one, two, four, and eight tokens arranged outside a visible 65 px watercolor anchor node, linked with delicate short pigment threads. Show outward-opening formations only; preserve the anchor center. Include subtle states for active token with moon-gold halo, recent movement watercolor trail, leader with three tiny orbiting dots, and a focused token with a small warm-paper name label. Poetic handcrafted watercolor, no UI panels, no plastic, no 3D, no game-board squares, no neon, no text except tiny sample label, spacious reference layout.
```
