# SPEC 05 — Avatares compartidos y persistentes

> **Status:** Implemented
> **Depends on:** SPEC 03 — Lobby TV con presencia en vivo; SPEC 04 — Unión móvil a sala por código
> **Date:** 2026-10-03
> **Objective:** Asignar a cada jugador un avatar único y persistente que la TV renderice desde el estado autoritativo de la sala.

## Scope

**In:**

- Definir los 16 IDs estables `fox`, `owl`, `deer`, `moth`, `turtle`, `octopus`, `mushroom`, `dragon`, `rabbit`, `raven`, `hummingbird`, `koi`, `lynx`, `swan`, `phoenix` y `snail` para los retratos del atlas `src/ImaginArte.Client.React/src/assets/player-avatars-atlas.png`.
- Guardar el `AvatarId` en el jugador de dominio y en el snapshot persistido de la sala.
- Aceptar temporalmente `avatarId` opcional en la unión. Si falta, asignar un ID libre para que el cliente React actual siga funcionando hasta SPEC 06.
- Rechazar IDs desconocidos y avatares ya elegidos por otra persona de la misma sala.
- Incluir `avatarId` en `PlayerStateResponse` y en todos los snapshots públicos HTTP/SignalR.
- Hacer que el lobby React de TV renderice el ID recibido y deje de inventar asignaciones en `sessionStorage`.
- Marcar como obsoletas las salas persistidas anteriores a este cambio. No migrarlas ni inventarles avatares.
- No modificar el cliente Blazor. Tras SPEC 06, sus solicitudes sin avatar dejarán de ser aceptadas.

**Out of scope (for future specs):**

- El selector móvil, la disponibilidad de avatares y el envío de una elección hecha por la persona; se definen en SPEC 06.
- Cambios en puntuación, fases, cartas, identidad de cuenta o catálogo de cartas.
- Actualizar el cliente Blazor. La aceptación final de una unión exigirá un avatar y el cliente Blazor actual no enviará ese campo.
- Añadir o reemplazar imágenes del atlas o renderizar el PNG completo del mockup como interfaz.

## Data model

El identificador estable es semántico y no depende de la posición que ocupe el retrato en el atlas. La correspondencia entre ID y recorte se mantiene en el cliente visual.

| `AvatarId` | Nombre accesible | Índice del atlas |
| --- | --- | ---: |
| `fox` | Zorro | 0 |
| `owl` | Búho | 1 |
| `deer` | Ciervo | 2 |
| `moth` | Polilla | 3 |
| `turtle` | Tortuga | 4 |
| `octopus` | Pulpo | 5 |
| `mushroom` | Seta | 6 |
| `dragon` | Dragón | 7 |
| `rabbit` | Conejo | 8 |
| `raven` | Cuervo | 9 |
| `hummingbird` | Colibrí | 10 |
| `koi` | Pez koi | 11 |
| `lynx` | Lince | 12 |
| `swan` | Cisne | 13 |
| `phoenix` | Fénix | 14 |
| `snail` | Caracol | 15 |

```ts
type AvatarId =
  | 'fox' | 'owl' | 'deer' | 'moth'
  | 'turtle' | 'octopus' | 'mushroom' | 'dragon'
  | 'rabbit' | 'raven' | 'hummingbird' | 'koi'
  | 'lynx' | 'swan' | 'phoenix' | 'snail'

interface PlayerStateResponse {
  playerId: string
  name: string
  avatarId: AvatarId
  isConnected: boolean
}
```

En .NET, `Player` y `PlayerSnapshot` conservan el mismo `AvatarId`. El `JoinRoomRequest.avatarId` es nullable solamente durante esta entrega intermedia; SPEC 06 lo hará obligatorio en el endpoint. El parámetro HTTP conserva un valor predeterminado para que el código Blazor compile sin cambios. Una sala no puede asignar el mismo ID a dos jugadores.

Los nuevos documentos JSON de sala llevan una versión de esquema y el ID de cada jugador. Los documentos anteriores que no tienen avatar se consideran caducados y responden como sala inexistente; no reciben una migración automática.

## Implementation plan

