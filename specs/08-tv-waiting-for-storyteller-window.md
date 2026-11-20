# SPEC 08 — Ventana de espera del narrador en TV

> **Estado:** Implemented  
> **Depende de:** SPEC 03 — Lobby TV con presencia en vivo; SPEC 05 — Avatares compartidos y persistentes; SPEC 07 — Recorrido dinámico del tablero en TV  
> **Fecha:** 2026-10-08  
> **Objetivo:** Completar la ventana central de espera en TV con la identidad pública del narrador, su retrato y mensajes de espera o reconexión, sin alterar el fondo ni el recorrido existente.

## Alcance

**Incluye:**

- Completar la ventana existente `TvWaitingForStorytellerScreen` para la fase `StorytellerChoosing`.
- Mantener «Esperando al narrador» dentro de la ventana central, junto con la identidad y la instrucción de espera.
- Mostrar el retrato correspondiente al `avatarId` público del narrador, obtenido del atlas compartido existente.
- Mostrar el nombre autoritativo del narrador y la etiqueta de rol «NARRADOR».
- Mientras el narrador esté conectado, mostrar «Está eligiendo una carta y una pista.» y, como texto secundario, «En unos instantes, comienza la magia».
- Cuando `currentStoryteller.isConnected` sea `false`, conservar retrato e identidad y comunicar «El narrador se está reconectando…» en la ventana.
- Reutilizar `CeremonialWindowDecorations` y la composición de ventana Night Wash/dorado existentes. Ajustar el contenido al formato horizontal del mockup sin renderizar el PNG completo como interfaz.
- Mantener el contenido de la ventana accesible, legible a distancia de TV y adaptable a tamaños 16:9.
- Actualizar «Waiting For Storyteller» en `docs/08-screen-specifications.md` con el retrato, los textos, la reconexión y la composición acordados.

**Fuera de alcance (para futuras specs):**

- Cambiar el fondo ilustrado, el recorrido, sus nodos o su geometría.
- Cambiar la composición, posición o comportamiento del indicador global de conexión en la esquina inferior derecha.
- Añadir acciones de TV, cambios de fase, lógica de dominio, peticiones HTTP o suscripciones SignalR.
- Exponer la mano, carta elegida, pista sin enviar, progreso privado de selección o cualquier otro dato privado.
- Cambiar otra pantalla TV o móvil, el cliente Blazor o el backend.
- Añadir modelos, contratos, persistencia, nuevos activos visuales, dependencias, configuración o suite de pruebas Playwright al repositorio.

## Modelo de datos

Esta spec no añade modelos, contratos ni persistencia. La ventana consume el `RoomStateResponse` existente a través de la prop `room`:

```ts
interface CurrentStorytellerView {
  name: string
  avatarId: AvatarId
  isConnected: boolean
}
```

La identidad se obtiene de `room.currentStoryteller`, que procede del snapshot público autoritativo. `avatarId` se resuelve mediante `getPlayerAvatar` en `models/playerAvatars.ts`. Si `currentStoryteller` es `null`, se conserva el fallback de nombre «Narrador» ya presente y no se inventa ni muestra un retrato.

La pantalla padre ya decide cuándo montar la ventana a partir de la fase recibida. La ventana solo representa la información presente en `room`; no decide transiciones ni guarda estado local.

## Plan de implementación

1. Actualizar `TvWaitingForStorytellerScreen.tsx` para componer el título de fase, el retrato del atlas, el nombre, la etiqueta de rol y los mensajes a partir de `room.currentStoryteller`. Derivar el mensaje de reconexión de `isConnected`, conservar el fallback actual si no hay narrador y no añadir interacciones ni estado de juego.
2. Ajustar `TvWaitingForStorytellerScreen.css` para reproducir la jerarquía horizontal del mockup dentro de la ventana central existente. Mantener el retrato como foco visual, evitar que nombres largos recorten el contenido y adaptar proporciones y tipografía a pantallas TV 16:9. No modificar estilos del fondo, la ruta ni el indicador global.
3. Actualizar la sección «Waiting For Storyteller» de `docs/08-screen-specifications.md` con la composición, los textos y el estado de reconexión aprobados.

