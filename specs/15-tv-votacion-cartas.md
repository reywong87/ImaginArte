# SPEC 15 — Votación de cartas en TV

> **Estado:** Approved  
> **Depende de:** SPEC 03 — Lobby TV con presencia en vivo; SPEC 07 — Recorrido dinámico del tablero en TV; SPEC 09 — Catálogo local de cartas; SPEC 13 — Ventana TV de progreso de envío de cartas  
> **Fecha:** 2026-11-18  
> **Objetivo:** Mostrar en la TV la pista, las cartas numeradas y el progreso colectivo real de la votación sin revelar quién votó, qué carta eligió ni a quién pertenece cada carta.

## Alcance

**Incluye:**

- Durante `Voting`, seguir la composición de `docs/assets/visual-references/mockups/05-votacion-cartas-pista-recorrido-tv.png`: «VOTACIÓN · LA PISTA», la pista y «Elige en tu teléfono la carta del narrador» arriba; cartas al centro; progreso abajo. Conservar el fondo, el recorrido y el indicador global de conexión. El mockup es una referencia, no un recurso de interfaz.
- Usar una cuadrícula adaptable de cartas verticales `2:3`. Con ocho cartas, mostrar dos filas de cuatro; con menos, centrar el conjunto sin alterar el orden. Mostrar el número de cada carta fuera de la ilustración, en alto contraste. No animar ni reordenar la cuadrícula al recibir votos.
- Mostrar «5/7 votos recibidos» como formato, con los valores reales y círculos anónimos como en la pantalla 04. Los círculos no representan a personas concretas.
- Si falta la pista, mostrar «Pista no disponible». Si es larga, permitir leerla completa mediante desplazamiento accesible con teclado sin ocultar las cartas ni el progreso.
- Si falta una imagen o falla su carga, mostrar «Imagen no disponible» en el mismo espacio, con el mismo número y sin mostrar el ID técnico.
- Durante una desconexión, mantener la última vista y mostrar el estado de conexión existente. Al reconectar, representar el snapshot público actualizado.
- Cuando el servidor indique `VoteReveal`, retirar la cuadrícula y mostrar «Votación cerrada» sobre el tablero, sin revelar votos ni ofrecer botones. Un conteo completo por sí solo no cambia la pantalla.

**Fuera de alcance:**

- Voto móvil, revelación, resultados y acciones de juego en TV.
- Cambios de backend, reglas, contratos, persistencia o cliente Blazor.
- Activos y dependencias nuevos, instalación de una suite Playwright o rediseño del tablero.

## Modelo de datos

No se introducen contratos ni datos persistentes. Se reutiliza `RoomStateResponse` de `src/ImaginArte.Client.React/src/models/roomState.ts`: `phase`, `clue`, `voteCount`, `expectedVoteCount` y `votingCards`. De cada carta se usa `votingCardId` como identidad estable, `cardId` para resolver la imagen y `displayNumber` para ordenar y numerar. La pantalla no usa `votes` ni `revealedCards`, ni reconstruye el progreso a partir de jugadores o votos individuales.

## Plan de implementación

1. Crear `src/ImaginArte.Client.React/src/screens/tv/TvVotingScreen.tsx` y `TvVotingScreen.css` para representar la pista, la cuadrícula numerada y el conteo a partir de datos públicos de entrada.
2. Resolver las ilustraciones mediante `src/ImaginArte.Client.React/src/services/cardAssets.ts`. Mantener el espacio y el número cuando falte la imagen o falle su carga. Ajustar la cuadrícula y la pista larga a 1080p y 720p.
3. Integrar la pantalla en `src/ImaginArte.Client.React/src/screens/tv/TvLobbyScreen.tsx` para `Voting` y el mensaje neutro para `VoteReveal`. Reutilizar la suscripción y la recuperación públicas existentes, sin abrir otra conexión ni emitir comandos desde TV. Ajustar `TvLobbyScreen.css` solo si lo exige el espacio central.
4. Actualizar la sección **Voting** de `docs/08-screen-specifications.md` con la composición, el formato `X/Y votos recibidos` y la salida neutra temporal, sin cambiar las reglas de revelación.

## Tutoría React