1. Añadir el tipo de dominio `AvatarId`, el conjunto canónico de 16 IDs, la propiedad en `Player`/`PlayerSnapshot` y la serialización versionada. Validar unicidad, asignar un ID libre cuando falte y tratar documentos antiguos como salas caducadas. Cubrir dominio, serialización y persistencia.
2. Pasar `avatarId` por `JoinRoomCommand`, `JoinRoomRequest`, `PlayerStateResponse` y los mapeos HTTP/SignalR. Mantenerlo opcional solo en esta etapa, devolver 400 para un ID inválido y 409 para una elección ocupada, y comprobarlo con pruebas de aplicación, contrato, endpoint y realtime.
3. Crear `src/ImaginArte.Client.React/src/models/playerAvatars.ts` como catálogo único de IDs, nombres accesibles y recortes. Alinear los validadores TypeScript y hacer que `TvLobbyScreen.tsx` renderice `player.avatarId` usando ese catálogo.
4. Retirar `playerAvatarAssignments.ts` y la asignación temporal en `sessionStorage`; comprobar que al reconectar la TV conserva el avatar recibido del servidor.
5. Actualizar «Room Lobby» en `docs/08-screen-specifications.md` para indicar que el avatar público procede del snapshot y dejar el selector móvil diferido a SPEC 06. Ejecutar `dotnet build ImaginArte.sln`, `npm run typecheck`, `npm run lint`, `npm run build` y las suites .NET enfocadas de Domain, Application, Infrastructure, Contracts y Server.

### Archivos previstos

**Modificados:**

- `src/ImaginArte.Domain/Player.cs`
- `src/ImaginArte.Domain/Room.cs`
- `src/ImaginArte.Domain/RoomSnapshot.cs`
- `src/ImaginArte.Domain/DomainErrorCode.cs`
- `src/ImaginArte.Application/Rooms/JoinRoom.cs`
- `src/ImaginArte.Contracts/Rooms/RoomHttpContracts.cs`
- `src/ImaginArte.Infrastructure/Persistence/RoomJsonSerializer.cs`
- `src/ImaginArte.Server/Program.cs`
- `src/ImaginArte.Server/RoomHttpMappings.cs`
- `src/ImaginArte.Client.React/src/models/roomState.ts`
- `src/ImaginArte.Client.React/src/screens/tv/TvLobbyScreen.tsx`
- `docs/08-screen-specifications.md`
- `tests/ImaginArte.Domain.Tests/RoomTests.cs`
- `tests/ImaginArte.Application.Tests/RoomUseCaseTests.cs`
- `tests/ImaginArte.Infrastructure.Tests/SqliteRoomRepositoryTests.cs`
- `tests/ImaginArte.Infrastructure.Tests/PostgresRoomRepositoryTests.cs`
- `tests/ImaginArte.Contracts.Tests/RoomRealtimeContractTests.cs`
- `tests/ImaginArte.Server.Tests/RoomEndpointTests.cs`
- `tests/ImaginArte.Server.Tests/RoomHubTests.cs`

**Nuevo:**

- `src/ImaginArte.Domain/AvatarId.cs`
- `src/ImaginArte.Client.React/src/models/playerAvatars.ts`

**Eliminado:**

- `src/ImaginArte.Client.React/src/services/playerAvatarAssignments.ts`

**Sin cambios:** Blazor, rutas, configuración de clientes, atlas de imágenes, endpoints nuevos y reglas de juego.

## Tutoría React

- **Objetivo observable:** el avatar que la TV muestra para cada jugador procede del snapshot de la sala y no cambia al llegar otro evento.
- **Conceptos:** un ID de dominio frente a su representación visual, acceso a una propiedad del snapshot y una función que convierte IDs estables en recortes del atlas.
- **Analogía con Blazor:** `TvLobby.razor` también renderiza la identidad del jugador; `TvLobbyScreen.tsx` consume `room.players`. La asignación en `playerAvatarAssignments.ts` era estado visual local, no estado de sala.
- **Límite de la analogía:** el atlas solo determina cómo dibujar el ID. No asigna ni valida avatares; esas decisiones pertenecen al dominio y al servidor.
- **Flujo:** unión → `Player.AvatarId` → snapshot persistido → `PlayerStateResponse`/`RoomUpdated` → render del lobby TV. El fallback aleatorio existe solo durante SPEC 05 y desaparece en SPEC 06.
- La explicación de implementación debe nombrar los archivos cambiados, explicar el recorrido del ID desde el servidor hasta el render y cerrar en español con verificación y recapitulación.

## Acceptance Criteria

