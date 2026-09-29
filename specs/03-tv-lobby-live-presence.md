# SPEC 03 — Lobby TV con presencia en vivo

> **Estado:** Approved  
> **Depende de:** SPEC 02 — Inicio TV: creación y reanudación de sala  
> **Fecha:** 2026-09-28  
> **Objetivo:** Implementar el lobby de TV en React con QR móvil, roster en tiempo real, selección del puntaje objetivo y transición animada desde `/tv`, manteniendo fijo el paisaje.

## Scope

**In:**

- Sustituir el placeholder de `/tv/{roomCode}` por el lobby de TV.
- Mostrar el código de sala como alternativa manual al QR, los jugadores unidos, el progreso de ocupación y la puntuación objetivo.
- Permitir elegir en el lobby el modo **Corta** (15 puntos), **Estándar** (25 puntos) o **Larga** (35 puntos). Las salas nuevas empiezan en **Estándar**.
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

**Fuera de alcance (para futuros specs):**

- La ruta visual de 25 nodos del tablero. Se aplaza explícitamente en este slice aunque `docs/08-screen-specifications.md` la contempla como contexto del lobby.
- Una pantalla de unión móvil React. El QR apunta al cliente Blazor existente.
- La selección y persistencia de avatares desde el móvil.
- La pantalla completa de espera al narrador y las fases posteriores; aquí solo se muestra el estado mínimo de espera acordado.
- Autorización real del jugador anfitrión. La etiqueta visual no cambia permisos ni traslada el control de inicio desde la TV.
- Cambios en el cálculo o reparto de puntos, la rotación del narrador u otras reglas y fases. Este slice solo permite configurar el puntaje objetivo existente.
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
- `TargetScore` sigue siendo la fuente de verdad para el modo: `15` corresponde a Corta, `25` a Estándar y `35` a Larga. La sala nueva conserva el valor predeterminado actual de `25`.
- El servidor solo acepta `15`, `25` o `35` al actualizar el objetivo y solo mientras la sala esté en `WaitingForPlayers`. El valor seleccionado se guarda con el estado existente de la sala y sobrevive a la recuperación del snapshot.
- No se añade una propiedad `GameMode`: la pantalla deriva la etiqueta del `TargetScore` recibido y el futuro spec del recorrido podrá usar ese mismo valor para determinar su tamaño.
- React asigna índices del atlas, del `0` al `15`, sin repetirlos en la sala. Guarda el mapa temporal en:

```text
sessionStorage["imaginarte.tv.roomAvatars.{roomCode}"] = { "{playerId}": 0..15 }
```

La asignación de avatar es estado visual temporal, no un dato de jugador ni una fuente de verdad del juego.

## Plan de implementación

1. **Extender los contratos públicos.** Añadir mínimo, máximo, anfitrión y estado de conexión al snapshot, además de la solicitud `UpdateRoomTargetScoreRequest`. Actualizar las pruebas de contrato y del endpoint.
2. **Hacer configurable el puntaje objetivo en el servidor.** Añadir una operación de dominio y aplicación que acepte solo `15`, `25` o `35` mientras la sala esté en `WaitingForPlayers`; persistir la sala y exponer `PUT /rooms/{roomCode}/target-score`. El endpoint devuelve el snapshot actualizado y publica `RoomUpdated` al grupo público solo tras guardar correctamente. Añadir pruebas de dominio, aplicación, endpoint y publicación realtime.
3. **Añadir presencia efímera al servidor.** Crear un rastreador por sala y jugador que gestione varias conexiones SignalR. Registrar al jugador al ejecutar `SubscribeAsPlayer`, retirar la conexión en `OnDisconnectedAsync` y publicar el estado actualizado al grupo público. Añadir pruebas para conexión, desconexión, reconexión y múltiples conexiones de una misma persona.
4. **Conectar el cliente React con la sala.** Añadir los modelos TypeScript, la consulta `GET /rooms/{roomCode}`, la actualización del puntaje objetivo y el adaptador SignalR usando `@microsoft/signalr`. Conectar la ruta existente a los estados de carga/error/snapshot y volver a solicitar un snapshot al reconectar.
5. **Reemplazar el placeholder por el lobby.** Mostrar código, roster, presencia, progreso, puntaje objetivo, anfitrión y avatares del atlas. Añadir el selector accesible de modo y reflejar solo valores confirmados por el servidor. Generar el QR con la base configurada y conservar la alternativa manual cuando falte.
6. **Conectar el inicio y el estado de espera.** Deshabilitar el botón según `MinimumPlayers`, evitar envíos duplicados y enviar `POST /start` al activarlo. Tras el éxito, bloquear el selector de modo y mostrar el narrador y la instrucción de espera usando el snapshot del servidor.
7. **Implementar la transición visual.** Mantener el fondo en un contenedor compartido entre `/tv` y `/tv/{roomCode}`. Animar el título, la tarjeta/semilla y los paneles de interfaz con Motion, que ya está instalado. Con movimiento reducido, pasar directamente al estado final o usar un fundido breve.
8. **Documentar la configuración.** Añadir `VITE_MOBILE_JOIN_BASE_URL` a `.env.example` y explicar en `README.md` que, para escanear desde un teléfono en la red local, la URL debe usar una dirección accesible desde ese dispositivo.