### Archivos previstos

**Modificados:**

- `src/ImaginArte.Client.React/src/screens/tv/TvWaitingForStorytellerScreen.tsx`
- `src/ImaginArte.Client.React/src/screens/tv/TvWaitingForStorytellerScreen.css`
- `docs/08-screen-specifications.md`

**Sin cambios:**

- `src/ImaginArte.Client.React/src/screens/tv/TvLobbyScreen.tsx` y `TvLobbyScreen.css`.
- `src/ImaginArte.Client.React/src/App.css`, el fondo ilustrado y los activos del recorrido.
- Modelos TypeScript, servicios, contratos HTTP/SignalR, dominio, servidor y cliente Blazor.
- Dependencias, configuración y archivos de pruebas del repositorio.

## Criterios de aceptación

- [x] En la fase `StorytellerChoosing`, el título «Esperando al narrador» aparece dentro de la ventana central; no se añade un encabezado exterior a la pantalla iniciada.
- [x] La ventana muestra el nombre y la etiqueta textual «NARRADOR» del narrador actual.
- [x] El retrato coincide con el `avatarId` del narrador en el snapshot público y usa el atlas compartido existente.
- [x] Con el narrador conectado, se muestran «Está eligiendo una carta y una pista.» y «En unos instantes, comienza la magia».
- [x] Con `currentStoryteller.isConnected === false`, la ventana conserva la identidad y el retrato y anuncia «El narrador se está reconectando…».
- [x] El estado de reconexión se representa solo como presentación del estado público; no cambia la fase ni activa una acción de TV.
- [x] La ventana no muestra manos, cartas seleccionadas, pistas sin enviar ni progreso privado de selección.
- [x] La ventana reutiliza las decoraciones ceremoniales existentes y no usa el mockup completo como imagen de pantalla.
- [x] El fondo, la ruta y el indicador global de conexión permanecen sin cambios; el indicador inferior derecho no queda cubierto por la ventana.
- [x] El retrato es decorativo para tecnología asistiva porque el nombre ya se presenta como texto. El título, nombre e instrucción conservan una jerarquía semántica y contraste legible.
- [x] A 1920 × 1080 y 1280 × 720, la ventana permanece centrada, legible y sin recortes ni desbordamiento de su contenido.
- [x] `npm run typecheck`, `npm run lint` y `npm run build` terminan correctamente sin añadir dependencias.

La verificación visual y de estados se realizará con las herramientas de navegador disponibles. No se añadirá Playwright al repositorio ni se creará configuración E2E como parte de esta spec.

### Resultados de verificación (2026-10-09)