- **Objetivo observable:** ver en la TV la pista, las cartas en orden estable y cómo aumenta el progreso sin identificar a los votantes.
- **Analogía con Blazor:** `src/ImaginArte.Client.Blazor/Pages/TvLobby.razor` también elige qué mostrar según la fase recibida. El componente React recibe datos de entrada de solo lectura (*props*) y vuelve a ejecutarse cuando cambian, sin llamar a `StateHasChanged`.
- **Flujo:** snapshot público → estado de `TvLobbyScreen` → vista de votación. Los votos se envían fuera de la TV. El servidor publica cambios por SignalR y, tras reconectar, la TV solicita el estado actual sin reproducir eventos perdidos.
- Explicar en español los archivos, el primer renderizado, las actualizaciones, la recuperación y el desmontaje. Cerrar con una comprobación reproducible: desconectar la TV antes del último voto y observar cómo el snapshot nuevo reemplaza la cuadrícula por «Votación cerrada».

## Criterios de aceptación

- [ ] Al entrar en `Voting`, la TV muestra la pista y todas las cartas autorizadas por el snapshot público, en orden ascendente de `displayNumber`. Los números y posiciones se conservan al recibir nuevos votos.
- [ ] En una sala real, los votos enviados mediante la API existente actualizan `X/Y votos recibidos` y los círculos sin recargar la TV. Cada círculo marcado representa un voto recibido de forma anónima y el progreso también se comunica en texto.
- [ ] Durante `Voting` no se muestran manos, propietarios, la identidad de la carta del narrador, votos individuales, identidades de votantes o pendientes, indicadores de selección ni controles de votación.
- [ ] Una pista ausente muestra «Pista no disponible». Una pista larga se puede leer completa con el teclado sin ocultar la cuadrícula ni el conteo. Una imagen ausente o fallida conserva el espacio y el número y muestra «Imagen no disponible», sin exponer el ID técnico.
- [ ] A 1920 × 1080 y 1280 × 720, con el mínimo y máximo de cartas admitido, las ilustraciones mantienen `2:3` y los números quedan fuera del arte. No hay recortes ni solapes con el recorrido o el indicador de conexión. La pista, los números y el conteo se leen a 2–3 m. La cuadrícula permanece estática, también con movimiento reducido.
- [ ] Al desconectar, se conserva el último estado con el indicador de conexión visible. Al reconectar, se refleja el snapshot fresco, tanto si sigue en `Voting` como si ya pasó a `VoteReveal`.
- [ ] `VoteReveal` muestra «Votación cerrada» en lugar de la cuadrícula, sin resultados, revelaciones ni botones. Un snapshot todavía en `Voting` con conteo completo no provoca una transición local.
- [ ] `docs/08-screen-specifications.md` describe la composición acordada, el formato de conteo y el estado neutro sin ampliar la información pública durante la votación.
- [ ] Pasan `npm run typecheck`, `npm run lint` y `npm run build` desde `src/ImaginArte.Client.React`. La revisión en navegador cubre progreso real, privacidad, recuperación y ambos tamaños sin instalar una suite nueva.

## Decisiones

- **Sí:** conectar datos reales desde el inicio y reutilizar el patrón `X/Y` de la pantalla 04. El servidor entrega el conteo; React solo lo muestra.
- **Sí:** priorizar la legibilidad, la proporción de las cartas y la privacidad sobre una copia exacta del mockup.
- **No:** prolongar artificialmente el conteo final ni implementar una revelación parcial. El servidor decide cuándo termina la votación.

## Riesgos

| Riesgo | Mitigación |
| --- | --- |
| El último voto cierra la fase antes de que se vea `Y/Y`. | Mostrar «Votación cerrada» en cuanto el servidor confirme `VoteReveal`, sin retrasar la transición. |
| La pista larga o el máximo de cartas invaden el recorrido. | Limitar el espacio de la pista con desplazamiento accesible y comprobar ambos tamaños de TV con el máximo de cartas. |
| El estado de revelación trae datos que no corresponden a la vista neutra. | Renderizar únicamente «Votación cerrada» en `VoteReveal`, sin pasar datos de votos ni resultados a esa vista. |

## Qué **no** incluye esta spec

- Voto móvil, revelación, resultados ni nuevas acciones de juego.
- Cambios en reglas, backend, contratos, persistencia, Blazor o rediseño del tablero.
- Activos, dependencias o una suite de navegador nuevos.