### Archivos previstos

**Modificados:**

- `src/ImaginArte.Contracts/Rooms/RoomHttpContracts.cs`
- `src/ImaginArte.Domain/Room.cs`
- `src/ImaginArte.Server/Program.cs`
- `src/ImaginArte.Server/RoomHub.cs`
- `src/ImaginArte.Server/RoomHttpMappings.cs`
- `tests/ImaginArte.Domain.Tests/RoomTests.cs`
- `tests/ImaginArte.Application.Tests/RoomUseCaseTests.cs`
- `tests/ImaginArte.Server.Tests/RoomHubTests.cs`
- `tests/ImaginArte.Server.Tests/RoomEndpointTests.cs`
- `tests/ImaginArte.Contracts.Tests/RoomRealtimeContractTests.cs`
- `src/ImaginArte.Client.React/package.json`
- `src/ImaginArte.Client.React/package-lock.json`
- `src/ImaginArte.Client.React/.env.example`
- `README.md`
- `src/ImaginArte.Client.React/src/App.tsx`
- `src/ImaginArte.Client.React/src/App.css`
- `src/ImaginArte.Client.React/src/services/roomsApi.ts`
- `src/ImaginArte.Client.React/src/screens/TvHomeScreen.tsx`
- `src/ImaginArte.Client.React/src/screens/TvHomeScreen.css`
- `src/ImaginArte.Client.React/src/screens/TvLobbyPlaceholder.tsx`
- `src/ImaginArte.Client.React/src/screens/TvLobbyPlaceholder.css`

**Nuevos:**

- `src/ImaginArte.Server/RoomPresenceTracker.cs`
- `src/ImaginArte.Application/Rooms/SetRoomTargetScore.cs`
- `src/ImaginArte.Client.React/src/models/roomState.ts`
- `src/ImaginArte.Client.React/src/services/roomRealtime.ts`
- `src/ImaginArte.Client.React/src/services/playerAvatarAssignments.ts`
- `src/ImaginArte.Client.React/src/screens/TvLobbyScreen.tsx`
- `src/ImaginArte.Client.React/src/screens/TvLobbyScreen.css`
- `src/ImaginArte.Client.React/src/screens/TvWaitingForStorytellerScreen.tsx`
- `src/ImaginArte.Client.React/src/screens/TvWaitingForStorytellerScreen.css`
- `src/ImaginArte.Client.React/src/assets/player-avatars-atlas.png`, copiado del atlas mockup.

## Tutoría React

- **Objetivo observable:** la persona verá cómo se llena la sala en la TV, podrá elegir Corta, Estándar o Larga, escanear el QR con el cliente móvil existente y verá el estado de espera del narrador al iniciar.
- **Conceptos nuevos:** estado React para representar un snapshot; `useEffect` para sincronizar la pantalla con SignalR y limpiar la suscripción; `Promise` para las llamadas HTTP; un `PUT` para persistir una opción de sala; Motion para mover elementos visuales sin modificar el estado del juego.
- **Analogía con Blazor:** `TvLobby.razor` presenta la información pública y `RoomClient.cs` coordina HTTP, SignalR y los cambios de estado. En React, el servicio de sala realiza esa coordinación y el componente vuelve a renderizarse cuando recibe un snapshot nuevo.
- **Límite de la analogía:** React no debe copiar `GameRoom` ni decidir reglas. El servidor sigue siendo la fuente de verdad; React muestra el estado y envía la intención de iniciar.
- **Flujo:** `GET snapshot → estado React → renderizado del lobby`; `selección de modo → PUT → snapshot confirmado y evento SignalR`; al conectarse o reconectarse se solicita estado actual; `clic en Iniciar → POST → snapshot del servidor → espera del narrador`.
- La implementación debe cerrar explicando los archivos y el flujo, la comparación con Blazor, las verificaciones ejecutadas y un breve recapitulativo de aprendizaje.

