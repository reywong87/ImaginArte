# SPEC 04 — Unión móvil a sala por código

> **Status:** Implemented
> **Depends on:** SPEC 02 — Inicio TV: creación y reanudación de sala; SPEC 03 — Lobby TV con presencia en vivo
> **Date:** 2026-10-02
> **Objective:** Implementar la pantalla móvil React para unirse a una sala con código y nombre, recordar la identidad por sala y confirmar la unión sin implementar el lobby móvil completo.

## Scope

**In:**

- Añadir las rutas `/join` y `/join/{roomCode}` al cliente React. `/join` permite escribir el código y `/join/{roomCode}` lo precarga en el formulario, pero sigue permitiendo editarlo.
- Reconstruir la pantalla del mockup con React y CSS siguiendo `docs/06-design-system.md`; el PNG es una referencia visual, no una imagen que se renderiza como pantalla.
- Mantener la entrada manual como flujo principal. El móvil no muestra un QR ni solicita acceso a la cámara.
- Normalizar el código con espacios exteriores recortados y mayúsculas. Aceptar exactamente seis caracteres del alfabeto actual de la TV: `ABCDEFGHJKLMNPQRSTUVWXYZ23456789`.
- Recortar el nombre y exigir entre 1 y 20 caracteres en el formulario móvil React.
- Unirse usando el contrato existente `POST /rooms/{roomCode}/players` con `playerId` y `playerName`; el servidor sigue validando si la sala acepta jugadores.
- Guardar por sala el código, el identificador estable del jugador y el nombre para recuperar la sesión después de una recarga o al volver a abrir el enlace.
- Resolver un resultado HTTP incierto antes de repetir la unión: consultar la sesión privada del mismo jugador y reutilizar su `playerId` si se reintenta el `POST`.
- Tras unirse, conectar al hub mediante `SubscribeAsPlayer(roomCode, playerId)`, mantener la conexión mientras la pantalla siga montada y volver a suscribirse al reconectar para recuperar un snapshot actual.
- Mostrar la confirmación en la misma ruta: nombre del jugador, código de sala y el mensaje «Esperando a que la TV inicie la partida».
- Cambiar `VITE_MOBILE_JOIN_BASE_URL` para que apunte al cliente React. El QR existente de la TV seguirá abriendo `/join/{roomCode}`.
- Mantener etiquetas visibles, foco de teclado, errores accesibles, estados de carga y áreas táctiles de al menos 44 × 44 px.

**Out of scope (for future specs):**

- El selector de avatar del mockup, tanto la elección local como su persistencia o publicación para la TV. Se omite por completo de esta pantalla hasta que una spec defina su comportamiento compartido.
- La pantalla completa de lobby móvil «Lobby Waiting» de `docs/08-screen-specifications.md`, el roster móvil y las transiciones posteriores a iniciar la partida.
- Un escáner QR con cámara en el teléfono.
- Cambios a entidades de dominio, persistencia de sala, endpoints o contratos del servidor. Se usan las operaciones actuales de unión, recuperación privada y SignalR.
- Cambios o eliminación del cliente Blazor, React Router, dependencias nuevas, una configuración E2E nueva o cambios a las reglas de juego.

## Modelo de datos

No se añaden datos de juego, endpoints ni contratos del servidor. El cliente conserva solamente la identidad de esta persona en el navegador:

```ts
interface StoredMobilePlayerSession {
  roomCode: string
  playerId: string
  playerName: string
  status: 'joining' | 'joined'
}
```

La clave local está versionada y usa el código normalizado:

```text
localStorage["imaginarte.mobile.v1.room.{ROOM_CODE}.player"]
```

- `playerId` es el mismo identificador enviado en `JoinRoomRequest` y se conserva antes de enviar el primer `POST`, para reutilizarlo si la respuesta de red se pierde.
- `status` distingue una unión cuyo resultado todavía debe verificarse (`joining`) de una unión confirmada (`joined`). Al recuperar una sesión, el cliente consulta `GET /rooms/{roomCode}/players/{playerId}`. Si una sesión `joining` no existe aún en el servidor, puede reintentar el `POST` con el mismo identificador. Si una sesión `joined` devuelve 404, se elimina como obsoleta y se vuelve al formulario.
- Un error de red durante la recuperación conserva los datos y ofrece reintentar; no se interpreta como una sala inexistente.
- La respuesta privada recibida para recuperar o suscribir al jugador permanece en memoria y no se renderiza en esta pantalla. No se guarda información de manos, cartas, avatares ni del tablero.

