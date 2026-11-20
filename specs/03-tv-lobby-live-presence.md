# SPEC 03 — Lobby TV con presencia en vivo

> **Estado:** Implemented  
> **Depende de:** SPEC 02 — Inicio TV: creación y reanudación de sala  
> **Fecha:** 2026-09-28  
> **Objetivo:** Implementar el lobby de TV en React con QR móvil, roster en tiempo real, selección del puntaje objetivo y transición animada desde `/tv`, manteniendo fijo el paisaje.

## Scope

**In:**

- Sustituir el placeholder de `/tv/{roomCode}` por el lobby de TV.
- Mostrar el código de sala como alternativa manual al QR, los jugadores unidos, el progreso de ocupación y la puntuación objetivo.
- Permitir elegir en el lobby el modo **Corta** (15 puntos), **Media** (25 puntos) o **Larga** (35 puntos). Las salas nuevas empiezan en **Media**.
- Guardar cada cambio de modo inmediatamente en el servidor mediante `PUT /rooms/{roomCode}/target-score`. El snapshot actualizado es la fuente autoritativa y se publica a la sala por SignalR.
- Permitir cambiar el modo mientras la sala está en espera de jugadores. El servidor rechaza cambios una vez iniciada la partida. Si falla el guardado, conservar seleccionado el último valor confirmado, mostrar un error accesible y permitir reintentar.
- Actualizar el roster y la presencia con SignalR. Mostrar «Conectado» si el jugador tiene una o más conexiones SignalR activas. Si no tiene ninguna, conservarlo en la lista con el estado neutral «Reconectando».
- Identificar visualmente al primer jugador como «Anfitrión». Esta etiqueta no concede permisos; el control de inicio permanece en la TV.
- Mostrar avatares temporales tomados del atlas `docs/assets/visual-references/mockups/avatares-jugadores-16.png`. La asignación será aleatoria, única por jugador dentro de la sala y estable en esa pestaña. La elección de avatar desde el móvil pertenece a otro spec.
- Generar un QR hacia el cliente Blazor móvil actual, con destino `/join/{roomCode}` y base configurable mediante `VITE_MOBILE_JOIN_BASE_URL`. Si falta la configuración, mantener visible el código manual y mostrar un aviso accesible. No generar un QR inválido.
- Mostrar «Sala completa» al alcanzar 8/8 jugadores. El inicio continúa disponible si ya se alcanzó el mínimo requerido.
- Mantener deshabilitado **Iniciar partida** hasta alcanzar el mínimo de jugadores. Un clic cuando está habilitado envía `POST /rooms/{roomCode}/start`; no abre un segundo diálogo.
- Tras iniciar, mostrar una pantalla mínima de espera con el nombre del narrador y el mensaje de que está eligiendo una carta y una pista.
- Animar la transición desde la pantalla `/tv`: el paisaje permanece fijo y se animan los elementos de interfaz superpuestos. Respetar `prefers-reduced-motion`.
- Cargar el snapshot inicial por HTTP y suscribirse a SignalR. Tras reconectar, obtener un snapshot actual; no reconstruir el estado reproduciendo eventos perdidos.
- Mantener los votos individuales fuera de los snapshots públicos mientras `Voting` esté abierta. Al recibirse el último voto, persistir la fase `VoteReveal` y publicar la relación votante-carta, manteniendo oculta la identidad de la carta del narrador.
- Exponer `POST /rooms/{roomCode}/reveal-storyteller-card` para pasar de `VoteReveal` a `Reveal`; guardar la transición antes de publicar `RoomUpdated`. El cálculo de puntos solo se permite después de esta acción.

**Fuera de alcance (para futuros specs):**

- La ruta visual de 25 nodos del tablero. Se aplaza explícitamente en este slice aunque `docs/08-screen-specifications.md` la contempla como contexto del lobby.
- Una pantalla de unión móvil React. El QR apunta al cliente Blazor existente.
- La selección y persistencia de avatares desde el móvil.
- La pantalla completa de espera al narrador y las fases posteriores; aquí solo se muestra el estado mínimo de espera acordado.
- Autorización real del jugador anfitrión. La etiqueta visual no cambia permisos ni traslada el control de inicio desde la TV.
- Cambios en el cálculo o reparto de puntos, la rotación del narrador u otras fases. La única fase de juego incluida es el estado server-side `VoteReveal` y la transición explícita a `Reveal`.
- Pantallas de juego React y controles para las dos revelaciones. Se implementarán en una spec posterior; este slice solo alinea el estado y los contratos del servidor.
- Actualización del cliente Blazor TV para las dos revelaciones. El cliente actual no podrá avanzar desde `VoteReveal` hasta que se implemente el futuro consumidor React.
- Una suite Playwright para React; el cliente todavía no tiene una suite ejecutable y no se introduce en este slice.

## Modelo de datos

El contrato público de sala añade los datos necesarios para presentar límites y presencia sin duplicar reglas del dominio en React:

```csharp
// Se añaden a RoomStateResponse:
int MinimumPlayers;
int MaximumPlayers;
Guid? HostPlayerId;

// Se añade a PlayerStateResponse:
bool IsConnected;
```

El cambio de modo usa el puntaje objetivo ya existente en `RoomStateResponse` y añade una solicitud HTTP:

```csharp
public sealed record UpdateRoomTargetScoreRequest(int TargetScore);
```

- `MinimumPlayers` y `MaximumPlayers` se derivan de los valores autoritativos del dominio.
- `HostPlayerId` identifica al primer jugador de la sala y es `null` mientras no haya jugadores. El servidor conserva el orden de incorporación en la colección pública de jugadores.
- `IsConnected` es `true` si el servidor mantiene una o más conexiones SignalR activas para ese jugador y sala.
- La presencia se mantiene en memoria en el servidor. No se persiste en snapshots ni en la base de datos.
- `TargetScore` sigue siendo la fuente de verdad para el modo: `15` corresponde a Corta, `25` a Media y `35` a Larga. La sala nueva conserva el valor predeterminado actual de `25`.
- El servidor solo acepta `15`, `25` o `35` al actualizar el objetivo y solo mientras la sala esté en `WaitingForPlayers`. El valor seleccionado se guarda con el estado existente de la sala y sobrevive a la recuperación del snapshot.
- No se añade una propiedad `GameMode`: la pantalla deriva la etiqueta del `TargetScore` recibido y el futuro spec del recorrido podrá usar ese mismo valor para determinar su tamaño.
- React asigna índices del atlas, del `0` al `15`, sin repetirlos en la sala. Guarda el mapa temporal en:

```text
sessionStorage["imaginarte.tv.roomAvatars.{roomCode}"] = { "{playerId}": 0..15 }
```

La asignación de avatar es estado visual temporal, no un dato de jugador ni una fuente de verdad del juego.

- `GamePhase.VoteReveal` y `RoomPhase.VoteReveal` representan que todos los votos están cerrados y el mapa votante-carta puede publicarse, pero la carta del narrador todavía no se ha revelado. Se añade con valor numérico `8`; los valores existentes `0` a `7` no cambian.
- En `Voting`, el snapshot público devuelve progreso agregado y no incluye votos por carta ni identidades de votantes. `PlayerPrivateStateResponse` conserva solo la confirmación privada del propio jugador.
- Al completarse la votación, el servidor transiciona y persiste `VoteReveal`. En esa fase, `VotingCards` puede incluir los votos con sus identidades, pero `RevealedCards` permanece vacío.
- `POST /rooms/{roomCode}/reveal-storyteller-card` permite pasar de `VoteReveal` a `Reveal`; desde `Reveal` se publican la carta del narrador y los datos de revelación existentes.

## Plan de implementación

1. **Extender los contratos públicos.** Añadir mínimo, máximo, anfitrión y estado de conexión al snapshot, además de la solicitud `UpdateRoomTargetScoreRequest`. Actualizar las pruebas de contrato y del endpoint.
2. **Hacer configurable el puntaje objetivo en el servidor.** Añadir una operación de dominio y aplicación que acepte solo `15`, `25` o `35` mientras la sala esté en `WaitingForPlayers`; persistir la sala y exponer `PUT /rooms/{roomCode}/target-score`. El endpoint devuelve el snapshot actualizado y publica `RoomUpdated` al grupo público solo tras guardar correctamente. Añadir pruebas de dominio, aplicación, endpoint y publicación realtime.
3. **Añadir presencia efímera al servidor.** Crear un rastreador por sala y jugador que gestione varias conexiones SignalR. Registrar al jugador al ejecutar `SubscribeAsPlayer`, retirar la conexión en `OnDisconnectedAsync` y publicar el estado actualizado al grupo público. Añadir pruebas para conexión, desconexión, reconexión y múltiples conexiones de una misma persona.
4. **Conectar el cliente React con la sala.** Añadir los modelos TypeScript, la consulta `GET /rooms/{roomCode}`, la actualización del puntaje objetivo y el adaptador SignalR usando `@microsoft/signalr`. Conectar la ruta existente a los estados de carga/error/snapshot y volver a solicitar un snapshot al reconectar.
5. **Reemplazar el placeholder por el lobby.** Mostrar código, roster, presencia, progreso, puntaje objetivo, anfitrión y avatares del atlas. Añadir el selector accesible de modo y reflejar solo valores confirmados por el servidor. Generar el QR con la base configurada y conservar la alternativa manual cuando falte.
6. **Conectar el inicio y el estado de espera.** Deshabilitar el botón según `MinimumPlayers`, evitar envíos duplicados y enviar `POST /start` al activarlo. Tras el éxito, bloquear el selector de modo y mostrar el narrador y la instrucción de espera usando el snapshot del servidor.
7. **Implementar la transición visual.** Mantener el fondo en un contenedor compartido entre `/tv` y `/tv/{roomCode}`. Animar el título, la tarjeta/semilla y los paneles de interfaz con Motion, que ya está instalado. Con movimiento reducido, pasar directamente al estado final o usar un fundido breve.
8. **Documentar la configuración.** Añadir `VITE_MOBILE_JOIN_BASE_URL` a `.env.example` y explicar en `README.md` que, para escanear desde un teléfono en la red local, la URL debe usar una dirección accesible desde ese dispositivo.
9. **Alinear la revelación pública.** Actualizar las reglas, la especificación de pantallas y estos criterios: ocultar votos durante `Voting`, publicar el mapa después del último voto y reservar la revelación de la carta del narrador para una acción HTTP aparte.
10. **Añadir la fase persistida.** Extender dominio y contrato con `VoteReveal`, sin renumerar las fases existentes. Cambiar la transición al último voto y conservar la fase en restauraciones de snapshot. Añadir pruebas de dominio y persistencia.
11. **Añadir el comando de revelación del narrador.** Implementar la operación de aplicación y `POST /rooms/{roomCode}/reveal-storyteller-card`; persistir la transición a `Reveal` y publicar `RoomUpdated` solo después de guardar. Añadir pruebas de aplicación, endpoint y SignalR.
12. **Blindar los snapshots públicos.** No serializar votos individuales durante `Voting`; exponerlos solo en `VoteReveal` y fases posteriores. Comprobar que la carta del narrador no se marca antes del comando y que puntuar antes de `Reveal` se rechaza.