- [x] El dominio acepta solamente los 16 `AvatarId` definidos en esta spec. — Evidencia: `src/ImaginArte.Domain/AvatarId.cs:5-78` limita los valores al conjunto canónico; `tests/ImaginArte.Domain.Tests/RoomTests.cs:7-26` comprueba los 16 IDs y rechaza desconocidos/mayúsculas (Domain: 101/101).
- [x] Una sala no puede contener dos jugadores con el mismo `AvatarId`; un duplicado solicitado produce HTTP 409. — Evidencia: `src/ImaginArte.Domain/Room.cs:281-286` aplica unicidad; `src/ImaginArte.Server/Program.cs:470-472` mapea el conflicto a 409; `tests/ImaginArte.Server.Tests/RoomEndpointTests.cs:170-190` lo verifica (Server: 76/76).
- [x] Un ID inexistente produce un error de validación HTTP 400. — Evidencia: `src/ImaginArte.Application/Rooms/JoinRoom.cs:36-45` rechaza el ID desconocido y `src/ImaginArte.Server/Program.cs:474-476` devuelve 400; `tests/ImaginArte.Server.Tests/RoomEndpointTests.cs:155-168` lo comprueba (Server: 76/76).
- [x] En esta etapa, una unión sin `avatarId` recibe un ID libre único y sigue siendo funcional mientras se prepara SPEC 06. — Evidencia: `src/ImaginArte.Domain/Room.cs:247-272` asigna el primer ID disponible; `tests/ImaginArte.Domain.Tests/RoomTests.cs:142-155` verifica asignaciones únicas consecutivas y `tests/ImaginArte.Server.Tests/RoomEndpointTests.cs:123-153` prueba una unión HTTP sin el campo (Domain: 101/101; Server: 76/76).
- [x] La respuesta HTTP de unión, los snapshots de sala y los eventos `RoomUpdated` contienen el avatar estable de cada jugador. — Evidencia: `src/ImaginArte.Server/RoomHttpMappings.cs:32-49` mapea `AvatarId` en jugadores y narrador; `src/ImaginArte.Server/RoomRealtimePublisher.cs:13-24` publica `RoomUpdated`; `tests/ImaginArte.Server.Tests/RoomEndpointTests.cs:123-153` verifica respuesta y snapshot HTTP, y `tests/ImaginArte.Server.Tests/RoomHubTests.cs:31-53` verifica el evento público (Server: 76/76).
- [x] El JSON nuevo conserva los IDs al guardar y restaurar una sala. — Evidencia: `src/ImaginArte.Infrastructure/Persistence/RoomJsonSerializer.cs:8-35,38-53,88-105` versiona y serializa/restaura cada ID; `tests/ImaginArte.Infrastructure.Tests/SqliteRoomRepositoryTests.cs:78-91` verifica round-trip con `snail`; suite SQLite enfocada: 13/13.
- [x] Una sala persistida con el esquema anterior se considera inexistente y no recibe IDs inventados. — Evidencia: `src/ImaginArte.Infrastructure/Persistence/RoomJsonSerializer.cs:28-35` devuelve `null` ante versión anterior/ausencia de avatar; `tests/ImaginArte.Infrastructure.Tests/SqliteRoomRepositoryTests.cs:65-76` comprueba la sala legacy como inexistente; suite SQLite enfocada: 13/13.
- [x] La TV obtiene cada retrato de `player.avatarId`; actualizar el roster o reconectar no cambia su identidad visual. — Evidencia: `src/ImaginArte.Client.React/src/screens/tv/TvLobbyScreen.tsx:121-130,498-513` aplica snapshots y mapea cada ID al atlas; `src/ImaginArte.Client.React/src/services/roomRealtime.ts:50-76` se resuscribe y carga snapshot al reconectar. Playwright: la unión de Dora actualizó el roster sin alterar Zorro/Búho/Tortuga; tras reiniciar el servidor la TV volvió a `Conectado en tiempo real`, recibió GET de snapshot HTTP 200 con los mismos IDs y mantuvo los retratos. Screenshots: `.playwright-mcp/spec05-tv-lobby-after-reconnect-1920x1080.png` y `.playwright-mcp/spec05-tv-lobby-after-reconnect-1280x720.png`; ambos viewports sin overflow (documento igual al viewport).
- [x] El cliente React ya no genera asignaciones de avatar ni las guarda en `sessionStorage`. — Evidencia: el servicio `playerAvatarAssignments.ts` está eliminado y no hay coincidencias de `playerAvatarAssignments`/`avatarAssignments` en `src/ImaginArte.Client.React/src`; el uso restante de `sessionStorage` en `TvHomeScreen.tsx:22-30` conserva solo el código de sala, no avatares.
- [x] El código del cliente Blazor permanece intacto. — Evidencia: `git diff --quiet main...HEAD -- src/ImaginArte.Client.Blazor` termina con código 0 y no lista cambios.
- [x] Blazor compila sin cambios mediante el parámetro HTTP nullable opcional mientras el fallback temporal sigue activo. — Evidencia: `JoinRoomRequest.AvatarId` es `string?` opcional en `src/ImaginArte.Contracts/Rooms/RoomHttpContracts.cs:7`; fallback en `Room.cs:247-272`; `dotnet build ImaginArte.sln` compiló el proyecto Blazor con 0 errores.
- [x] `dotnet build ImaginArte.sln`, `npm run typecheck`, `npm run lint`, `npm run build` y las suites .NET enfocadas indicadas terminan correctamente. — Evidencia: build .NET (0 errores); `npm run typecheck`, `npm run lint` y `npm run build`; Domain (101/101), Application (59/59), Infrastructure (24/24), Contracts (8/8) y Server (76/76), todas correctas. La suite Infrastructure completa se repitió con `dotnet test tests/ImaginArte.Infrastructure.Tests/ImaginArte.Infrastructure.Tests.csproj --no-restore -m:1` y pasó (24/24), incluidas las pruebas PostgreSQL. Se mantienen advertencias NU1903 existentes sobre SSH.NET 2025.1.0; no afectan el resultado de las pruebas.