## Plan de implementación

1. **Añadir la ruta y la pantalla estática.** Extender `App.tsx` para resolver `/join` y `/join/{roomCode}`. Crear `MobileJoinScreen.tsx` y `MobileJoinScreen.css` con campos accesibles de código y nombre, diseño móvil y sin selector de avatar ni QR. Comprobar ambas rutas y ejecutar `npm run build`.
2. **Implementar el formulario y validación.** Añadir estado controlado para código, nombre, carga y error. Aplicar el alfabeto de código confirmado y los límites de nombre de 1–20; mantener el código de enlace editable. Comprobar errores visibles y foco de teclado sin enviar solicitudes inválidas.
3. **Integrar unión y recuperación.** Añadir a `roomsApi.ts` la unión y la lectura privada de jugador, y crear `mobilePlayerSession.ts` para leer, escribir y limpiar la sesión versionada. Crear y persistir un `playerId` antes del `POST`; recuperar una unión ya aceptada antes de repetir el comando y presentar la confirmación en la misma ruta.
4. **Añadir presencia móvil.** Crear `mobileRoomRealtime.ts` usando la dependencia SignalR existente. Invocar `SubscribeAsPlayer`, volver a suscribirse después de una reconexión para recibir el snapshot actual y detener la conexión al desmontar la pantalla. Comprobar que la TV muestra al jugador conectado y que la UI móvil no muestra roster, tablero ni información privada.
5. **Redirigir el enlace del QR y documentar la configuración.** Actualizar `VITE_MOBILE_JOIN_BASE_URL` en `.env.example` para el origen React y corregir las instrucciones de ejecución y despliegue en `README.md`. Confirmar que el QR existente construye `/join/{roomCode}` y que el origen configurado debe ser accesible desde el teléfono.

### Archivos previstos

**Nuevos:**

- `src/ImaginArte.Client.React/src/screens/mobile/MobileJoinScreen.tsx`
- `src/ImaginArte.Client.React/src/screens/mobile/MobileJoinScreen.css`
- `src/ImaginArte.Client.React/src/services/mobilePlayerSession.ts`
- `src/ImaginArte.Client.React/src/services/mobileRoomRealtime.ts`

**Modificados:**

- `src/ImaginArte.Client.React/src/App.tsx`
- `src/ImaginArte.Client.React/src/services/roomsApi.ts`
- `src/ImaginArte.Client.React/src/models/roomState.ts`, para tipar y validar la respuesta privada necesaria para recuperar la sesión
- `src/ImaginArte.Client.React/.env.example`
- `README.md`

**Sin cambios:** contratos, dominio, aplicación, infraestructura, endpoints del servidor, Blazor y el generador de URL del QR en `TvLobbyScreen.tsx`; este último ya construye `/join/{roomCode}` a partir de `VITE_MOBILE_JOIN_BASE_URL`.

## Tutoría React

- **Objetivo observable:** una persona introduce o recibe un código, escribe su nombre, se une a la sala y ve una confirmación privada mientras la TV actualiza su presencia.
- **Conceptos principales:** componente funcional y JSX, estado local con `useState`, formulario controlado, `async/await` para el comando HTTP, `localStorage` para identidad recuperable y limpieza de una conexión externa.
- **Analogía con Blazor:** `JoinRoom.razor` muestra el formulario y los estados; `MobileJoinCoordinator.cs` coordina unión y recuperación; `MobilePlayerSessionStore.cs` muestra la sesión persistida; `MobileRoomClient.cs` ilustra la conexión móvil.
- **Límite de la analogía:** el setter de React solicita un nuevo renderizado, no equivale exactamente a actualizar campos y llamar a `StateHasChanged`. El `POST` debe ejecutarse desde el evento de envío, no desde un efecto. SignalR entrega estado actual; el componente no copia `GameRoom` ni decide reglas.
- **Flujo:** URL/campos → estado React → validación → `POST` → sesión local → `SubscribeAsPlayer` → confirmación; al recuperar o reconectar, se consulta/suscribe para obtener estado actual, no se reproducen eventos perdidos.
- La explicación de implementación debe describir los archivos, el primer renderizado, el envío del comando, la recuperación, la limpieza de SignalR y cómo verificar los estados. Debe cerrar en español con recapitulación y glosario breve.

