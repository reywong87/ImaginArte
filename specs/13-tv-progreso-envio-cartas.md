# SPEC 13 — Ventana TV de progreso de envío de cartas

> **Estado:** Implemented  
> **Depende de:** SPEC 03 — Lobby TV con presencia en vivo; SPEC 07 — Recorrido dinámico del tablero en TV; SPEC 08 — Ventana de espera del narrador en TV; SPEC 12 — Integración del turno del jugador en móvil  
> **Fecha:** 2026-11-07  
> **Objetivo:** Mostrar en la ventana central de la TV la pista publicada y el progreso colectivo real de las cartas recibidas durante `GuessersSubmitting`.

## Alcance

**Incluye:**

- Sustituir el contenido de lobby por una ventana informativa central exclusivamente durante `GuessersSubmitting`.
- Usar la pista y los conteos públicos del snapshot autoritativo, con actualizaciones en vivo y recuperación mediante el flujo existente.
- Seguir la composición del mockup `docs/assets/visual-references/mockups/04-pista-progreso-envios-ruta-tv-ventana-oscura.png`: «PISTA PÚBLICA», pista, instrucción «Elige en tu teléfono una carta que pueda contar esta historia.», «CARTAS RECIBIDAS», conteo, círculos anónimos y mensaje de espera.
- Reutilizar el diseño oscuro, la textura, el marco y las ramas decorativas de la ventana existente. El mockup completo no es un recurso de interfaz.
- Representar círculos anónimos, sin asociarlos a jugadores: uno por envío esperado, con tantos marcados como envíos recibidos. Acompañarlos de un conteo textual accesible.
- Mostrar «Esperando las cartas…» cuando falten varias y «Esperando la última carta…» cuando falte una. Si se recibe un estado de fase con el conteo completo, mostrar «Todas las cartas recibidas» sin avanzar de fase desde la interfaz.
- Si falta la pista, mostrar «Pista no disponible» y conservar el progreso disponible. Permitir pistas largas en varias líneas y, si es necesario, desplazamiento accesible de la pista sin ocultar el conteo.
- Durante una desconexión conservar el último estado disponible e indicar la conexión con el indicador global existente; al recuperar la conexión, representar el snapshot actualizado.
- Actualizar la sección «Clue And Submission Progress» de `docs/08-screen-specifications.md` para reflejar la composición oscura central acordada y mantener privacidad y accesibilidad.

**Fuera de alcance:**

- Cambiar el fondo, el recorrido, los nodos, las ramas existentes o la posición del indicador global de conexión.
- Implementar la pantalla de votación o nuevas fases, controles de TV, reglas, cambios de backend, contratos o persistencia.
- Mostrar cartas, manos, propietarios, identidades pendientes o votos; asociar círculos con personas.
- Cambiar el móvil o Blazor, añadir imágenes, dependencias o una suite Playwright al repositorio.

## Modelo de datos

No se introducen modelos persistentes ni contratos nuevos. Se reutiliza `RoomStateResponse` desde `src/ImaginArte.Client.React/src/models/roomState.ts`:

```ts
room.phase                    // fase autoritativa
room.clue                     // pista publicada o null
room.submissionCount          // envíos de jugadores recibidos
room.expectedSubmissionCount  // envíos de jugadores esperados
```

Los conteos ya excluyen la carta del narrador según el dominio; la interfaz no calcula quién debe enviar ni modifica la fase. La ventana no guarda datos de juego ni simula progreso. Las actualizaciones proceden del estado público existente; tras conectar o reconectar se recupera un snapshot actual, sin reproducir eventos perdidos.

## Plan de implementación