## Criterios de aceptación

- [ ] `/tv/{roomCode}` muestra el lobby real, obtiene sus datos del servidor y no muestra el texto «Sala de juego».
- [ ] El código de sala es legible a distancia y puede introducirse manualmente.
- [ ] Con `VITE_MOBILE_JOIN_BASE_URL` configurada, el QR representa una URL accesible del cliente Blazor con `/join/{roomCode}`.
- [ ] Sin esa configuración, no aparece un QR inválido; el código manual permanece disponible y se comunica el problema con texto accesible.
- [ ] Al unirse un jugador, su nombre, avatar y el contador se actualizan en la TV sin recargar la página.
- [ ] El progreso muestra los jugadores actuales sobre la capacidad devuelta por el servidor; la capacidad no está duplicada como regla en React.
- [ ] Una sala nueva muestra **Estándar** seleccionado porque su `TargetScore` inicial es `25`.
- [ ] El lobby permite seleccionar **Corta** (`15`), **Estándar** (`25`) o **Larga** (`35`). La selección actual se deriva del `TargetScore` confirmado por el servidor.
- [ ] Seleccionar un modo envía `PUT /rooms/{roomCode}/target-score`; el servidor valida el valor, guarda la sala, devuelve el snapshot actualizado y publica `RoomUpdated`.
- [ ] Tras recargar la TV o reconectar SignalR, el snapshot conserva el modo confirmado. Un fallo al guardar conserva la última selección confirmada, presenta un error accesible y permite reintentar.
- [ ] El selector solo está habilitado mientras la sala está en espera de jugadores. El servidor rechaza cambios después de iniciar la partida.
- [ ] Un jugador conectado tiene `IsConnected=true` mientras conserva al menos una conexión SignalR activa. Al cerrarse todas, su nombre permanece y la TV comunica «Reconectando».
- [ ] Al reconectar un jugador, la presencia vuelve a «Conectado»; al reconectar la TV, se solicita un snapshot actualizado.
- [ ] La primera persona de la lista tiene la etiqueta visual «Anfitrión», que no concede permisos ni cambia que el inicio ocurre desde la TV.
- [ ] Cada jugador tiene un avatar del atlas, único en la sala y estable tras actualizaciones SignalR y recarga de la pestaña.
- [ ] Al llegar a 8/8 se comunica «Sala completa»; el botón de inicio sigue disponible si se alcanzó el mínimo.
- [ ] El botón está deshabilitado por debajo del mínimo devuelto por el servidor. Un clic al estar habilitado envía un único `POST /start`; no hay un diálogo adicional.
- [ ] Si `POST /start` responde correctamente, aparece el nombre del narrador y el mensaje de que está eligiendo una carta y una pista.
- [ ] Un error al cargar, conectar o iniciar se presenta con texto accesible y permite recuperarse sin duplicar el comando.
- [ ] La transición conserva el mismo paisaje y anima solo elementos superpuestos. Con `prefers-reduced-motion`, no se reproduce el desplazamiento.
- [ ] Los estados de conexión incluyen texto y no dependen solo del color; las acciones tienen foco de teclado visible.
- [ ] Las actualizaciones públicas no muestran manos ni otros datos privados de jugadores.
- [ ] `npm run typecheck`, `npm run lint` y `npm run build` finalizan correctamente.
- [ ] Las pruebas de servidor cubren presencia, desconexión, reconexión y conexiones múltiples; las pruebas de contratos validan los nuevos campos públicos.
- [ ] Las pruebas de dominio y aplicación validan los puntajes permitidos y que el modo solo se pueda cambiar antes de iniciar; las pruebas de endpoint y SignalR verifican guardado, snapshot y publicación. Se usan los proyectos de prueba existentes, sin crear uno nuevo.
- [ ] La revisión manual en una vista TV verifica roster, QR, ocupación completa, inicio y movimiento reducido. El QR se prueba con un teléfono conectado a la misma red.
- [ ] La revisión manual comprueba los tres modos, el valor estándar inicial, la persistencia tras recarga, el bloqueo al iniciar y la recuperación accesible tras un error de guardado.

