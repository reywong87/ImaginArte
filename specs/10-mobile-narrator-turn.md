# SPEC 10 — Turno del narrador en móvil

> **Estado:** Draft  
> **Depende de:** SPEC 04 — Unión móvil a sala por código; SPEC 06 — Selector de avatar en la unión móvil; SPEC 09 — Catálogo local de cartas  
> **Fecha:** 2026-10-17  
> **Objetivo:** Permitir que el narrador seleccione una carta privada y envíe una pista desde el móvil, usando el estado autoritativo de la sala y el mockup aprobado.

## Alcance

**Incluye:**

- Extender el flujo existente `/join/{roomCode}`; no crear una ruta móvil paralela.
- Recuperar el snapshot privado existente al restaurar una sesión y representar `PlayerStateUpdated` después de suscribirse como jugador. Al reconectar, recuperar un snapshot nuevo en vez de reconstruir eventos perdidos.
- Mostrar el turno solo si el estado privado del jugador indica `isCurrentStoryteller === true` y el snapshot público está en `StorytellerChoosing`.
- Mantener a los demás jugadores en la pantalla de espera existente. No implementar su turno de selección de carta.
- Presentar la mano privada de seis cartas recibida del servidor, con sus imágenes resueltas por SPEC 09. No derivar ni alterar la propiedad de cartas en React.
- Seguir `docs/assets/visual-references/mockups/11-turno-narrador-movil.png` para la composición, pero mantener las etiquetas y controles fuera de la ilustración.
- Usar el escenario móvil Night Wash y el marco dorado existentes. Mostrar la mano en un área desplazable con cartas `2:3`; mantener la pista y la acción en una zona inferior estable sobre el área segura.
- Mostrar «Tu turno», «Elige una carta y crea una pista» y «La pista debe ayudar a algunos jugadores, pero no a todos» como jerarquía de instrucción.
- Permitir una sola carta seleccionada y una pista editable. Ambas elecciones son reversibles hasta el envío.
- Validar la pista únicamente como texto no vacío después de recortar espacios. No imponer un máximo nuevo de caracteres.
- Mantener deshabilitada la acción **Enviar carta y pista** hasta que haya carta elegible y pista no vacía. Enviar explícitamente a `POST /rooms/{roomCode}/storyteller-card` con `playerId`, `cardId` y `clue`.
- Mantener el borrador solamente en memoria. No guardar la carta elegida ni la pista en `localStorage` o en el servidor antes del envío.
- Tras confirmar el envío, mostrar la espera compacta de la pantalla 12 sin volver a mostrar la mano, la carta elegida ni la pista privada. No ofrecer otra acción de juego móvil en esta spec.
- Si falla el envío, conservar el borrador y mostrar un error accesible. Si el resultado del POST es incierto, consultar el estado privado antes de permitir otro intento; no repetir el POST automáticamente.
- Actualizar la sección del narrador y la espera posterior al envío en `docs/08-screen-specifications.md`.

**Fuera de alcance (para futuras specs):**

- El turno móvil del jugador que adivina, votación, revelación, resultados personales y avance de ronda.
- Nuevos endpoints, contratos, eventos, campos de estado, reglas de dominio o persistencia.
- Cambiar el cliente Blazor, la pantalla de TV, la rotación del narrador o la validación del backend.
- Guardar borradores privados entre recargas o dispositivos.
- Subir imágenes a Cloudflare R2/CDN o añadir una dependencia de animación o una suite Playwright al repositorio.

## Modelo de datos

La pantalla reutiliza los contratos existentes. No añade datos al servidor ni a la persistencia.

```ts
interface NarratorDraft {
  selectedCardId: string | null
  clue: string
}

interface GetPlayerPrivateStateResponse {
  room: RoomStateResponse
  player: {
    playerId: string
    hand: { cardId: string }[]
    isCurrentStoryteller: boolean
  }
}
```

`NarratorDraft` es estado efímero de la interfaz. La mano, el rol y la fase vienen del servidor. La mutación usa el `ChooseStorytellerCardRequest` y el endpoint ya existentes. Después de una recarga se restaura la identidad guardada, se consulta el estado privado y se vuelve a mostrar la pantalla que corresponda a la fase autoritativa; el borrador no se recupera.

## Plan de implementación

