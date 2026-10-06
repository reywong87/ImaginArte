# SPEC 06 — Selector de avatar en la unión móvil

> **Status:** Implemented
> **Depends on:** SPEC 04 — Unión móvil a sala por código; SPEC 05 — Avatares compartidos y persistentes
> **Date:** 2026-10-03
> **Objective:** Permitir que cada persona elija uno de los 16 avatares libres antes de unirse y confirmar después la identidad visual que quedó guardada.

## Scope

**In:**

- Añadir a `MobileJoinScreen.tsx` la sección **Elige tu avatar**, siguiendo la jerarquía del mockup 09 y usando el atlas existente, no el PNG completo como pantalla.
- Presentar los 16 IDs estables definidos en SPEC 05 con nombres accesibles en español. Mostrar una vista previa grande de la elección y una cuadrícula responsiva de opciones.
- No preseleccionar ningún avatar. La persona debe elegir explícitamente antes de poder unirse.
- Consultar el snapshot público de la sala para marcar como no disponibles los avatares que ya eligieron otras personas. No mostrar en esta pantalla sus nombres ni roster.
- Enviar el ID elegido junto al código, `playerId` y nombre en la unión existente.
- Resolver un 409 por elección ocupada refrescando la disponibilidad, conservando código/nombre y pidiendo elegir otra opción.
- Guardar el `avatarId` antes de enviar la unión y reutilizar la misma elección y el mismo `playerId` al resolver un resultado HTTP incierto.
- Cambiar la clave local de sesión a `imaginarte.mobile.v2.room.{ROOM_CODE}.player`. Borrar las sesiones locales v1 sin intentar recuperarlas.
- En la confirmación, mostrar el avatar unido junto al nombre y el código. No ofrecer edición después de unirse.
- Hacer obligatorio `avatarId` en el `POST` al completar esta spec. El servidor rechaza solicitudes sin avatar y el fallback temporal de SPEC 05 se elimina.
- Actualizar la especificación de pantalla móvil y mantener las etiquetas, foco visible, anuncios de estado y objetivos táctiles de al menos 44 × 44 px.

**Out of scope (for future specs):**

- Cambiar el avatar después de unirse o durante la partida.
- Modificar el cliente Blazor para que envíe avatar. Al acabar esta spec, dicho cliente no podrá unirse con su solicitud actual.
- Añadir cuentas, perfiles persistentes entre salas, nuevos endpoints, una nueva librería o nuevos archivos de imagen.
- Mostrar en móvil el roster, el tablero, las manos o avatares elegidos por otros jugadores; solo se usa su ID para bloquear opciones.
- Añadir al repositorio una configuración o dependencia nueva de Playwright.

## Data model

La sesión local pasa a conservar el avatar elegido para reintentos de unión. La clave v2 evita interpretar un registro antiguo como si ya tuviera una elección confirmada.

```ts
interface StoredMobilePlayerSession {
  roomCode: string
  playerId: string
  playerName: string
  avatarId: AvatarId
  status: 'joining' | 'joined'
}
```

`AvatarId` y el catálogo de 16 valores se reutilizan desde SPEC 05. El JSON de unión debe incluir `avatarId`; si falta, el endpoint responde 400. El DTO C# conserva un parámetro nullable con valor predeterminado para que el cliente Blazor compile sin cambios, pero esa solicitud no será aceptada por el servidor. La selección es estado local reversible antes del envío; después de la confirmación, el avatar del snapshot del servidor es el valor autoritativo.

La clave queda definida como:

```text
localStorage["imaginarte.mobile.v2.room.{ROOM_CODE}.player"]
```

## Implementation plan