1. Crear `TvSubmissionProgressScreen.tsx` y su CSS para representar pista, instrucción, conteo y círculos anónimos a partir de la prop pública `room`; reutilizar `CeremonialWindowDecorations` y la textura existente. Cubrir pista ausente y textos de espera sin acciones de juego.
2. Integrar la ventana en `TvLobbyScreen.tsx` solo para `GuessersSubmitting`, conservando el comportamiento existente de las demás fases, la ruta y el indicador de conexión. Comprobar actualizaciones reales y recuperación del snapshot sin añadir suscripciones ni mutaciones.
3. Ajustar el contenido a 1920 × 1080 y 1280 × 720, pistas extensas, acceso al desplazamiento con teclado y movimiento reducido; mantener conteo e indicador global visibles.
4. Actualizar `docs/08-screen-specifications.md` con la ventana oscura central y sus estados, sin modificar sus límites de privacidad ni las reglas del juego.

### Archivos previstos

**Nuevos:** `src/ImaginArte.Client.React/src/screens/tv/TvSubmissionProgressScreen.tsx` y `TvSubmissionProgressScreen.css`.

**Modificados:** `src/ImaginArte.Client.React/src/screens/tv/TvLobbyScreen.tsx` y `docs/08-screen-specifications.md`. Ajustar `TvLobbyScreen.css` solo si la integración lo necesita sin alterar la ruta ni el indicador global.

**Reutilizados:** `src/ImaginArte.Client.React/src/components/CeremonialWindowDecorations.tsx`, sus estilos y los activos de ventana existentes, además del flujo público de `roomRealtime.ts` y `roomsApi.ts`.

## Tutoría React

- **Objetivo observable:** la TV muestra la pista real y cómo aumenta el conteo anónimo al recibir cartas.
- **Conceptos:** una prop es un dato de entrada de solo lectura; el renderizado condicional elige qué vista mostrar según la fase; un snapshot es una fotografía del estado del servidor.
- **Analogía con Blazor:** `src/ImaginArte.Client.Blazor/Pages/TvLobby.razor` también representa la sala pública. En React, el componente recibe props y vuelve a ejecutarse cuando cambian; no necesita llamar a `StateHasChanged` para convertir esos datos en contenido visual.
- **Flujo:** snapshot público → estado de `TvLobbyScreen` → prop `room` → ventana; los jugadores envían cartas por móvil y el servidor publica el estado actualizado. No hay intención ni comando desde esta ventana. Al reconectar se solicita el snapshot actual.
- Explicar en español archivos, primer renderizado, actualizaciones, recuperación, desmontaje y verificación; cerrar con recapitulación didáctica y glosario breve.

## Criterios de aceptación