1. Añadir al cliente HTTP React una función para enviar la carta y la pista al endpoint existente, validando la respuesta y representando errores HTTP sin añadir un contrato nuevo.
2. Extender `mobileRoomRealtime` para entregar estados privados validados a la pantalla móvil. Integrar recuperación inicial y tras reconexión con snapshots; conservar la vista de espera de los no narradores.
3. Crear `MobileNarratorTurnScreen.tsx` y su CSS para presentar instrucción, mano privada, selección reversible, campo de pista y acción accesible. Resolver cada imagen mediante el catálogo de SPEC 09 y deshabilitar la carta cuyo JPG no esté disponible.
4. Conectar el envío, estados `submitting`/`error`/`waiting` y recuperación ante una respuesta incierta. Al confirmar el cambio de fase, desmontar la mano y mostrar la espera compacta sin duplicar el comando.
5. Actualizar `docs/08-screen-specifications.md` y verificar en navegador el flujo completo en viewport móvil, además de ejecutar los comandos de calidad del cliente React.

### Archivos previstos

**Nuevos:**

- `src/ImaginArte.Client.React/src/screens/mobile/MobileNarratorTurnScreen.tsx`
- `src/ImaginArte.Client.React/src/screens/mobile/MobileNarratorTurnScreen.css`

**Modificados:**

- `src/ImaginArte.Client.React/src/screens/mobile/MobileJoinScreen.tsx`
- `src/ImaginArte.Client.React/src/screens/mobile/MobileJoinScreen.css`
- `src/ImaginArte.Client.React/src/services/roomsApi.ts`
- `src/ImaginArte.Client.React/src/services/mobileRoomRealtime.ts`
- `docs/08-screen-specifications.md`

**Sin cambios:**

- `src/ImaginArte.Client.React/src/models/roomState.ts`; ya representa los snapshots públicos y privados necesarios.
- `src/ImaginArte.Server/Program.cs`, contratos, aplicación, dominio, infraestructura, base de datos y cliente Blazor.
- `App.tsx` y la ruta `/join/{roomCode}`.
- Manifiesto e imágenes; los define SPEC 09 y los JPG locales siguen ignorados.

## Tutoría React

- **Objetivo observable:** quien tiene el turno de narrador ve su mano privada, elige una carta, escribe una pista y confirma el envío; el resto sigue esperando.
- **Conceptos:** renderizado condicional desde props/estado autoritativo, estado local reversible, formulario controlado, `async/await`, callbacks de SignalR y limpieza de suscripciones.
- **Analogía con Blazor:** `MobileJoinScreen.tsx` y `JoinRoom.razor` reciben o consultan el estado de la persona y eligen qué vista presentar. `RoomClient.cs` ayuda a comparar la lectura remota y la notificación de cambios.
- **Límite de la analogía:** el componente React no modifica la prop ni decide la fase legal. El setter de estado provoca un nuevo render; HTTP envía el comando y solo el servidor confirma la transición.
- **Flujo:** sesión móvil guardada → snapshot privado → `isCurrentStoryteller` y `phase` → pantalla o espera → borrador local → POST → snapshot/actualización autoritativa → espera. Después de reconectar se lee el estado actual, no se reproducen eventos perdidos.
- La implementación se explicará en español antes y después del cambio, incluyendo archivos, primer renderizado, selección, validación, envío, reconexión, desmontaje y verificación reproducible.

## Criterios de aceptación

- [ ] En `/join/{roomCode}`, un snapshot con `isCurrentStoryteller === true` y fase `StorytellerChoosing` muestra el turno del narrador.
- [ ] Un jugador que no es el narrador permanece en la espera existente y no recibe la mano de otra persona.
- [ ] La mano renderizada coincide con los IDs privados del snapshot y las imágenes se resuelven por el manifiesto de SPEC 09.
- [ ] Las cartas mantienen proporción `2:3`, tienen etiquetas accesibles de posición y nombre, y el texto de selección no cubre la ilustración.
- [ ] La selección de carta se puede cambiar antes del envío y no envía ningún comando por sí sola.
- [ ] La pista tiene etiqueta visible, se puede editar y solo se considera válida cuando contiene texto no blanco después de recortar espacios.
- [ ] No se impone un límite de caracteres que no exista en las reglas actuales.
- [ ] **Enviar carta y pista** solo se habilita con una carta elegible y una pista válida; su objetivo táctil mide al menos 44 × 44 px.
- [ ] El botón envía una sola solicitud al endpoint existente con el `playerId`, el ID de carta seleccionado y la pista recortada.
- [ ] Un envío correcto muestra confirmación/espera y no vuelve a renderizar la mano ni la carta elegida.
- [ ] La espera no expone la mano ni el borrador privado, y no ofrece votación, revelación u otra acción móvil.
- [ ] Si falta la imagen de una carta del manifiesto, la carta aparece como no disponible, se deshabilita y no puede enviarse.
- [ ] Ante error HTTP, el borrador permanece y el error se comunica de forma accesible.
- [ ] Ante respuesta incierta, el cliente consulta el estado privado antes de permitir reintentar; si la fase ya avanzó, muestra la espera y no repite la mutación automáticamente.
- [ ] Tras conectar o reconectar, la pantalla consulta un snapshot fresco. Si la fase o el rol ya cambiaron, descarta el borrador y representa el estado actual.
- [ ] Recargar recupera la identidad móvil y el estado del servidor, pero no restaura una carta o pista que aún no se había enviado.
- [ ] A 390 × 844 px y en un viewport móvil bajo, la mano puede desplazarse y los controles quedan por encima del área segura inferior.
- [ ] El foco de teclado es visible, la selección no depende solo del color y los anuncios de envío/error usan semántica accesible.
- [ ] La pantalla respeta `prefers-reduced-motion` y no añade animación o dependencia nueva.
- [ ] La TV no muestra la mano, carta seleccionada ni pista antes del envío.
- [ ] `npm run typecheck`, `npm run lint` y `npm run build` terminan correctamente sin añadir dependencias ni una suite E2E al repositorio.
- [ ] La verificación de navegador cubre selección reversible, validación, envío, error/recuperación, privacidad y layout móvil con las herramientas disponibles.

