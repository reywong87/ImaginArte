# SPEC 16 — Votación de cartas en móvil

> **Estado:** Approved  
> **Depende de:** SPEC 09 — Catálogo local de cartas; SPEC 14 — Espera móvil tras enviar una carta; SPEC 15 — Votación de cartas en TV  
> **Fecha:** 2026-11-19  
> **Objetivo:** Permitir que el jugador vote en privado desde el móvil y recupere el estado de su voto sin revelar qué carta eligió.

## Alcance

**Incluye:**

- Durante `Voting`, mostrar al jugador que aún no votó la pista y las cartas en orden ascendente de `displayNumber`, con los mismos números que en TV. Mostrar su carta visible pero deshabilitada, con la etiqueta «Tu carta» y una explicación. El narrador permanece en espera, sin controles de voto.
- Usar `docs/assets/visual-references/mockups/14-votar-carta-movil.png` como referencia para el marco oscuro, la pista visible y la cuadrícula desplazable de dos columnas. Si hay un número impar de cartas, la última queda en la columna izquierda. Mantener el arte en proporción `2:3`, con números y etiquetas fuera de la ilustración.
- Mantener el botón de confirmación en una franja inferior que respete el área segura sin tapar las cartas. Mostrar la selección con texto y contorno, no solo color. Permitir cambiarla antes de confirmar.
- Enviar el voto al servidor solo al confirmar. Bloquear envíos simultáneos y, tras confirmarse, retirar la cuadrícula y mostrar «Voto enviado», sin indicar el destino.
- Si se pierde la conexión o la respuesta del envío, consultar el estado privado actualizado sin reenviar el voto. Si el servidor confirma que no hubo voto y la fase sigue abierta, permitir un nuevo intento explícito. Si la fase cerró sin confirmación de voto, mostrar una espera neutral.

**Fuera de alcance:**

- Diseño completo del mockup 15, revelación y resultados.
- Cambios de comportamiento en TV o Blazor, nuevas reglas, endpoints, persistencia o dependencias.

## Modelo de datos

- El servidor añade `HasVoted` a la respuesta privada. Lo obtiene del voto registrado en la ronda actual, sin añadir un campo persistente. El indicador no se incorpora al snapshot público ni incluye el destino del voto.
- React lee `hasVoted`, `isCurrentStoryteller`, `submittedVotingCardId`, `room.phase`, `room.clue` y `room.votingCards`. Usa `votingCardId` para identificar cada opción y la carta propia, `cardId` para resolver la ilustración y `displayNumber` para ordenar y numerar.
- La selección sin confirmar y el estado de envío son temporales. No se conservan entre sesiones ni se usan como prueba de que el servidor registró el voto.

## Plan de implementación

1. Añadir `HasVoted` a `src/ImaginArte.Application/Rooms/GetPlayerPrivateState.cs` y `src/ImaginArte.Contracts/Rooms/RoomHttpContracts.cs`. Mapearlo en `src/ImaginArte.Server/RoomHttpMappings.cs`. Añadir cobertura en las suites de Application y Server para el estado antes y después de votar, el reinicio entre rondas y la ausencia del indicador en el estado público.
2. Actualizar `src/ImaginArte.Client.React/src/models/roomState.ts` para leer el indicador privado y `src/ImaginArte.Client.React/src/services/roomsApi.ts` para usar el comando existente `POST /rooms/{roomCode}/votes`.
3. Crear `src/ImaginArte.Client.React/src/screens/mobile/MobileVotingScreen.tsx` y `MobileVotingScreen.css` en el mismo directorio. Construir la vista con datos de entrada y acciones de selección y confirmación. Reutilizar `src/ImaginArte.Client.React/src/services/cardAssets.ts` para las imágenes.
4. Integrar la vista en `src/ImaginArte.Client.React/src/screens/mobile/MobileJoinScreen.tsx`, distinguiendo votación, confirmación básica y espera neutral. Reutilizar la conexión móvil y la recuperación por snapshot existentes. Actualizar `docs/08-screen-specifications.md` con la composición acordada y la transición provisional tras votar.

## Tutoría React

- **Objetivo observable:** elegir una carta en el móvil, confirmar el voto y recuperar «Voto enviado» tras recargar, sin mostrar cuál fue la carta.
- **Analogía con Blazor:** `src/ImaginArte.Client.Blazor/Pages/JoinRoom.razor` usa un campo local para recordar que votó durante la vida del componente. En React, el estado local también sirve para recordar la selección mientras la vista está abierta, pero no prueba que el voto quedó registrado. React vuelve a ejecutar la vista cuando cambia su estado, sin llamar a `StateHasChanged`.
- **Flujo:** snapshot privado → estado de `MobileJoinScreen` → pantalla. Selección local → confirmación explícita → comando HTTP → resultado. Conexión o reconexión → snapshot nuevo → pantalla vigente, sin reproducir eventos perdidos ni repetir el comando.
- Explicar en español qué ocurre en el primer renderizado, al seleccionar y confirmar, al recuperar la conexión y al salir de la pantalla. Cerrar con una comprobación reproducible y un resumen de la diferencia entre selección local y voto registrado.