### Archivos previstos

**Modificados:**

- `src/ImaginArte.Contracts/Rooms/RoomHttpContracts.cs`
- `src/ImaginArte.Domain/Room.cs`
- `src/ImaginArte.Domain/GamePhase.cs`
- `src/ImaginArte.Server/Program.cs`
- `src/ImaginArte.Server/RoomHub.cs`
- `src/ImaginArte.Server/RoomHttpMappings.cs`
- `tests/ImaginArte.Domain.Tests/RoomTests.cs`
- `tests/ImaginArte.Application.Tests/RoomUseCaseTests.cs`
- `tests/ImaginArte.Server.Tests/RoomHubTests.cs`
- `tests/ImaginArte.Server.Tests/RoomEndpointTests.cs`
- `tests/ImaginArte.Contracts.Tests/RoomRealtimeContractTests.cs`
- `docs/01-game-rules.md`
- `docs/08-screen-specifications.md`
- `src/ImaginArte.Client.React/package.json`
- `src/ImaginArte.Client.React/package-lock.json`
- `src/ImaginArte.Client.React/.env.example`
- `README.md`
- `src/ImaginArte.Client.React/src/App.tsx`
- `src/ImaginArte.Client.React/src/App.css`
- `src/ImaginArte.Client.React/src/services/roomsApi.ts`
- `src/ImaginArte.Client.React/src/screens/tv/TvHomeScreen.tsx`
- `src/ImaginArte.Client.React/src/screens/tv/TvHomeScreen.css`
- `src/ImaginArte.Client.React/src/screens/TvLobbyPlaceholder.tsx`
- `src/ImaginArte.Client.React/src/screens/TvLobbyPlaceholder.css`

**Nuevos:**

- `src/ImaginArte.Server/RoomPresenceTracker.cs`
- `src/ImaginArte.Application/Rooms/SetRoomTargetScore.cs`
- `src/ImaginArte.Application/Rooms/RevealStorytellerCard.cs`
- `src/ImaginArte.Client.React/src/models/roomState.ts`
- `src/ImaginArte.Client.React/src/services/roomRealtime.ts`
- `src/ImaginArte.Client.React/src/services/playerAvatarAssignments.ts`
- `src/ImaginArte.Client.React/src/screens/tv/TvLobbyScreen.tsx`
- `src/ImaginArte.Client.React/src/screens/tv/TvLobbyScreen.css`
- `src/ImaginArte.Client.React/src/screens/tv/TvWaitingForStorytellerScreen.tsx`
- `src/ImaginArte.Client.React/src/screens/tv/TvWaitingForStorytellerScreen.css`
- `src/ImaginArte.Client.React/src/assets/player-avatars-atlas.png`, copiado del atlas mockup.

## Tutoría React

- **Objetivo observable:** la persona verá cómo se llena la sala en la TV, podrá elegir Corta, Media o Larga, escanear el QR con el cliente móvil existente y verá el estado de espera del narrador al iniciar.
- **Conceptos nuevos:** estado React para representar un snapshot; `useEffect` para sincronizar la pantalla con SignalR y limpiar la suscripción; `Promise` para las llamadas HTTP; un `PUT` para persistir una opción de sala; Motion para mover elementos visuales sin modificar el estado del juego.
- **Analogía con Blazor:** `TvLobby.razor` presenta la información pública y `RoomClient.cs` coordina HTTP, SignalR y los cambios de estado. En React, el servicio de sala realiza esa coordinación y el componente vuelve a renderizarse cuando recibe un snapshot nuevo.
- **Límite de la analogía:** React no debe copiar `GameRoom` ni decidir reglas. El servidor sigue siendo la fuente de verdad; React muestra el estado y envía la intención de iniciar.
- **Flujo:** `GET snapshot → estado React → renderizado del lobby`; `selección de modo → PUT → snapshot confirmado y evento SignalR`; al conectarse o reconectarse se solicita estado actual; `clic en Iniciar → POST → snapshot del servidor → espera del narrador`.
- La implementación debe cerrar explicando los archivos y el flujo, la comparación con Blazor, las verificaciones ejecutadas y un breve recapitulativo de aprendizaje.