1. Integrar en `MobileJoinScreen.tsx` la consulta de `getRoomState` al disponer de un código válido. Al cambiar el código, invalidar el snapshot anterior y representar carga/error sin mostrar el roster.
2. Añadir el selector accesible y la selección requerida. Actualizar `StoredMobilePlayerSession` a v2, borrar una clave v1 encontrada y enviar el ID seleccionado con el `POST` mientras SPEC 05 aún proporciona su fallback temporal.
3. Resolver los conflictos 409 refrescando disponibilidad y conservando código/nombre. Persistir y reutilizar el mismo `playerId` y `avatarId` al recuperar un POST incierto; mostrar el avatar confirmado en la vista de espera.
4. Validar `avatarId` como obligatorio en el endpoint, pasar un valor no nullable a `JoinRoomCommand`/`Room.Join`, eliminar el fallback de SPEC 05 y probar el rechazo de solicitudes sin avatar. Mantener el parámetro nullable opcional del DTO HTTP para que Blazor compile sin cambios; no modificar su cliente.
5. Actualizar la sección «Join By Code Or QR» de `docs/08-screen-specifications.md` con las reglas de selección, unicidad y confirmación. Ejecutar `npm run typecheck`, `npm run lint`, `npm run build`, `dotnet build ImaginArte.sln`, las pruebas Server/Domain/Contracts enfocadas y la verificación de navegador a 390 × 844 px con las herramientas disponibles; no instalar Playwright en el repositorio.

### Archivos previstos

**Modificados:**

- `src/ImaginArte.Client.React/src/screens/mobile/MobileJoinScreen.tsx`
- `src/ImaginArte.Client.React/src/screens/mobile/MobileJoinScreen.css`
- `src/ImaginArte.Client.React/src/services/mobilePlayerSession.ts`
- `src/ImaginArte.Client.React/src/services/roomsApi.ts`
- `src/ImaginArte.Contracts/Rooms/RoomHttpContracts.cs`
- `src/ImaginArte.Server/Program.cs`
- `src/ImaginArte.Application/Rooms/JoinRoom.cs`
- `src/ImaginArte.Domain/Room.cs`
- `docs/08-screen-specifications.md`
- `tests/ImaginArte.Domain.Tests/RoomTests.cs`
- `tests/ImaginArte.Application.Tests/RoomUseCaseTests.cs`
- `tests/ImaginArte.Contracts.Tests/RoomRealtimeContractTests.cs`
- `tests/ImaginArte.Server.Tests/RoomEndpointTests.cs`
- `tests/ImaginArte.Server.Tests/RoomHubTests.cs`

**Sin cambios:** Blazor, rutas, entidades de cartas, almacenamiento de imágenes, endpoints nuevos, configuración de E2E y dependencias del cliente.

## Tutoría React

- **Objetivo observable:** antes de unirse, la persona elige un avatar disponible; después del envío ve confirmados su nombre, sala y avatar.
- **Conceptos:** estado local de selección, renderizado de una colección con `map` y `key`, botones de opción accesibles, carga HTTP de disponibilidad y persistencia local de una elección pendiente.
- **Analogía con Blazor:** los campos existentes en `MobileJoinScreen.tsx` ya usan estado controlado, similar a `@bind-Value` en `JoinRoom.razor`. La cuadrícula equivale a renderizar una colección con `@foreach`, pero React requiere una `key` estable y el estado se cambia creando el nuevo valor.
- **Límite de la analogía:** la disponibilidad proviene del snapshot del servidor; la selección local solo es un borrador. La interfaz no reserva ni valida por sí misma la identidad.
- **Flujo:** código → `getRoomState` → IDs ocupados → estado local de selección → `POST` con el mismo `avatarId` guardado → snapshot confirmado → espera con avatar visible. La recuperación consulta primero el estado autoritativo.
- La explicación debe recorrer el primer render, la carga al validar/cambiar código, la elección, el envío, el conflicto 409 y la limpieza de recursos. Debe cerrar en español con recapitulación, glosario breve y verificación reproducible.

## Acceptance Criteria