## Acceptance Criteria

- [x] `/join` muestra el formulario móvil y `/join/{roomCode}` precarga el código sin bloquear su edición. Evidencia: Playwright en `/join` y `/join/tst234` (campo inicialmente vacío o precargado `TST234` y editable); `src/ImaginArte.Client.React/src/App.tsx:29-44`, `src/ImaginArte.Client.React/src/screens/mobile/MobileJoinScreen.tsx:157-160,549-571`.
- [x] La pantalla se compone con React/CSS y capas reutilizables del sistema visual; no renderiza el PNG del mockup como pantalla. Evidencia: `MobileJoinScreen.tsx:3,472-474`, `MobileJoinScreen.css:9-45`; screenshot `.playwright-mcp/04-mobile-join-by-code/spec04-join-390x844.png` muestra capas CSS/ornamentos y campos renderizados, no una pantalla-imagen.
- [x] No aparece selector de avatar, QR ni control de cámara en esta entrega. Evidencia: accessibility snapshots y screenshots Playwright `.playwright-mcp/04-mobile-join-by-code/spec04-join-390x844.png` y `.playwright-mcp/04-mobile-join-by-code/spec04-joined-390x844.png`; `MobileJoinScreen.tsx:468-657` solo renderiza formulario/confirmación y privacidad.
- [x] El formulario conserva siempre la entrada manual y valida exactamente seis caracteres de `ABCDEFGHJKLMNPQRSTUVWXYZ23456789`; recorta espacios exteriores y normaliza a mayúsculas. Evidencia: `MobileJoinScreen.tsx:24-25,336-360`; Playwright rechazó `LUNA-482` sin petición y envió código ` mNp234 ` normalizado como `/rooms/MNP234/players`; el enlace minúsculo `/join/tst234` precargó `TST234`.
- [x] El nombre se recorta y solo acepta entre 1 y 20 caracteres. Ambos campos tienen etiqueta visible y los errores se anuncian de forma accesible. Evidencia: `MobileJoinScreen.tsx:343-355,584-616` valida el máximo y aplica `maxLength={20}`; a 390 × 844, Playwright escribió 21 caracteres y el campo conservó 20. Al retirar el atributo HTML solo para comprobar la validación defensiva, enviar 21 caracteres produjo `role=alert` y foco en el campo. Nombres de 1 y 20 caracteres pasaron la validación del nombre y llegaron a recuperación de sesión; la API local no estaba disponible para completar esas solicitudes.
- [x] Una unión válida envía una sola solicitud concurrente a `POST /rooms/{roomCode}/players` con `playerId` estable y nombre recortado. Evidencia: protección con `joinRequestInFlightRef` y persistencia antes del POST en `MobileJoinScreen.tsx:170-174,336-340,373-434`; dos `form.requestSubmit()` síncronos en Playwright produjeron un solo POST para `MNP234` y un jugador; el cuerpo del POST de `TST234` contenía un `playerId` y `Alice Mobile` recortado.
- [x] Un código de sala inexistente produce un mensaje claro; una sala que rechaza la unión muestra un error accesible y conserva las entradas para corregir o reintentar. Evidencia: `MobileJoinScreen.tsx:85-97,445-449`; Playwright recibió 404 para `ZXCV23` y 400 para la sala iniciada `BDF567`, mostrando alertas y conservando código/nombre; snapshot/captura 390 × 844 en `.playwright-mcp/04-mobile-join-by-code/spec04-join-404-390x844.png`.
- [x] Un fallo de red conserva los datos y ofrece reintentar. Si el servidor aceptó el `POST` pero se perdió la respuesta, la recuperación encuentra al mismo jugador o reintenta con el mismo `playerId`, sin crear una segunda identidad. Evidencia: persistencia previa y recuperación GET en `MobileJoinScreen.tsx:380-436`; en Playwright se dejó completar el POST real a `QWER23` (200) y se descartó su respuesta; la UI conservó la sesión `joining` y ofreció «Reintentar»; el retry hizo GET 200 con el mismo `playerId`, sin segundo POST, y confirmó una sola identidad.
- [x] Tras una unión correcta, la pantalla muestra en la misma ruta el nombre, el código y «Esperando a que la TV inicie la partida». Evidencia: `MobileJoinScreen.tsx:484-499`; Playwright confirmó la ruta `/join/tst234`, `Alice Mobile`, `TST234` y el mensaje requerido. El formulario limita los nuevos nombres a 20 caracteres; además, a 390 px el título de confirmación con 40 letras `W` tuvo `scrollWidth` y `clientWidth` iguales (326 px) con `overflow-wrap: anywhere` (`MobileJoinScreen.css:114-121`), para que nombres largos ya guardados también puedan envolver.
- [x] Recargar o volver al enlace recupera la identidad de la sala; una sesión confirmada que ya no existe se limpia ante 404 y permite usar el formulario de nuevo. Evidencia: `mobilePlayerSession.ts:8-26,55-90` y `MobileJoinScreen.tsx:176-270,197-240`; Playwright reabrió `/join/QWER23` (GET privado 200 y confirmación) y luego probó un `playerId` confirmado inexistente (GET 404, clave local eliminada y formulario recuperado); contrato 404 en `tests/ImaginArte.Server.Tests/RoomEndpointTests.cs:1219-1233`.
- [x] El cliente se suscribe como jugador; la TV marca su presencia como conectada. Tras reconectar, el cliente vuelve a suscribirse y recibe estado actual. Evidencia: `mobileRoomRealtime.ts:34-38,63-91` vuelve a invocar `SubscribeAsPlayer`; `RoomHub.cs:66-112` publica snapshots y presencia; `tests/ImaginArte.Server.Tests/RoomHubTests.cs:55-148` (tests de snapshots y presencia al reconectar) pasó. Playwright observó presencia conectada en `/tv/QWER23`; al reiniciar el servidor sin recargar la página móvil, el snapshot HTTP volvió a marcar al jugador conectado.
- [x] La pantalla no renderiza roster, tablero, manos ni datos privados de otras personas. Evidencia: `MobileJoinScreen.tsx:483-507,654-657` y `mobileRoomRealtime.ts:40-61` (los eventos se validan y descartan, no se entregan a la UI); screenshots de formulario y confirmación `.playwright-mcp/04-mobile-join-by-code/spec04-join-390x844.png`, `.playwright-mcp/04-mobile-join-by-code/spec04-joined-390x844.png` no muestran esos datos.
- [x] El diseño funciona en un viewport móvil de 390 × 844 px sin controles cortados; los objetivos táctiles miden al menos 44 × 44 px y el foco de teclado es visible. Evidencia: screenshots Playwright del formulario a 390 × 844 (`.playwright-mcp/04-mobile-join-by-code/spec04-join-390x844.png`, `.playwright-mcp/04-mobile-join-by-code/spec04-join-404-390x844.png`); `MobileJoinScreen.css:71-74,114-121,210-243` mantiene envoltura incluso para un título de 40 letras W (medido sin overflow horizontal), campos de 52–55 px, botón de 56 px y foco dorado de 3 px.
- [x] El QR existente de la TV usa el origen React configurado y abre `/join/{roomCode}`. La base sigue siendo configurable, no codificada para producción. Evidencia: `TvLobbyScreen.tsx:289-298,471-487,710-728` usa `VITE_MOBILE_JOIN_BASE_URL` para `QRCodeSVG` y construye `/join/{roomCode}`; `src/ImaginArte.Client.React/.env.example:1-2` configura `http://localhost:5173`, README:166-175,215 documenta el destino React; Playwright verificó la ruta profunda y el QR accesible en `/tv/QWER23`.
- [x] `npm run typecheck`, `npm run lint` y `npm run build` finalizan correctamente; una comprobación integrada con API verifica unión, recuperación, 404 y fallo de red. Evidencia: los tres scripts finalizaron con código 0; Playwright integrado con React en `http://localhost:5173` y API en `http://localhost:5080` verificó POST 200, recuperación GET 200, GET/POST 404 y pérdida simulada de respuesta tras POST aceptado seguida de retry con la misma identidad; `RoomEndpointTests.cs:124-150,1145-1233` y `RoomHubTests.cs:55-148` pasaron en las suites enfocadas.