## Criterios de aceptación

- [x] `/tv/{roomCode}` muestra el lobby real, obtiene sus datos del servidor y no muestra el texto «Sala de juego».
  - Evidencia: `App.tsx:48-72` renderiza `TvLobbyScreen`; `TvLobbyScreen.tsx:149-193` carga el snapshot HTTP y se suscribe a SignalR. La captura accesible `.playwright-mcp/page-yml/page-2026-10-01T19-30-50-686Z.yml` muestra el lobby y no contiene «Sala de juego».
- [ ] **Parcial:** El código de sala es legible a distancia y puede introducirse manualmente.
  - Evidencia: la captura `.playwright-mcp/03-tv-lobby-live-presence/spec03-verify-tv-1920x1080.png` muestra el código en una vista de 1920×1080; `JoinRoom.razor` declara `/join/{RoomCode}`. No se midió la legibilidad física a 2–3 m en una TV ni se confirmó la entrada manual en un teléfono.
- [ ] **Parcial:** Con `VITE_MOBILE_JOIN_BASE_URL` configurada, el QR representa una URL accesible del cliente Blazor con `/join/{roomCode}`.
  - Evidencia: `TvLobbyScreen.tsx:290-298, 471-487` construye el QR desde la base configurada; `src/ImaginArte.Client.React/.env.example:2` y `README.md:175,229` documentan la configuración y `/join/{roomCode}`. El QR aparece en `.playwright-mcp/page-yml/spec03-verify-tv-1920x1080.yml`. Al abrir el cliente local Blazor se observó un 404 del módulo `_framework/dotnet.octvxs4wm8.js` (`.playwright-mcp/console-log/console-2026-10-01T23-00-47-703Z.log`); por tanto no se confirmó que la URL abra correctamente ni se escaneó con un teléfono en la misma red.
- [x] Sin esa configuración, no aparece un QR inválido; el código manual permanece disponible y se comunica el problema con texto accesible.
  - Evidencia: `TvLobbyScreen.tsx:456-496` conserva el código y, si la URL no se puede construir, omite `QRCodeSVG` y presenta el aviso con `role="status"`.
- [x] Al unirse un jugador, su nombre, avatar y el contador se actualizan en la TV sin recargar la página.
  - Evidencia: los snapshots `RoomUpdated` pasan por `applyRoomSnapshot` (`TvLobbyScreen.tsx:132-147`); el endpoint de unión publica la actualización (`Program.cs:112-130`). La revisión Playwright informó roster/contador en vivo; capturas de roster: `.playwright-mcp/03-tv-lobby-live-presence/page-2026-10-01T19-39-11-521Z.png`.
- [x] El progreso muestra los jugadores actuales sobre la capacidad devuelta por el servidor; la capacidad no está duplicada como regla en React.
  - Evidencia: `RoomHttpMappings.cs:98-101` pone los límites autoritativos en el contrato; `TvLobbyScreen.tsx:301, 509-512` renderiza `playerCount / room.maximumPlayers`. `RoomEndpointTests.cs:1072-1083` comprueba los límites públicos.
- [x] Una sala nueva muestra **Media** seleccionado porque su `TargetScore` inicial es `25`.
  - Evidencia: `Room.cs:8` establece 25 y la captura `.playwright-mcp/page-yml/page-2026-10-01T19-30-50-686Z.yml` muestra seleccionado el radio de 25 puntos, con etiqueta **«Media»**. Las opciones se definen en `TvLobbyScreen.tsx:58-61`.
- [x] El lobby permite seleccionar **Corta** (`15`), **Media** (`25`) o **Larga** (`35`). La selección actual se deriva del `TargetScore` confirmado por el servidor.
  - Evidencia: `TvLobbyScreen.tsx:58-61` define las tres etiquetas y puntajes; `:605-610` compara la selección con `room.targetScore` y guarda mediante el endpoint. Las capturas `.playwright-mcp/page-yml/page-2026-10-01T19-37-13-892Z.yml` y `.playwright-mcp/page-yml/page-2026-10-01T19-38-44-866Z.yml` muestran las opciones y los valores seleccionados.