## Criterios de aceptación

- [x] En una sala real, móvil y TV coinciden en orden y números. Solo el propietario ve su carta marcada «Tu carta» y no puede seleccionarla. El narrador no ve controles de voto.
- [x] Seleccionar y cambiar de carta no envía ningún comando. Confirmar emite un único `POST` aunque se pulse repetidamente durante el envío.
- [x] Tras el éxito, incluido el último voto de la ronda, se muestra «Voto enviado» sin cuadrícula, número ni miniatura del destino.
- [x] Recargar tras votar, reconectar o perder la respuesta de un voto registrado recupera «Voto enviado» sin repetir el `POST`. Si no se registró y la fase sigue abierta, se permite reintentar explícitamente. Si ya cerró, no se ofrece votar ni se inventa una confirmación.
- [x] `HasVoted` se limita al jugador y la ronda actuales. El snapshot público no lo incluye y, durante `Voting`, no expone votos individuales. La pantalla móvil no muestra votos ajenos ni identifica la carta del narrador.
- [x] A 390 × 844 y 360 × 568, con hasta ocho cartas, no hay desbordamiento horizontal ni solapes. La última carta impar queda a la izquierda. Pista y confirmación siguen accesibles al desplazar la cuadrícula.
- [x] La selección y la confirmación funcionan con teclado, tienen foco visible y ofrecen objetivos táctiles de al menos 44 px. Etiquetas y números no invaden las ilustraciones.
- [x] Pasan las pruebas de Application y Server pertinentes y `npm run typecheck`, `npm run lint` y `npm run build` desde `src/ImaginArte.Client.React`. La revisión en navegador comprueba la pérdida de respuesta y la ausencia de reenvío.

## Evidencia de verificación parcial

Se verificaron los criterios 3, 7 y 8 en la sala local real `HJQJMJ`, con ocho jugadores, cliente en `localhost:5174` y servidor en `localhost:5081`. El criterio 4 sigue sin verificarse por completo: no se probaron la reconexión ni el cierre de fase sin voto registrado.