- [x] El formulario muestra **Elige tu avatar** y los 16 retratos del atlas en una disposición móvil legible, con vista previa grande de la selección. — Evidencia: `src/ImaginArte.Client.React/src/screens/mobile/MobileJoinScreen.tsx:823-929` renderiza la vista previa y 16 opciones; screenshot móvil `.playwright-mcp/spec06-avatar-390x844-bottom.png`. Legibilidad móvil comprobada manualmente por la persona usuaria.
- [x] Ninguna opción aparece seleccionada inicialmente y **Unirme a la sala** no envía una unión hasta que se seleccione una opción disponible. — Evidencia: `MobileJoinScreen.tsx:231,260-262,525-543,982-997`; Playwright 390 × 844: cero radios seleccionados, botón deshabilitado y ninguna solicitud `POST` antes de elegir; tras seleccionar **Zorro**, el botón se habilita.
- [x] Cada opción tiene nombre accesible en español, estado elegido/no disponible comprensible sin depender del color y foco de teclado visible. — Evidencia: el snapshot accesible anuncia radios como `Zorro Disponible` y `Búho No disponible`; teclado Tab enfoca **Zorro** con contorno dorado de 3 px (`MobileJoinScreen.css:275-279`), screenshot `.playwright-mcp/spec06-avatar-390x844-keyboard-focus.png`. Comprensión visual de los estados a tamaño móvil comprobada manualmente por la persona usuaria.
- [x] Cada objetivo táctil de avatar mide al menos 44 × 44 px; a 390 × 844 px el contenido puede desplazarse y ningún control queda cortado por el borde o el área segura inferior. — Evidencia: Playwright a 390 × 844 midió objetivos de 75,6 × 90,8 px como mínimo; región desplazable de 788 px con 1225 px de contenido; al final del desplazamiento el botón queda en y=677–733 y el mensaje de privacidad en y=765–812, sin controles cortados (`.playwright-mcp/spec06-avatar-390x844-bottom.png`). `MobileJoinScreen.css:52-68,251-255,257-273` define scroll, área segura y grid. El navegador no emuló valores físicos de safe-area; sí se observó el margen CSS de respaldo de 32 px.
- [x] El snapshot de disponibilidad bloquea los `avatarId` usados por otras personas sin mostrar el roster ni sus nombres. — Evidencia: `MobileJoinScreen.tsx:257-259,876-927` usa únicamente IDs para disponibilidad; en Playwright, con `AVT246` y `BCE357` precargadas con jugadores identificados, los avatares ocupados se deshabilitaron y el árbol accesible/DOM no mostró ninguno de sus nombres ni roster.
- [x] Cambiar el código invalida la selección y disponibilidad del código anterior; el snapshot nuevo determina qué avatares pueden elegirse. — Evidencia: `MobileJoinScreen.tsx:759-767,248-255,271-314`; Playwright cambió `AVT246` → `BCE357` y de vuelta: se limpió la selección y el nuevo snapshot deshabilitó **Zorro** ocupado en `BCE357`.
- [x] Un `POST` válido incluye exactamente el `avatarId` elegido junto al `playerId` estable y nombre recortado. — Evidencia: `roomsApi.ts:90-108` envía `{ playerId, playerName, avatarId }`; Playwright capturó el `POST` 200 con cuerpo exacto `{"playerId":"4a989ce8-e334-4190-85aa-0d9e3d2b6a95","playerName":"Marta","avatarId":"deer"}` para `AVT246`; la sesión v2 guardó el mismo ID y avatar.
- [x] Un avatar ocupado por una unión simultánea produce un mensaje accesible, refresca disponibilidad y permite elegir otro sin perder el código ni el nombre. — Evidencia: `MobileJoinScreen.tsx:622-632` limpia la sesión pendiente y vuelve a consultar; Playwright forzó una carrera real: el primer `POST` recibió 409, anunció el conflicto con `role="alert"`, conservó `AVT246`/`Marta`, deshabilitó **Zorro** y permitió elegir **Ciervo**, cuyo `POST` respondió 200.
- [x] Una respuesta incierta se resuelve primero consultando la identidad guardada; no crea otro jugador ni cambia el avatar enviado originalmente. — Evidencia: `MobileJoinScreen.tsx:554-595,599-611`; Playwright simuló pérdida de respuesta después de que el servidor confirmara el `POST` 200: la sesión `joining` ya existía antes del envío, y al reintentar se hizo `GET /rooms/AVT246/players/{playerId}` 200, sin un segundo `POST`; se confirmó el mismo `playerId` y `moth`.
- [x] La pantalla guarda avatar e identidad en la sesión v2 antes del `POST`; una recarga permite recuperar una unión confirmada o reintentar la misma identidad. — Evidencia: `mobilePlayerSession.ts:3-12,109-125` y `MobileJoinScreen.tsx:599-611` persisten `joining` antes de enviar; Playwright capturó la clave `imaginarte.mobile.v2.room.AVT246.player` antes del `POST` y recargó una sesión pendiente para recuperar el mismo jugador y avatar del snapshot privado.
- [x] Una sesión v1 antigua se elimina y vuelve al formulario sin reintentar automáticamente con avatar ausente. — Evidencia: `mobilePlayerSession.ts:64-80` elimina la clave legacy; Playwright inyectó `imaginarte.mobile.v1.room.AVT246.player`, recargó y comprobó que la clave desapareció, no se creó sesión v2, no hubo selección ni solicitud de unión y se mostró el formulario.
- [x] Tras una unión confirmada, se muestran avatar, nombre, código y «Esperando a que la TV inicie la partida»; la pantalla no permite cambiar el avatar. — Evidencia: `MobileJoinScreen.tsx:94-112,670-696`; Playwright y screenshot 390 × 844 `.playwright-mcp/spec06-confirmation-390x844.png` muestran **Marta**, **AVT246**, **Ciervo** y el mensaje requerido; la pantalla confirmada tiene cero radios y cero botones de edición.
- [x] El servidor rechaza con 400 una solicitud sin `avatarId` y con 409 una elección que ya no está disponible. — Evidencia: `src/ImaginArte.Server/Program.cs:113-140,477-483`; `tests/ImaginArte.Server.Tests/RoomEndpointTests.cs:157-210`; suites enfocadas de Server 3/3. Una petición HTTP real sin campo devolvió 400 y la carrera de navegador produjo 409; Domain/Application también verifican la unicidad.
- [x] El cliente Blazor no cambia y su solicitud actual sin avatar deja de ser aceptada. — Evidencia: `git diff --quiet main...HEAD -- src/ImaginArte.Client.Blazor` terminó con código 0; `MobileJoinClient.cs:18-21` sigue usando el DTO con `AvatarId` opcional (`RoomHttpContracts.cs:7`), que el endpoint rechaza con 400 (`Program.cs:121-126` y prueba `RoomEndpointTests.cs:157-173`). `dotnet build ImaginArte.sln` compiló Blazor.
- [x] `npm run typecheck`, `npm run lint`, `npm run build`, las suites .NET enfocadas y la revisión de navegador indicada terminan correctamente. — Evidencia: los tres comandos npm pasaron; `dotnet restore ImaginArte.sln` y `dotnet build ImaginArte.sln` pasaron; pruebas enfocadas: Domain 6/6, Application 3/3, Contracts 8/8 y Server 3/3; Playwright 390 × 844 verificó el formulario, privacidad, selección, 409, recuperación incierta, sesión v1 y confirmación. Nota: la ejecución completa no filtrada de `Server.Tests` dio 76/77 por `RoomHubTests.PostNextRound_BroadcastsFinalPublicStateAndEachPlayersPrivateState` (fase esperada 1, actual 5), ajena a avatar; el filtro enfocado de esta spec pasó.