- [x] Seleccionar un modo envía `PUT /rooms/{roomCode}/target-score`; el servidor valida el valor, guarda la sala, devuelve el snapshot actualizado y publica `RoomUpdated`.
  - Evidencia: `roomsApi.ts:87-113`, `Room.SetTargetScore` (`Room.cs:220-237`) y endpoint (`Program.cs:163-183`). `RoomEndpointTests.cs:53-121` cubre valores válidos, inválidos y sala iniciada; `RoomHubTests.cs:401-457` comprueba guardado, snapshot y publicación solo después de guardar.
- [ ] **Parcial:** Tras recargar la TV o reconectar SignalR, el snapshot conserva el modo confirmado. Un fallo al guardar conserva la última selección confirmada, presenta un error accesible y permite reintentar.
  - Evidencia: `RoomTests.cs:72-80` prueba restaurar el puntaje y `roomRealtime.ts:50-76` vuelve a suscribirse y pide un snapshot tras reconectar. En la revisión Playwright, `.playwright-mcp/page-yml/page-2026-10-01T23-03-08-399Z.yml` muestra el error accesible con **Media** aún confirmado; `.playwright-mcp/page-yml/page-2026-10-01T23-03-17-888Z.yml` muestra **Corta** confirmada tras reintentar. `TvLobbyScreen.tsx:237-285, 605-610, 636-655` conserva la respuesta autoritativa y ofrece reintento. No se comprobó manualmente la recarga completa de la página.
- [x] El selector solo está habilitado mientras la sala está en espera de jugadores. El servidor rechaza cambios después de iniciar la partida.
  - Evidencia: el `fieldset` depende de `WaitingForPlayers` (`TvLobbyScreen.tsx:595-598`); `Room.SetTargetScore` aplica la misma regla (`Room.cs:222-227`). Cubierto por `RoomTests.cs:55-69` y `RoomEndpointTests.cs:91-107`.
- [x] Un jugador conectado tiene `IsConnected=true` mientras conserva al menos una conexión SignalR activa. Al cerrarse todas, su nombre permanece y la TV comunica «Reconectando».
  - Evidencia: `RoomPresenceTracker.cs:9-37, 40-85` cuenta conexiones por jugador y sala; `RoomHubTests.cs:98-205` prueba conexión, desconexión, reconexión y conexiones múltiples. La UI no elimina al jugador y muestra ambos estados textualmente (`TvLobbyScreen.tsx:525-557`).
- [x] Al reconectar un jugador, la presencia vuelve a «Conectado»; al reconectar la TV, se solicita un snapshot actualizado.
  - Evidencia: reconexión de jugador cubierta en `RoomHubTests.cs:139-147`; el cliente TV ejecuta `SubscribeToRoom` y `GET` tras `onreconnected` (`roomRealtime.ts:50-76`).
- [x] La primera persona de la lista tiene la etiqueta visual «Anfitrión», que no concede permisos ni cambia que el inicio ocurre desde la TV.
  - Evidencia: `RoomHttpMappings.cs:32-37, 98-102` conserva el orden y deriva `HostPlayerId` de la primera persona; `RoomEndpointTests.cs:1074-1075` lo valida. La etiqueta se renderiza en `TvLobbyScreen.tsx:525-548`; el inicio es un control de TV y no depende de `HostPlayerId` (`TvLobbyScreen.tsx:317-355, 677-684`).
- [x] Cada jugador tiene un avatar del atlas, único en la sala y estable tras actualizaciones SignalR y recarga de la pestaña.
  - Evidencia: el atlas está en `src/ImaginArte.Client.React/src/assets/player-avatars-atlas.png`; `playerAvatarAssignments.ts:3-60, 63-94` asigna índices distintos y aleatorios, y guarda/recupera el mapa en `sessionStorage` con la clave acordada. `TvLobbyScreen.tsx:142-146, 525-539` conserva la asignación al aplicar snapshots.
- [x] Al llegar a 8/8 se comunica «Sala completa»; el botón de inicio sigue disponible si se alcanzó el mínimo.
  - Evidencia: `TvLobbyScreen.tsx:301, 303-306, 515-519` compara contra el máximo y solo condiciona el inicio al mínimo. La vista 8/8 y el inicio habilitado se revisaron en `.playwright-mcp/03-tv-lobby-live-presence/page-2026-10-01T19-40-05-285Z.png`.
- [x] El botón está deshabilitado por debajo del mínimo devuelto por el servidor. Un clic al estar habilitado envía un único `POST /start`; no hay un diálogo adicional.
  - Evidencia: `TvLobbyScreen.tsx:303-315, 317-392, 677-684` usa `minimumPlayers`, evita solicitudes concurrentes y llama `startRoom`; `roomsApi.ts:115-131` usa `POST /rooms/{roomCode}/start`. En la captura `.playwright-mcp/page-yml/page-2026-10-01T19-30-50-686Z.yml` aparece deshabilitado con 0/8 y mínimo 3; en 8/8 se observó habilitado.