## Decisiones

- **Sí:** usar la ruta y sesión móvil existentes. Evita crear otra identidad o ruta para la misma persona.
- **Sí:** decidir la pantalla desde `isCurrentStoryteller` y `phase` autoritativos; React representa el estado, no rota narradores ni valida reglas de dominio.
- **Sí:** reutilizar el POST existente y recuperar snapshots después de conectar, reconectar o resolver un resultado incierto.
- **Sí:** mantener el borrador en memoria y no persistir una pista privada antes de enviarla.
- **Sí:** conservar carta y pista ante un error confirmado y exigir una consulta de estado antes de reintentar un POST incierto.
- **Sí:** mostrar una espera posterior al envío, sin carta, mano, pista privada ni acciones de fases futuras.
- **No:** implementar en este slice el turno de los demás jugadores, votación, revelación o puntuación móvil.
- **No:** representar el mockup PNG como pantalla completa ni cubrir la ilustración con etiquetas de selección.
- **No:** añadir Motion, Playwright, contratos o endpoints nuevos.

## Riesgos

| Riesgo | Mitigación |
| --- | --- |
| Los activos están ignorados y pueden faltar fuera del entorno local. | Deshabilitar únicamente las cartas sin imagen y explicar la ausencia; el alojamiento productivo queda para una spec de Cloudflare. |
| Una respuesta HTTP incierta podría causar un envío duplicado. | Consultar el snapshot privado y dejar que la fase autoritativa determine si el comando fue aceptado; nunca repetir automáticamente. |
| La sesión SignalR puede perder actualizaciones mientras el móvil está desconectado. | Solicitar un snapshot fresco al suscribirse o reconectar; no reconstruir estado reproduciendo eventos perdidos. |
| La mano, selección o pista podrían filtrarse a la TV. | Mantener el estado privado en el flujo móvil y probar explícitamente que no se añade a la superficie de TV. |
| La altura del viewport puede ocultar la pista o el botón. | Desplazar solo el área de cartas, reservar la zona de controles y verificar safe inset en viewports móviles. |

## Referencias

- `specs/04-mobile-join-by-code.md`
- `specs/06-mobile-avatar-selection.md`
- `specs/09-local-card-catalog.md`
- `docs/06-design-system.md`
- `docs/08-screen-specifications.md`
- `docs/09-react-tutoring-guide.md`
- `docs/assets/visual-references/mockups/11-turno-narrador-movil.png`
- `docs/assets/visual-references/mockups/13-esperando-tras-enviar-carta-movil.png`
- `src/ImaginArte.Client.React/src/screens/mobile/MobileJoinScreen.tsx`
- `src/ImaginArte.Client.React/src/services/roomsApi.ts`
- `src/ImaginArte.Client.React/src/services/mobileRoomRealtime.ts`
- `src/ImaginArte.Client.React/src/models/roomState.ts`
- `src/ImaginArte.Contracts/Rooms/RoomHttpContracts.cs`
- `src/ImaginArte.Server/Program.cs` (`POST /rooms/{roomCode}/storyteller-card`)

## Qué **no** incluye esta spec

- El turno de selección de carta del jugador, votos, revelación, puntuación o fin de ronda en móvil.
- Cambios en reglas, backend, contratos, snapshots, persistencia, SignalR o el cliente Blazor.
- Subir los JPG a Git/Cloudflare, guardar borradores privados o añadir una suite E2E al repositorio.
- Alterar las pantallas TV o revelar antes de tiempo información privada del narrador.