1. **Verificado.** `TvWaitingForStorytellerScreen.tsx:21-32` coloca el título dentro del panel; `TvLobbyScreen.tsx:391-434` omite el encabezado exterior tras iniciar la partida. Capturas de navegador de ambos tamaños muestran el título dentro de la ventana.
2. **Verificado.** `TvWaitingForStorytellerScreen.tsx:34-47` presenta el nombre y «NARRADOR»; ambos aparecen en los snapshots de accesibilidad de los estados observados.
3. **Verificado.** El componente resuelve `storyteller.avatarId` con `getPlayerAvatar`; `models/playerAvatars.ts:1-28` contiene el atlas compartido. En la sesión visual se comprobó el retrato `fox` contra un snapshot público con `avatarId: fox`.
4. **Verificado.** `TvWaitingForStorytellerScreen.tsx:55-64` deriva los dos textos del estado conectado. Ambos se encontraron en el snapshot de accesibilidad y en las capturas conectadas.
5. **Verificado.** `TvWaitingForStorytellerScreen.tsx:18,55-64` cambia el mensaje a partir de `isConnected` y conserva avatar e identidad; los snapshots y capturas desconectados muestran el mensaje esperado junto al nombre y retrato.
6. **Verificado.** El componente solo deriva contenido de `room.currentStoryteller` y no tiene acciones; `TvLobbyScreen.tsx:432-434` decide el montaje según la fase. La reconexión se probó como estado de presentación.
7. **Verificado.** La ventana representa nombre, avatar y conectividad, sin renderizado de mano, carta, pista o progreso privado. La inspección de la pantalla y del snapshot público usado en la prueba no mostró esos datos.
8. **Verificado.** `TvWaitingForStorytellerScreen.tsx:25` reutiliza `CeremonialWindowDecorations`; el CSS aplica la textura botánica. La implementación compone texto y atlas, no el PNG completo del mockup. El commit `239e097` solo modificó el TSX y su CSS.
9. **Verificado.** `239e097` no modificó la pantalla padre, el fondo ni la ruta. Las capturas de 1280 × 720 y 1920 × 1080 conservan ruta e indicador inferior derecho sin solapamiento visible; en la captura 1920 × 1080, el indicador aparece en `[1621, 1014, 275, 42]`, separado de la ventana.
10. **Verificado en navegador.** El avatar usa `aria-hidden="true"`; el título y nombre son encabezados y el mensaje tiene `role="status"`/`aria-live="polite"`. La revisión visual encontró texto claro sobre la superficie Night Wash. No se hizo una medición formal de contraste ni una prueba en televisor físico.
11. **Verificado con Playwright.** En la sala temporal `LNG2R7`, el snapshot público confirmó `currentStoryteller.isConnected: true` para `NarradorConUnNombreExtremadamenteLargo`. En 1280 × 720, la ventana mide `[384, 95, 512, 531]`; el encabezado del nombre mide `[565, 153, 301, 317]`. En 1920 × 1080, la ventana mide `[576, 286, 768, 507]`; el encabezado del nombre mide `[845, 369, 460, 282]`. El panel, el nombre y los mensajes no tienen desbordamiento (`scrollWidth`/`scrollHeight` coinciden con las dimensiones de cliente). El nombre se ajusta en más líneas a 1280 × 720, pero queda completo, separado del rol y los mensajes y sin recorte. El indicador global también queda fuera del panel en ambos tamaños. Capturas: `.playwright-mcp/08-tv-waiting-for-storyteller-window/spec08-tv-connected-long-1280x720.png` y `.playwright-mcp/08-tv-waiting-for-storyteller-window/spec08-tv-connected-long-1920x1080.png`. Es verificación de navegador, no una prueba a distancia en un televisor físico.
12. **Verificado.** Ejecutados desde `src/ImaginArte.Client.React`: `npm run typecheck`, `npm run lint` y `npm run build`; los tres terminaron correctamente. El commit `239e097` no cambió manifiestos de dependencias.

Capturas de navegador disponibles localmente en `.playwright-mcp/08-tv-waiting-for-storyteller-window/`: `spec08-tv-connected-{1280x720,1920x1080}.png`, `spec08-tv-disconnected-{1280x720,1920x1080}.png` y `spec08-tv-connected-long-{1280x720,1920x1080}.png`. No se añadió Playwright ni configuración de pruebas al repositorio.

## Tutoría React

- **Objetivo observable:** la TV presenta al narrador y explica si está preparando la historia o reconectándose, usando datos públicos ya recibidos.
- **Conceptos:** composición de JSX, props y renderizado condicional derivado de una prop.
- **Analogía con Blazor:** `TvLobby.razor` también presenta el estado público de una sala. `TvWaitingForStorytellerScreen.tsx` funciona como una vista hija que recibe el estado de la sala, de forma similar a un componente Razor con parámetros.
- **Límite de la analogía:** React vuelve a ejecutar la función componente al cambiar sus props. No se necesita `useState`, `useEffect` ni `StateHasChanged` para transformar `isConnected` en el texto visible.
- **Flujo:** snapshot HTTP/SignalR → estado público en `TvLobbyScreen` → prop `room` → retrato y textos de la ventana. En una actualización de conexión, el snapshot actualizado vuelve a renderizar el mensaje correspondiente.
- La implementación se explicará en español antes y después del cambio. Identificará los archivos, recorrerá el flujo de props y renderizado y cerrará con recapitulación, glosario breve y comandos reproducibles de verificación.