- [x] Si `POST /start` responde correctamente, aparece el nombre del narrador y el mensaje de que está eligiendo una carta y una pista.
  - Evidencia: `TvWaitingForStorytellerScreen.tsx:9-33` renderiza nombre y mensaje; la vista observada está registrada en `.playwright-mcp/page-yml/page-2026-10-01T19-40-19-176Z.yml`.
- [x] Un error al cargar, conectar o iniciar se presenta con texto accesible y permite recuperarse sin duplicar el comando.
  - Evidencia: `TvLobbyScreen.tsx:183-190, 429-439, 317-392` usa mensajes de estado/alerta, ofrece reintento y protege el inicio con `startRequestsInFlight`.
- [x] La transición conserva el mismo paisaje y anima solo elementos superpuestos. Con `prefers-reduced-motion`, no se reproduce el desplazamiento.
  - Evidencia: el paisaje está en el backdrop fijo compartido (`App.css:10-27`); `App.tsx:51-91` anima la capa de pantalla con Motion y elimina el desplazamiento al reducir movimiento. Revisado en `.playwright-mcp/03-tv-lobby-live-presence/page-2026-10-01T19-42-17-274Z.png` con movimiento reducido.
- [x] Los estados de conexión incluyen texto y no dependen solo del color; las acciones tienen foco de teclado visible.
  - Evidencia: `TvLobbyScreen.tsx:411-419, 529-557, 792-802` presenta etiquetas textuales; `TvLobbyScreen.css:428-431, 494-497, 618-620` define foco visible. Captura de revisión de foco: `.playwright-mcp/03-tv-lobby-live-presence/page-2026-10-01T19-42-31-184Z.png`.
- [x] Los snapshots públicos no contienen manos de jugadores.
  - Evidencia: `RoomStateResponse` y sus modelos públicos no tienen propiedad `Hand`; esa información está en `PlayerPrivateStateResponse` (`RoomHttpContracts.cs:24-34, 38-65`). `RoomRealtimeContractTests.cs:53-73` comprueba la separación.
- [x] Durante `Voting`, ni HTTP ni `RoomUpdated` incluyen votos individuales, identidades de votantes o su carta elegida; el snapshot conserva solo el progreso agregado.
  - Evidencia: `RoomHttpMappings.cs:38-68` vacía los votos individuales mientras la fase es `Voting`. `RoomEndpointTests.cs:427-470` comprueba respuesta de voto y snapshots HTTP públicos/privados; `RoomHubTests.cs:849-862` comprueba que el `RoomUpdated` durante la votación conserva el progreso agregado y no contiene identidades. La suite Server enfocada pasó 74/74.
- [x] Al recibirse el último voto, el servidor guarda y publica `VoteReveal`; ese snapshot permite ver el mapa votante-carta, pero no identifica la carta del narrador.
  - Evidencia: `Room.cs:424-427` cambia a `VoteReveal`; `CastVote.cs:35-41` guarda antes de devolver el resultado. `RoomTests.cs:830-873` verifica fase, votos y `RevealedVotingCards` vacío; `RoomTests.cs:906-917` verifica restauración del snapshot. `RoomEndpointTests.cs:474-509` y `RoomHubTests.cs:863-881` validan HTTP y `RoomUpdated` después del último voto, con el mapa publicado pero sin revelar la carta del narrador.
- [x] `POST /rooms/{roomCode}/reveal-storyteller-card` solo acepta `VoteReveal`, persiste la transición a `Reveal` y publica el snapshot actualizado después del guardado.
  - Evidencia: `Room.cs:430-440` restringe la transición; `RevealStorytellerCard.cs:18-36` persiste antes de devolver el estado; `Program.cs:284-303` publica `RoomUpdated` después de que termina el handler. `RoomEndpointTests.cs:512-588` cubre fase válida, fase inválida y sala inexistente; `RoomHubTests.cs:460-539` comprueba que no se publique antes de completar el guardado. Suite Server enfocada: 74/74.
- [x] El servidor rechaza el cálculo de puntos antes de `Reveal`, incluida la fase `VoteReveal`.
  - Evidencia: `Room.ScoreRound` solo admite `GamePhase.Reveal` (`Room.cs:442-449`); `RoomTests.cs:1036-1060` y `RoomEndpointTests.cs:768-781` comprueban rechazo sin puntos durante `VoteReveal`. Suite Domain: 95/95; Server: 74/74.
- [x] `npm run typecheck`, `npm run lint` y `npm run build` finalizan correctamente.
  - Evidencia: los tres comandos terminaron con código 0 el 2026-10-02. La advertencia no bloqueante por la ruta con typo (`hola-lateral.png`) se corrigió el 2026-10-03 para usar el recurso existente `hoja-lateral.png`; el build posterior ya no muestra esa advertencia.
- [x] Las pruebas de servidor cubren presencia, desconexión, reconexión y conexiones múltiples; las pruebas de contratos validan los nuevos campos públicos.
  - Evidencia: `RoomHubTests.cs:98-205` cubre los estados de presencia y varias conexiones; `RoomRealtimeContractTests.cs:25-50` comprueba mínimo, máximo, anfitrión, conexión y solicitud. Suites enfocadas ejecutadas el 2026-10-02: Server 74/74; Contracts 8/8.