## Decisions

- **Sí:** mostrar los 16 retratos del atlas, aunque el mockup presente ocho opciones pequeñas.
- **Sí:** usar los IDs estables de SPEC 05; los recortes visuales no son identificadores persistentes.
- **Sí:** no preseleccionar; la persona confirma explícitamente cuál será su identidad visual.
- **Sí:** mantener los avatares únicos por sala y consultar disponibilidad antes de unirse; el servidor vuelve a validarla para resolver carreras.
- **Sí:** guardar la elección pendiente con el `playerId` antes del `POST`, para que un resultado HTTP incierto no genere otra identidad.
- **Sí:** eliminar sesiones locales v1 en vez de migrarlas o recuperar una sesión sin avatar.
- **Sí:** mostrar el avatar en la confirmación, pero no permitir cambiarlo una vez unida la persona.
- **Sí:** quitar en esta entrega el fallback temporal de SPEC 05. El contrato final requiere avatar.
- **Sí:** conservar en el DTO C# el parámetro nullable opcional para compatibilidad de compilación con `MobileJoinClient.cs`, pero responder 400 si llega ausente. No se conserva compatibilidad de ejecución con el cliente Blazor.
- **No:** modificar Blazor para hacerlo compatible ni mantener un avatar por defecto para peticiones antiguas.
- **No:** añadir una suite Playwright al repositorio antes de su spec E2E; se usa la verificación de navegador disponible.