## Decisions

- **Sí:** usar `/join` y `/join/{roomCode}`; el código del enlace se precarga, pero puede corregirse manualmente.
- **Sí:** usar el formato canónico actual de seis caracteres y no interpretar el ejemplo `LUNA-482` del mockup como formato válido de sala.
- **Sí:** limitar a 20 caracteres el nombre del formulario móvil React; no se cambia el cliente Blazor ni la validación del servidor en este slice.
- **Sí:** conservar en `localStorage` una identidad versionada por sala y reutilizar el mismo `playerId` al resolver una respuesta HTTP incierta.
- **Sí:** reutilizar `POST /rooms/{roomCode}/players`, `GET /rooms/{roomCode}/players/{playerId}` y `SubscribeAsPlayer`; no añadir contratos ni reglas duplicadas en React.
- **Sí:** confirmar la unión en la misma ruta y dejar el lobby completo para otra spec.
- **Sí:** configurar la base del QR de TV para apuntar al cliente React; no añadir un escáner al móvil.
- **No:** implementar el selector del mockup solo de forma local, porque la TV mostraría una identidad distinta. Su selección compartida se define en otra spec.
- **No:** añadir Playwright al repositorio en este slice; validar con scripts del cliente y la comprobación de navegador/integración disponible.

## Risks