- [x] Las pruebas de dominio y aplicación validan los puntajes permitidos y que el modo solo se pueda cambiar antes de iniciar; las pruebas de endpoint y SignalR verifican guardado, snapshot y publicación. Se usan los proyectos de prueba existentes, sin crear uno nuevo.
  - Evidencia: `RoomTests.cs:26-80`, `RoomUseCaseTests.cs:136-205`, `RoomEndpointTests.cs:53-121` y `RoomHubTests.cs:401-457`. Suites enfocadas ejecutadas el 2026-10-02: Domain 95/95, Application 56/56, Server 74/74 y Contracts 8/8.
- [ ] **Parcial:** La revisión manual en una vista TV verifica roster, QR, ocupación completa, inicio y movimiento reducido. El QR se prueba con un teléfono conectado a la misma red.
  - Evidencia: la revisión Playwright verificó la vista 1920×1080 y el roster 8/8, además del layout 390×844 y el inicio habilitado: `.playwright-mcp/page-yml/spec03-verify-tv-1920x1080.yml`, `.playwright-mcp/03-tv-lobby-live-presence/spec03-verify-tv-full-1920x1080.png` y `.playwright-mcp/page-yml/spec03-verify-tv-390x844.yml`. La transición con movimiento reducido se revisó en `.playwright-mcp/03-tv-lobby-live-presence/page-2026-10-01T19-42-17-274Z.png`. No se probó en una TV física ni se escaneó el QR con un teléfono en la misma red; la carga del módulo Blazor local también falló con 404 según el log citado arriba.
- [ ] **Parcial:** La revisión manual comprueba los tres modos, el valor Media inicial, la persistencia tras recarga, el bloqueo al iniciar y la recuperación accesible tras un error de guardado.
  - Evidencia: se observaron 15/25/35 y **Media** inicial en las capturas `.playwright-mcp/page-yml/page-2026-10-01T19-37-13-892Z.yml`, `.playwright-mcp/page-yml/page-2026-10-01T19-38-44-866Z.yml` y `.playwright-mcp/page-yml/spec03-verify-tv-1920x1080.yml`. El fallo/reintento accesible se comprobó en `.playwright-mcp/page-yml/page-2026-10-01T23-03-08-399Z.yml` y `.playwright-mcp/page-yml/page-2026-10-01T23-03-17-888Z.yml`. Siguen sin comprobarse manualmente la recarga completa, el bloqueo del selector después de iniciar y la experiencia en una TV física.

### Resultado de verificación — 2026-10-02

**Estado global: En implementación; no cerrar este spec todavía.** Los cuatro criterios server-side de privacidad/revelación ahora están implementados y verificados. Quedan revisiones manuales parciales: lectura física del código a distancia, QR en un teléfono de la misma red, recarga de la página y bloqueo visible del selector tras iniciar.

Comprobaciones ejecutadas el 2026-10-02:

- `dotnet build ImaginArte.sln --no-restore`: correcto, 0 errores; advertencias NU1903 existentes para SSH.NET 2025.1.0.
- Suites enfocadas: Domain 95/95, Application 56/56, Server 74/74 y Contracts 8/8. `npm run typecheck`, `npm run lint` y `npm run build`: correctos en esa ejecución del 2026-10-02; entonces el build todavía mostraba la advertencia de `hola-lateral.png`, corregida después el 2026-10-03.
- `dotnet test ImaginArte.sln --no-restore -m:1`: no completó limpio. 9 pruebas de `Infrastructure.Tests` fallaron porque Docker/Testcontainers no está disponible. En esa ejecución también falló una prueba de SignalR (`PostNextRound_BroadcastsFinalPublicStateAndEachPlayersPrivateState`) por comparar fase esperada 1 con fase recibida 5; la suite Server enfocada, ejecutada por separado, pasó 74/74. El resto de proyectos de prueba del recorrido completo pasó.
- Playwright del entorno: vistas de 1920×1080 y 390×844; roster 8/8, estado completo, inicio habilitado y recuperación accesible del error de guardado. Evidencia en `.playwright-mcp/` (ignorada por Git); no se añadió una suite E2E al repositorio. No se pudo validar el destino Blazor en este entorno: faltó el módulo `_framework/dotnet.octvxs4wm8.js` (404), y no se escaneó el QR con un teléfono ni se midió la legibilidad en una TV física.

## Decisiones