## Decisiones

- **Sí:** ubicar el título de fase dentro de la ventana. La especificación de TV asigna la fase, identidad e instrucción a la ventana central después del inicio.
- **Sí:** usar el atlas existente para el retrato. El snapshot público ya incluye el `avatarId` autoritativo.
- **Sí:** usar la etiqueta «NARRADOR» para todas las personas. El modelo no contiene información de género y el rol ya se nombra así en las especificaciones del juego.
- **Sí:** mostrar la instrucción literal exigida por la especificación de pantalla junto con la frase secundaria del mockup cuando el narrador está conectado.
- **Sí:** representar la reconexión dentro de la ventana a partir de `isConnected`, conservando la identidad pública. El indicador global de conexión sigue siendo independiente.
- **Sí:** reutilizar la ventana y sus decoraciones en capas. El mockup orienta la composición, pero no sustituye la interfaz por una imagen rasterizada.
- **No:** cambiar el recorrido, el fondo, el contrato de estado o la integración de la pantalla padre.
- **No:** añadir animaciones, acciones de juego ni una nueva dependencia para esta ventana informativa.

## Riesgos

| Riesgo | Mitigación |
| --- | --- |
| El recorte del atlas no corresponde al `avatarId` recibido. | Reutilizar el catálogo de avatares y verificar visualmente el retrato contra varios IDs en el navegador. |
| Un nombre largo o una ventana reducida recortan la identidad. | Permitir que el nombre se ajuste dentro del panel y revisar ambos viewports TV definidos en los criterios. |
| El texto de la ventana compite con nodos del recorrido. | Conservar la zona central segura existente y comprobar que la posición y el tamaño del panel no cubren el indicador global ni alteran la ruta. |
| La referencia gráfica induce a mostrar información privada o elementos de otras fases. | Aplicar `docs/08-screen-specifications.md` como contrato de comportamiento y mantener el componente limitado a identidad y espera. |

## Referencias

- `specs/03-tv-lobby-live-presence.md`
- `specs/05-shared-player-avatars.md`
- `specs/07-tv-dynamic-progress-route.md`
- `docs/06-design-system.md`
- `docs/07-progress-board.md`
- `docs/08-screen-specifications.md`
- `docs/09-react-tutoring-guide.md`
- `docs/assets/visual-references/mockups/03-esperando-al-narrador-recorrido-v4-avatares-tv.png`
- `src/ImaginArte.Client.React/src/screens/tv/TvWaitingForStorytellerScreen.tsx`
- `src/ImaginArte.Client.React/src/screens/tv/TvWaitingForStorytellerScreen.css`
- `src/ImaginArte.Client.React/src/components/CeremonialWindowDecorations.tsx`
- `src/ImaginArte.Client.React/src/models/playerAvatars.ts`
- `src/ImaginArte.Client.React/src/models/roomState.ts`
- `src/ImaginArte.Client.Blazor/Pages/TvLobby.razor`

## Qué **no** incluye esta spec

- Rediseñar o reubicar el fondo ilustrado, la ruta, sus nodos o el indicador de conexión inferior derecho.
- Añadir tokens, puntuación, controles de anfitrión, cartas, pistas o información privada a la TV.
- Alterar fases, reglas, estado del servidor, contratos, persistencia, SignalR o el cliente Blazor.
- Implementar pantallas móviles, agregar imágenes nuevas o instalar dependencias o herramientas E2E en el repositorio.