## Risks

| Risk | Mitigation |
| --- | --- |
| Dos móviles consultan la misma disponibilidad y eligen el mismo avatar. | Volver a validar en el servidor; el perdedor recibe 409, conserva sus datos y elige otro tras refrescar. |
| Cambia el código mientras se carga la disponibilidad. | Cancelar o ignorar la petición anterior y no reutilizar su snapshot ni selección. |
| El navegador bloquea `localStorage`. | Mantener el error de almacenamiento accesible y no afirmar que la elección se podrá recuperar. |
| Se borran sesiones v1 de jugadores que ya se habían unido. | Seguir la decisión de no migrarlas; explicar que la persona tendrá que volver al formulario y mostrar el error real del servidor si el jugador sigue ocupando una plaza. |
| El cliente Blazor deja de poder unirse. | Es una incompatibilidad aceptada; registrar en la documentación que la unión con avatar solo se soporta en React. |
| El contrato nuevo podría impedir compilar la solución si cambia el constructor usado por Blazor. | Mantener el parámetro nullable opcional en el DTO y verificar `dotnet build ImaginArte.sln`; rechazar el valor ausente en el endpoint. |
| Dieciséis opciones aumentan la altura del formulario. | Usar la región desplazable existente, grid adaptable, controles táctiles de 44 px y verificar el viewport móvil solicitado. |

## Referencias

- `specs/04-mobile-join-by-code.md`
- `specs/05-shared-player-avatars.md`
- `docs/06-design-system.md`
- `docs/08-screen-specifications.md`
- `docs/09-react-tutoring-guide.md`
- `docs/assets/visual-references/mockups/09-unirse-sala-movil-avatar-selector-sin-qr.png`
- `src/ImaginArte.Client.React/src/assets/player-avatars-atlas.png`
- `src/ImaginArte.Client.React/src/screens/mobile/MobileJoinScreen.tsx`
- `src/ImaginArte.Client.React/src/services/mobilePlayerSession.ts`
- `src/ImaginArte.Client.React/src/services/roomsApi.ts`
- `src/ImaginArte.Client.Blazor/Pages/JoinRoom.razor`
- `src/ImaginArte.Client.Blazor/Rooms/MobileJoinClient.cs`

## What is **not** in this spec

- Editar el avatar después de unirse.
- Migrar sesiones locales v1 o snapshots de sala antiguos.
- Actualizar Blazor o conservar compatibilidad para solicitudes sin avatar.
- Mostrar roster, tablero, manos u otra información privada en la pantalla móvil.
- Añadir QR/cámara, endpoints, dependencias o archivos de avatar nuevos.