## Decisions

- **Sí:** IDs estables con nombres semánticos, en vez de guardar índices del atlas; un cambio de orden visual no cambia la identidad persistida.
- **Sí:** unicidad por sala; el máximo actual de ocho jugadores cabe dentro de los 16 avatares.
- **Sí:** publicar el avatar junto al nombre en el snapshot público para que TV y móvil consulten la misma identidad.
- **Sí:** conservar un fallback de asignación libre solo durante SPEC 05 para no dejar inoperante el formulario React antes de SPEC 06.
- **Sí:** invalidar salas persistidas anteriores en vez de migrar o asignar avatares sin una elección explícita.
- **No:** modificar Blazor. Al finalizar SPEC 06, las solicitudes que no incluyan avatar dejarán de ser aceptadas.
- **No:** mantener el mapa de `sessionStorage` del lobby TV como segunda fuente de identidad.

## Risks

| Risk | Mitigation |
| --- | --- |
| Un snapshot antiguo no contiene `AvatarId`. | Versionar el documento y responder como sala caducada; probar el caso de lectura sin versión. |
| La nueva validación de unicidad recibe elecciones simultáneas. | Validar y guardar dentro del flujo autoritativo de la sala; responder 409 para que el cliente pueda refrescar disponibilidad. |
| Cambia el orden del atlas. | Mantener una correspondencia explícita `AvatarId` → recorte; no persistir posiciones de píxel como identidad. |
| El fallback temporal podría permanecer por accidente. | SPEC 06 incluye como criterio quitar la asignación sin avatar y probar que el endpoint la rechaza. |
| El cliente Blazor actual no envía `avatarId`. | No tocarlo según el alcance; aceptar que dejará de poder unirse después de SPEC 06 y registrar esa incompatibilidad en la documentación del cambio. |

## Referencias

- `specs/03-tv-lobby-live-presence.md`
- `specs/04-mobile-join-by-code.md`
- `docs/06-design-system.md`
- `docs/08-screen-specifications.md`
- `docs/09-react-tutoring-guide.md`
- `docs/assets/visual-references/mockups/09-unirse-sala-movil-avatar-selector-sin-qr.png`
- `src/ImaginArte.Client.React/src/assets/player-avatars-atlas.png`
- `src/ImaginArte.Client.React/src/screens/tv/TvLobbyScreen.tsx`
- `src/ImaginArte.Client.React/src/services/playerAvatarAssignments.ts`
- `src/ImaginArte.Contracts/Rooms/RoomHttpContracts.cs`
- `src/ImaginArte.Domain/Player.cs`
- `src/ImaginArte.Infrastructure/Persistence/RoomJsonSerializer.cs`

## What is **not** in this spec

- El selector React móvil y la persistencia local de su elección; corresponden a SPEC 06.
- Un cambio en el cliente Blazor para añadir selector o enviar un avatar predeterminado.
- Una migración automática de snapshots antiguos.
- Un cambio de reglas, puntuación, cartas, fases o privacidad de sala.