## Decisiones

- **Sí:** usar `/join/{roomCode}` del cliente Blazor actual detrás de `VITE_MOBILE_JOIN_BASE_URL`; el flujo móvil React queda para otro spec.
- **Sí:** usar el cliente SignalR de JavaScript y generación QR en React; no añadir un endpoint de imagen QR al servidor.
- **Sí:** registrar presencia solo en memoria y considerar conectado al jugador mientras mantenga al menos una conexión.
- **Sí:** asignar la etiqueta visual de anfitrión a la primera persona; la etiqueta no representa autorización.
- **Sí:** asignar avatares temporales del atlas de forma aleatoria, única y estable en la pestaña. La selección desde el móvil reemplazará esta solución en otro spec.
- **Sí:** interpretar el clic sobre **Iniciar partida**, cuando ya está habilitado, como la confirmación explícita. Esta decisión descarta un segundo diálogo.
- **Sí:** permitir seleccionar Corta (`15`), Estándar (`25`) o Larga (`35`) en el lobby; usar `TargetScore` existente como única representación autoritativa, sin añadir `GameMode`.
- **Sí:** iniciar las salas nuevas en Estándar (`TargetScore = 25`) y guardar cada cambio inmediatamente mediante `PUT /rooms/{roomCode}/target-score`.
- **Sí:** permitir cambios solo durante `WaitingForPlayers`; la validación del servidor bloquea cambios una vez iniciada la partida.
- **Sí:** conservar en la interfaz el último valor confirmado y permitir reintentar si falla el guardado.
- **Sí:** mantener fijo el paisaje y animar la interfaz superpuesta con la dependencia Motion existente.
- **No:** implementar ahora el recorrido ni variar su tamaño. La siguiente spec del tablero usará el `TargetScore` elegido para definir el tamaño del recorrido.

## Riesgos

| Riesgo | Mitigación |
| --- | --- |
| El servidor reinicia y pierde el rastreador de presencia en memoria. | Mostrar a las personas como «Reconectando» hasta que sus clientes se suscriban de nuevo; recuperar siempre el estado mediante snapshot. |
| La URL del cliente móvil no es accesible desde el teléfono. | Hacer configurable la base y mantener siempre visible el código manual. |
| La asignación temporal de avatares se reemplaza cuando el móvil incorpore selección real. | Guardar los índices solo en `sessionStorage`; no introducir avatares en el dominio ni en el contrato. |
| Una selección de modo no confirmada podría contradecir el estado persistido de la sala. | Mostrar como seleccionada únicamente la última respuesta confirmada por el servidor y permitir reintentar los errores. |
| El modo de partida todavía no cambia la ruta del tablero en este slice. | Mantener la selección en `TargetScore` y hacer explícito que la siguiente spec adaptará el recorrido a ese valor. |

## Qué **no** incluye este spec

- La ruta visual de 25 nodos ni el progreso del tablero.
- La adaptación del tamaño del recorrido al modo seleccionado; corresponde a la siguiente spec del tablero.
- El cliente React móvil, la entrada por código o la selección móvil de avatar.
- Las pantallas completas de fases posteriores a la espera mínima del narrador.
- Permisos de inicio ligados al jugador anfitrión.
- Cambios en el cálculo y reparto de puntuación, reglas de fase, persistencia de presencia o contratos privados. El puntaje objetivo existente sí se configura en este spec.
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
- `src/ImaginArte.Client.React/src/screens/TvHomeScreen.tsx`
- `src/ImaginArte.Client.React/src/screens/TvLobbyPlaceholder.tsx`
- `src/ImaginArte.Client.Blazor/Pages/TvLobby.razor`
- `src/ImaginArte.Client.Blazor/Pages/JoinRoom.razor`
- `src/ImaginArte.Client.Blazor/Rooms/TvRoomJoinPresentation.cs`
- `src/ImaginArte.Client.Blazor/Rooms/RoomClient.cs`
- `src/ImaginArte.Server/RoomHub.cs`
- `src/ImaginArte.Server/RoomRealtimePublisher.cs`
- `src/ImaginArte.Contracts/Rooms/RoomHttpContracts.cs`