- [x] En `GuessersSubmitting`, la TV muestra la ventana central y no el contenido de lobby; la ventana se retira al cambiar de fase sin implementar votación. **Evidencia:** `TvLobbyScreen.tsx:433-437`; Playwright en `http://localhost:5174/tv/TV13QA` mostró la ventana durante fase 2; después del envío final (API fase 3) dejó de mostrarla y no apareció una pantalla de votación.
- [x] La pista y el conteo corresponden a `clue`, `submissionCount` y `expectedSubmissionCount` del snapshot público, y se actualizan al recibir nuevos estados. **Evidencia:** `TvSubmissionProgressScreen.tsx:9-17, 43-57`; en sala real por SignalR el conteo cambió de 0/2 a 1/2 (`.playwright-mcp/spec13-progress-1-of-2-1280x720.png`).
- [x] La ventana muestra los títulos, instrucción, conteo y círculos anónimos acordados; la cantidad de círculos marcados y totales coincide con los conteos públicos. **Evidencia:** `TvSubmissionProgressScreen.tsx:25-57`; snapshot Playwright 1280 × 720 mostró “PISTA PÚBLICA”, instrucción, 1/2 y un círculo marcado de dos.
- [x] El mensaje distingue varias cartas pendientes, la última carta y el estado completo, sin identificar personas ni decidir la transición. **Evidencia:** `TvSubmissionProgressScreen.tsx:11-17, 46-57`; vistas reales 0/2 y 1/2 mostraron los dos mensajes pendientes; fixture de snapshot público 2/2 en `TV13CO` mostró “Todas las cartas recibidas” y 2/2 círculos marcados, sin acción de fase.
- [x] Una pista ausente muestra «Pista no disponible» sin inventar contenido y conserva el conteo. **Evidencia:** fixture de snapshot público con `clue: null` en Playwright (`TV13MI`) mostró “Pista no disponible” y conservó 1/3 (`.playwright-mcp/spec13-missing-clue-1280x720.png`).
- [x] Una pista larga puede leerse completa mediante líneas y desplazamiento accesible por teclado cuando sea necesario, sin ocultar el conteo. **Evidencia:** `TvSubmissionProgressScreen.tsx:28-35`, `TvSubmissionProgressScreen.css:47-68`; con pista de 4525 caracteres/24 líneas, Tab enfocó la región nombrada y ArrowDown/End la desplazaron hasta el final (`scrollTop: 4727`, máximo 4728); conteo visible en `.playwright-mcp/spec13-long-clue-keyboard-scroll-bottom-1280x720.png`.
- [x] La ventana no muestra manos, imágenes de cartas enviadas, propietarios, jugadores pendientes, votos ni acciones de TV. **Evidencia:** `TvSubmissionProgressScreen.tsx:9-57` solo consume pista y conteos; el snapshot accesible y las capturas del progreso real no muestran manos, cartas, identidades, votos ni acciones de TV.
- [x] Ante desconexión se conserva el último estado y se muestra el estado global de conexión; tras reconectar se obtiene y representa el snapshot actual, incluso si la fase cambió. **Evidencia:** Playwright MCP, sala real `TV13RY`, viewport 1280 × 720: con el navegador offline la TV conservó 0/2 y mostró «Reconectando…» aunque la API ya informaba fase 2 y 1/2 (`.playwright-mcp/spec13-reconnect-offline-retains-0-of-2-1280x720.png`). Al restablecer la red, `GET /rooms/TV13RY` (respuesta Playwright #60, HTTP 200) devolvió fase 2, 1/2; la TV representó 1/2, «Esperando la última carta…» y «Conectado en tiempo real» (`.playwright-mcp/spec13-reconnect-same-phase-1-of-2-1280x720.png`). En la segunda desconexión retuvo 1/2 (`.playwright-mcp/spec13-reconnect-before-phase-change-1-of-2-1280x720.png`); el envío final devolvió fase 3 y 2/2; tras reconectar, `GET /rooms/TV13RY` (respuesta Playwright #65, HTTP 200) devolvió ese snapshot, retiró la ventana de progreso y restableció la conexión (`.playwright-mcp/spec13-reconnect-phase-change-3-1280x720.png`). No apareció una pantalla de votación; quedó la vista fallback existente. `roomRealtime.ts:46-76` marca la reconexión, vuelve a suscribir y carga el snapshot HTTP actualizado.
- [x] Se reutilizan la ventana oscura, textura, marco y ramas existentes sin usar la captura completa; fondo, recorrido e indicador global mantienen su diseño y posición. **Evidencia:** `TvSubmissionProgressScreen.tsx:1-3, 18-24` reutiliza `CeremonialWindowDecorations`; el CSS usa la textura y el fondo oscuro (`TvSubmissionProgressScreen.css:13-24`), y `.playwright-mcp/spec13-guessers-submitting-1920x1080.png` muestra la ventana, el recorrido y el indicador inferior derecho sin usar la captura completa. **Aprobación explícita del usuario (2026-11-13):** aprueba la revisión visual pendiente de la decoración y la equivalencia de las ramas observadas con las existentes.
- [x] En 1920 × 1080 y 1280 × 720 el contenido es legible, no se recorta y no cubre el indicador global; la información no depende solo del color y respeta movimiento reducido. **Evidencia:** `.playwright-mcp/spec13-guessers-submitting-1920x1080.png` y `.playwright-mcp/spec13-progress-1-of-2-1280x720.png` muestran contenido sin recorte ni solapamiento; el progreso incluye conteo textual y marca; Playwright con `prefers-reduced-motion: reduce` confirmó `transform: none`. **Aprobación explícita del usuario (2026-11-13):** aprueba la legibilidad en ambas capturas. Se conserva la medida observada: a 1280 × 720 la instrucción y el mensaje se renderizan a 14,4 px (`TvSubmissionProgressScreen.css:70-75`), por debajo del objetivo inicial de 28 px para texto principal de TV (`docs/06-design-system.md:127-141`); la aprobación no cambia ni oculta esa diferencia.
- [x] `docs/08-screen-specifications.md` describe la composición central oscura y conserva los límites de privacidad. **Evidencia:** `docs/08-screen-specifications.md:113-145` especifica ventana oscura central, información agregada, privacidad, estados y accesibilidad.
- [x] Pasan `npm run typecheck`, `npm run lint` y `npm run build` desde `src/ImaginArte.Client.React`; la revisión de navegador cubre progreso real, pista ausente y larga, reconexión, cambio de fase y privacidad sin instalar Playwright en el repositorio. **Evidencia:** desde `src/ImaginArte.Client.React`, `npm run typecheck`, `npm run lint` y `npm run build` finalizaron correctamente. En la sala real `TV13LV`, Playwright MCP observó 0/2 y, tras `POST /rooms/TV13LV/guess-cards` (HTTP 200), el evento SignalR actualizó la TV a 1/2, con un círculo marcado y «Esperando la última carta…»; el DOM visible no incluyó nombres de jugadores QA ni IDs `TEMP-*` (`.playwright-mcp/spec13-live-progress-1-of-2-1280x720.png`). Una respuesta de snapshot de prueba con `clue: null`, fase 2 y conteo 1/3 mostró «Pista no disponible», mantuvo el conteo y los tres círculos (`.playwright-mcp/spec13-missing-clue-1280x720.png`). Con pista real de 6.229 caracteres y 24 líneas en `TV13LG`, Tab enfocó la región «Pista pública» y PageDown llevó el desplazamiento al máximo (5.538/5.538); 0/2 y el mensaje de espera siguieron visibles (`.playwright-mcp/spec13-long-clue-keyboard-scroll-bottom-actual-1280x720.png`). La revisión de reconexión y cambio de fase está documentada arriba y se confirmó que la vista de progreso no expone nombres ni IDs de cartas. `package.json` y `package-lock.json` no incluyen Playwright; no se instaló en el repositorio.

## Decisiones

- **Sí:** conectar datos reales desde el inicio; el contrato público ya expone pista y conteos.
- **Sí:** presentar pista y progreso en la ventana oscura central existente y actualizar la guía, en vez de mantener su composición anterior con pista clara fuera del centro.
- **Sí:** reproducir la jerarquía del mockup y círculos puramente agregados, sin atribuirlos a jugadores.
- **Sí:** conservar el último estado durante la desconexión y recuperar el estado autoritativo después.
- **Sí:** mantener el progreso visible cuando la pista extensa requiera desplazamiento accesible.
- **No:** mantener la ventana después de `GuessersSubmitting` ni implementar votación; la fase siguiente corresponde a otra spec.
- **No:** rediseñar el tablero, agregar acciones o duplicar reglas del servidor.

## Riesgos

| Riesgo | Mitigación |
| --- | --- |
| Los círculos se interpretan como jugadores identificables. | No añadir nombres, avatares ni asociaciones; el conteo es colectivo. |
| Una pista extensa desborda la ventana. | Permitir varias líneas y desplazamiento accesible, manteniendo visible el conteo. |
| El estado queda desactualizado durante una desconexión. | Mostrar la conexión y recuperar un snapshot nuevo al reconectar. |
| La fase avanza antes de que se perciba el conteo completo. | Respetar la fase autoritativa; no retrasar ni simular transiciones en React. |

## Qué **no** incluye esta spec

- Votación, revelación, cambios de reglas, backend o contratos.
- Rediseño del fondo, tablero, indicador de conexión o pantallas móviles.
- Nuevos activos, dependencias, persistencia o pruebas E2E instaladas en el repositorio.