| Riesgo | Mitigación |
| --- | --- |
| El servidor acepta el `POST`, pero la respuesta se pierde y el jugador intenta unirse otra vez. | Persistir el `playerId` antes del envío, comprobar primero el estado privado y reutilizar la identidad al reintentar. |
| La sesión local apunta a una sala o jugador que ya no existe. | Recuperar por HTTP; limpiar la sesión confirmada ante 404 y conservarla ante errores de red. |
| El navegador no permite acceder a `localStorage`. | Detectar el error y comunicarlo de forma accesible antes de afirmar que la sesión se podrá recuperar; no continuar silenciosamente como si se hubiera persistido. |
| El QR apunta a un origen React no accesible desde el teléfono. | Mantener visible el código manual y documentar que `VITE_MOBILE_JOIN_BASE_URL` debe ser el origen React accesible en ese entorno. |
| El mockup sugiere avatar, pero el contrato actual no lo soporta. | Omitir el selector completo y registrar su comportamiento compartido como trabajo de otra spec. |

## Qué **no** incluye esta spec

- La selección, persistencia o publicación de avatares.
- El lobby móvil completo, el roster de otros jugadores o las pantallas de juego.
- Escaneo QR con cámara o cambios en el servidor, contratos, dominio o persistencia de sala.
- La eliminación o actualización funcional del cliente Blazor.

## Referencias

- `specs/02-tv-home-room-creation.md`
- `specs/03-tv-lobby-live-presence.md`
- `docs/06-design-system.md`
- `docs/08-screen-specifications.md`, secciones «Join By Code Or QR» y «Lobby Waiting»
- `docs/09-react-tutoring-guide.md`
- `docs/assets/visual-references/mockups/09-unirse-sala-movil-avatar-selector-sin-qr.png`
- `src/ImaginArte.Client.Blazor/Pages/JoinRoom.razor`
- `src/ImaginArte.Client.Blazor/Rooms/MobileJoinCoordinator.cs`
- `src/ImaginArte.Client.Blazor/Rooms/MobilePlayerSessionStore.cs`
- `src/ImaginArte.Client.React/src/App.tsx`
- `src/ImaginArte.Client.React/src/services/roomsApi.ts`
- `src/ImaginArte.Client.React/src/screens/tv/TvLobbyScreen.tsx`
- `src/ImaginArte.Server/RoomHub.cs`
- `src/ImaginArte.Contracts/Rooms/RoomHttpContracts.cs`