| Criterio | Evidencia y brecha |
| --- | --- |
| 1. Orden y roles | En la sala local real `S4F2M5` (7 jugadores), Playwright mostró en TV (1280 × 720) los números 1–7 y en los seis móviles (390 × 844) «Carta 1»–«Carta 7» en el mismo orden. Cada votante vio solo su carta propia marcada «Tu carta» y deshabilitada; el narrador vio «Esperando a que continúe la partida», sin radios, región ni controles de voto. Capturas: `.playwright-mcp/spec16-voting-tv-1280x720.png` y `.playwright-mcp/spec16-storyteller-waiting-voting-390x844.png`. |
| 2. Envío único | En la sala nueva `8UR3HY`, Playwright mantuvo pendiente el `POST /rooms/8UR3HY/votes` y pulsó cinco veces más el botón deshabilitado durante el envío: el handler observó un único `POST`, HTTP 200 y la pantalla pasó a «Voto enviado». Captura: `.playwright-mcp/spec16-c2-repeat-submit-sending-390x844.png`. |
| 3. Confirmación | En el último voto válido, `route.fetch()` recibió HTTP 200 con `voteCount=7` y `expectedVoteCount=7`; luego `route.abort('failed')` impidió entregar la respuesta al móvil. Tras recuperar el estado, a 360 × 568 mostró «Voto enviado» sin cuadrícula, número ni miniatura del destino. Capturas: `spec16-lost-response-last-vote-before-reload-360x568.png` y `spec16-lost-response-last-vote-after-reload-360x568.png`. |
| 4. Recuperación | Sigue vigente la evidencia documentada de pérdida de respuesta/recarga y reintento ante HTTP 400. En `K5M7QC`, después de un voto HTTP 200, la reconexión solicitó un snapshot privado nuevo (`hasVoted=true`, `Voting`, `1/2`), mantuvo «Voto enviado» y no repitió `POST /votes`. Para cubrir el cierre sin voto de un votante elegible, en `Q6D9XT` Playwright abortó el `POST`; el servidor real siguió en `Voting`, `hasVoted=false`, `voteCount=0/2`. Se entregó a la UI una copia del snapshot privado con el formato real, cambiando `phase` a `VoteReveal` y dejando `hasVoted=false`: mostró espera neutral sin cuadrícula, confirmación ni reenvío. **Esta última comprobación es una simulación de UI, no un estado reproducible de partida real**, pues el servidor cierra la fase cuando se reciben todos los votos. En `W3F8NK`, un aborto con fase abierta (`Voting`, `hasVoted=false`, `0/2`) habilitó el reintento explícito; sin otro clic no hubo reenvío, y el clic explícito produjo HTTP 200 y `voteCount=1`. Capturas: `.playwright-mcp/spec16-c4-reconnect-after-recorded-vote-reconnecting-390x844.png`, `.playwright-mcp/spec16-c4-reconnect-after-recorded-vote-restored-390x844.png`, `.playwright-mcp/spec16-c4-closed-voter-aborted-post-simulated-votereveal-390x844.png`, `.playwright-mcp/spec16-c4-open-aborted-post-explicit-retry-390x844.png` y `.playwright-mcp/spec16-c4-open-aborted-post-explicit-retry-confirmed-390x844.png`. |
| 5. Privacidad | Pasaron `dotnet test tests/ImaginArte.Application.Tests/ImaginArte.Application.Tests.csproj --no-restore --filter FullyQualifiedName~GetPlayerPrivateState_HasVoted_TracksPlayerAndCurrentRound` (1/1) y `dotnet test tests/ImaginArte.Server.Tests/ImaginArte.Server.Tests.csproj --no-restore --filter FullyQualifiedName~GetPlayerPrivateState_HasVoted_IsPrivateAndReflectsRecordedVotes` (1/1). En `S4F2M5`, tras un voto real, el snapshot privado lo marcó solo para su autor; el público conservó `voteCount=1` pero `votes=[]` y no incluyó `hasVoted`. El móvil de otro votante no mostró nombres ni votos ajenos. `GetPlayerPrivateState.cs:54–64`, `RoomHttpMappings.cs:39–69`, `RoomHttpContracts.cs:68–72` y `MobileVotingScreen.tsx:69–108` respaldan que el indicador/voto propio son privados y la carta del narrador no se identifica. Captura móvil: `.playwright-mcp/spec16-long-clue-odd-grid-360x568.png`. |
| 6. Disposición | La verificación previa en `HJQJMJ` cubrió ocho cartas y pista corta en ambos viewports, sin desbordamiento. En `S4F2M5`, con siete cartas (impar) y pista de 261 caracteres, Playwright confirmó a 390 × 844 y 360 × 568 ausencia de overflow horizontal y solapes; la carta 7 quedó bajo la columna izquierda. A 360 × 568, la cuadrícula desplazó sus cartas mientras pista y confirmación permanecieron visibles; las etiquetas quedaron debajo del arte. Capturas: `.playwright-mcp/spec16-long-clue-odd-grid-390x844.png`, `.playwright-mcp/spec16-long-clue-odd-grid-360x568.png` y `.playwright-mcp/spec16-long-clue-odd-grid-bottom-360x568.png`. |
| 7. Accesibilidad | Espacio/Flecha derecha seleccionaron y cambiaron la carta. Tab enfocó «Confirmar voto» con un contorno de 3 px y Enter activó el envío. El botón midió 54 px de alto y las opciones superaron 44 px. En las cartas 5 y 6, el número y la etiqueta quedaron debajo de la ilustración. |
| 8. Pruebas | En la revisión previa pasaron las 339 pruebas de `dotnet test ImaginArte.sln --no-restore -m:1`. Tras el último ajuste de la pista pasaron `npm run typecheck`, `npm run lint` y `npm run build`. Esta revisión simuló la pérdida de respuesta del último voto: un `POST` contado, recuperación de «Voto enviado» y ningún reenvío tras recargar. |

Capturas en `.playwright-mcp/`: `spec16-mobile-voting-initial-390x844.png`, `spec16-mobile-voting-360x568.png`, `spec16-voting-grid-bottom-360x568.png`, `spec16-vote-not-recorded-retry-open-360x568.png`, `spec16-lost-response-last-vote-before-reload-360x568.png` y `spec16-lost-response-last-vote-after-reload-360x568.png`.

## Decisiones


- **Sí:** voto real desde este incremento, con el comando HTTP existente y un indicador privado autoritativo.
- **Sí:** confirmación básica sin destino; el mockup 15 se reserva para otro incremento.
- **Sí:** dos columnas con la última carta impar a la izquierda y confirmación en una franja inferior que no tape el contenido.
- **No:** deducir si el jugador votó del conteo público, persistir la selección en el navegador o reenviar automáticamente un voto.

## Riesgos

| Riesgo | Mitigación |
| --- | --- |
| El servidor registra el voto pero se pierde la respuesta. | Consultar el snapshot privado antes de volver a ofrecer un envío. |
| El último voto cambia la fase mientras llega la confirmación. | Usar la confirmación del servidor para mostrar la espera sin destino; no deducirla solo del cambio de fase. |
| La franja inferior tapa cartas en pantallas pequeñas. | Separarla del área desplazable y verificar ambos tamaños con el máximo de cartas. |

## Qué **no** incluye esta spec

- Diseño completo de la espera tras votar, revelación o resultados personales.
- Cambios en TV o Blazor, nuevas reglas, endpoints, persistencia o dependencias.