- **Sí:** usar `/join/{roomCode}` del cliente Blazor actual detrás de `VITE_MOBILE_JOIN_BASE_URL`; el flujo móvil React queda para otro spec.
- **Sí:** usar el cliente SignalR de JavaScript y generación QR en React; no añadir un endpoint de imagen QR al servidor.
- **Sí:** registrar presencia solo en memoria y considerar conectado al jugador mientras mantenga al menos una conexión.
- **Sí:** asignar la etiqueta visual de anfitrión a la primera persona; la etiqueta no representa autorización.
- **Sí:** asignar avatares temporales del atlas de forma aleatoria, única y estable en la pestaña. La selección desde el móvil reemplazará esta solución en otro spec.
- **Sí:** interpretar el clic sobre **Iniciar partida**, cuando ya está habilitado, como la confirmación explícita. Esta decisión descarta un segundo diálogo.
- **Sí:** permitir seleccionar Corta (`15`), Media (`25`) o Larga (`35`) en el lobby; usar `TargetScore` existente como única representación autoritativa, sin añadir `GameMode`.
- **Sí:** iniciar las salas nuevas en Media (`TargetScore = 25`) y guardar cada cambio inmediatamente mediante `PUT /rooms/{roomCode}/target-score`.
- **Sí:** permitir cambios solo durante `WaitingForPlayers`; la validación del servidor bloquea cambios una vez iniciada la partida.
- **Sí:** conservar en la interfaz el último valor confirmado y permitir reintentar si falla el guardado.
- **Sí:** mantener fijo el paisaje y animar la interfaz superpuesta con la dependencia Motion existente.
- **Sí:** cerrar la votación antes de publicar identidades; mostrar el mapa votante-carta en `VoteReveal` y revelar la carta del narrador mediante una acción posterior.
- **Sí:** preservar los valores numéricos existentes de las fases y asignar `VoteReveal = 8`.
- **No:** implementar pantallas React o actualizar la TV Blazor para esta transición en este slice; quedan para la futura spec de fases React.
- **No:** implementar ahora el recorrido ni variar su tamaño. La siguiente spec del tablero usará el `TargetScore` elegido para definir el tamaño del recorrido.

## Riesgos

| Riesgo | Mitigación |
| --- | --- |
| El servidor reinicia y pierde el rastreador de presencia en memoria. | Mostrar a las personas como «Reconectando» hasta que sus clientes se suscriban de nuevo; recuperar siempre el estado mediante snapshot. |
| La URL del cliente móvil no es accesible desde el teléfono. | Hacer configurable la base y mantener siempre visible el código manual. |
| La asignación temporal de avatares se reemplaza cuando el móvil incorpore selección real. | Guardar los índices solo en `sessionStorage`; no introducir avatares en el dominio ni en el contrato. |
| Una selección de modo no confirmada podría contradecir el estado persistido de la sala. | Mostrar como seleccionada únicamente la última respuesta confirmada por el servidor y permitir reintentar los errores. |
| El modo de partida todavía no cambia la ruta del tablero en este slice. | Mantener la selección en `TargetScore` y hacer explícito que la siguiente spec adaptará el recorrido a ese valor. |
| El cliente Blazor actual no conoce `VoteReveal` ni su comando de avance. | No actualizarlo en este slice según el alcance aprobado; la futura pantalla de juego React consumirá el nuevo contrato. |

## Qué **no** incluye este spec

- La ruta visual de 25 nodos ni el progreso del tablero.
- La adaptación del tamaño del recorrido al modo seleccionado; corresponde a la siguiente spec del tablero.
- El cliente React móvil, la entrada por código o la selección móvil de avatar.
- Las pantallas completas de fases posteriores a la espera mínima del narrador.
- Permisos de inicio ligados al jugador anfitrión.
- Cambios en el cálculo y reparto de puntuación, reglas de fase, persistencia de presencia o contratos privados. El puntaje objetivo existente sí se configura en este spec.
- Las pantallas y controles React de revelación; esta spec solo prepara el contrato y estado server-side.
- La actualización de Blazor para la nueva secuencia de revelación.
- La eliminación o modificación del cliente Blazor.

## Referencias

- `specs/01-react-client-scaffold.md`
- `specs/02-tv-home-room-creation.md`
- `docs/06-design-system.md`
- `docs/07-progress-board.md`
- `docs/08-screen-specifications.md`
- `docs/09-react-tutoring-guide.md`
- `docs/01-game-rules.md`
- `docs/assets/visual-references/mockups/02-lobby-sala-tv.png`
- `docs/assets/visual-references/mockups/avatares-jugadores-16.png`
- `src/ImaginArte.Client.React/src/screens/tv/TvHomeScreen.tsx`
- `src/ImaginArte.Client.React/src/screens/TvLobbyPlaceholder.tsx`
- `src/ImaginArte.Client.Blazor/Pages/TvLobby.razor`
- `src/ImaginArte.Client.Blazor/Pages/JoinRoom.razor`
- `src/ImaginArte.Client.Blazor/Rooms/TvRoomJoinPresentation.cs`
- `src/ImaginArte.Client.Blazor/Rooms/RoomClient.cs`
- `src/ImaginArte.Server/RoomHub.cs`
- `src/ImaginArte.Server/RoomRealtimePublisher.cs`
- `src/ImaginArte.Contracts/Rooms/RoomHttpContracts.cs`
