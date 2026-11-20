# SPEC 07 — Recorrido dinámico del tablero en TV

> **Estado:** Implemented  
> **Depende de:** SPEC 03 — Lobby TV con presencia en vivo  
> **Fecha:** 2026-10-06  
> **Objetivo:** Mostrar en la TV una ruta numerada compuesta con elementos HTML y recursos PNG después de que el servidor confirme el inicio de la partida. La ruta tendrá 15, 25 o 35 nodos según el objetivo de puntos de la sala.

## Alcance

**Incluye:**

- Crear la ruta pública de TV como una capa HTML sobre el fondo ilustrado existente de “Entre cielo y tierra”. Conservar el orden y las coordenadas existentes salvo las posiciones expresamente revisadas para cada longitud en la actualización geométrica aprobada del 2026-10-07.
- Mantener oculta la ruta durante `WaitingForPlayers`. Mostrarla cuando un snapshot autoritativo confirme que la partida salió de esa fase, incluida la respuesta de inicio, `RoomUpdated`, la carga inicial y la recuperación después de reconectar.
- Seleccionar el diseño de ruta a partir de `RoomStateResponse.TargetScore`: 15 nodos para Corta, 25 para Media y 35 para Larga.
- Renderizar cada nodo como un `<span>` con `medallon-vacio.png` como `background-image`; escalar el recurso transparente de `200 x 200` a aproximadamente `65 x 65` unidades del plano de referencia. El número se mantiene como texto HTML centrado sobre el medallón y no forma parte de la imagen.
- Renderizar cada conector como un `<span>` con `conector-dorado.png` (`698 x 100`) como `background-image`. Su eje largo une los centros de dos posiciones consecutivas: ubicarlo en el punto medio, rotarlo según el ángulo entre ellas y conservar la proporción nativa `698:100`. Renderizar todos los conectores detrás de los medallones.
- Definir una lista ordenada de coordenadas por longitud de ruta en un plano de referencia de `1920 x 1080`. Usar el recorrido numerado transparente como guía para la variante de 25 nodos y las imágenes anotadas para los centros revisados por longitud, fijados en la tabla de actualización geométrica. Mantener la lectura orgánica ascendente y una separación mínima de 100 unidades entre cualquier par de centros.
- Mantener separadas la imagen de fondo, la geometría y los dos recursos visuales de ruta. Los PNG de medallón y conector son activos de ejecución; el mockup de progreso y la guía numerada siguen siendo referencias, no imágenes completas de pantalla.
- Mantener un plano responsivo 16:9 común para el fondo y las coordenadas HTML, con el mismo escalado y recorte del fondo para conservar la alineación en otros tamaños de TV.
- Mantener la ruta en la superficie pública de TV durante las fases de partida. La ruta es periférica y deja libres las zonas centrales definidas en `docs/07-progress-board.md`; la franja superior queda disponible para el recorrido.
- Tras confirmar el inicio, ocultar del encabezado superior los textos **ImaginArte · Sala familiar** y **El narrador prepara la historia**. La fase, la identidad del narrador y la instrucción de espera se presentan en la ventana flotante central. El estado de conexión en tiempo real se presenta en la esquina inferior derecha.
- Conservar el nodo final ya aprobado de las tres rutas en `{ x: 1836, y: 252 }`. Esta actualización no mueve ni rediseña la llegada al portal; solo cambia los centros enumerados en la tabla geométrica vigente.
- Hacer que la capa HTML escale junto con el fondo, conserve los números legibles a distancia de TV y exponga el recorrido como una lista accesible de posiciones numeradas; los conectores decorativos no se anuncian por separado.
- Mantener la ruta estática: no necesita animación para comunicar su forma o sus números y respeta `prefers-reduced-motion`.
- Actualizar `docs/07-progress-board.md` y `docs/08-screen-specifications.md` para que describan la longitud y el momento de visibilidad acordados.

**Fuera de alcance (para futuros specs):**

- Fichas, avatares o constelaciones de jugadores sobre los nodos.
- Posiciones de progreso por jugador, puntuación, movimiento, estelas, halos, líderes o respuesta visual del portal.
- El panel central de resumen, el botón **Siguiente ronda** y la implementación de la pantalla completa posterior a puntuación del mockup 07.
- Cambios a reglas, fases, contratos, endpoints, dominio, persistencia o SignalR del servidor.
- Mostrar el tablero en móvil, implementar pantallas móviles o modificar el cliente Blazor.
- Incorporar los PNG completos de los mockups o de la guía como fondos de pantalla o sustitutos del recorrido compuesto. Los únicos PNG de ruta permitidos en esta entrega son el medallón y el conector especificados arriba.
- Añadir dependencias, configuración de Playwright o una suite E2E al repositorio.

## Modelo de datos

No se añaden modelos, contratos ni persistencia. La ruta consume el snapshot público ya existente:

- `phase` determina si el servidor ya confirmó el inicio. `WaitingForPlayers` no muestra la ruta; las fases posteriores sí.
- `targetScore` (C# `TargetScore`) selecciona uno de los diseños estáticos: 15, 25 o 35 posiciones. La UI no ofrece un selector local ni inventa un modo alternativo.

Si el inicio falla, la pantalla permanece en el lobby sin ruta. Si se pierde la respuesta HTTP, el cliente recupera un snapshot y muestra la ruta únicamente si ese estado autoritativo ya refleja el inicio.

## Actualización geométrica aprobada — 2026-10-07

Las imágenes anotadas compartidas durante la revisión fijan los centros específicos que cambian en cada longitud. Esta revisión sustituye el ajuste anterior de Corta y Larga: en Media mantiene las posiciones 12–14 aprobadas previamente y mueve solo la 11; en Corta vuelve a los centros previos de las posiciones 12 y 14 y mueve las posiciones 8 y 13; en Larga vuelve a los centros previos de las posiciones 12–14 y mueve las posiciones 16–18.

| Ruta | Centros revisados en esta versión | Nodo final (sin cambios) |
| --- | --- | --- |
| Corta (`15`) | 8 `(508, 178)`; 12 `(1513, 142)`; 13 `(1678, 289)`; 14 `(1783, 438)` | `(1836, 252)` |
| Media (`25`) | 11 `(330, 336)`; 12 `(300, 195)`; 13 `(493, 130)`; 14 `(688, 120)` | `(1836, 252)` |
| Larga (`35`) | 12 `(297, 629)`; 13 `(355, 544)`; 14 `(271, 484)`; 16 `(290, 255)`; 17 `(440, 165)`; 18 `(580, 163)` | `(1836, 252)` |

Estas coordenadas son canónicas para esta revisión. En Corta y Larga, las posiciones 12–14 indicadas vuelven a sus centros previos; en Media se conservan los centros 12–14 de la revisión anterior. Todas las demás posiciones no enumeradas y el orden de cada lista permanecen sin cambios. La ubicación terminal existente se considera correcta y queda fuera de esta reorganización.

## Plan de implementación

1. **Definir los datos geométricos.** Crear listas ordenadas de centros para las rutas de 15, 25 y 35 posiciones en el plano de referencia de `1920 x 1080`. Aplicar los centros específicos de la actualización geométrica aprobada, conservar el orden y el nodo final, y validar separación mínima de 100 unidades y zonas centrales seguras.
2. **Crear las primitivas HTML.** Añadir un componente de ruta que posicione los `<span>` de conector y nodo dentro del plano 16:9. Importar los dos PNG aprobados, orientar y escalar cada conector entre centros, colocar los medallones encima y renderizar cada número como texto. Usar identidad estable por nodo.
3. **Integrar la ruta en TV.** La pantalla elige el diseño usando `targetScore` del snapshot y solo lo monta cuando `phase` ya no es `WaitingForPlayers`. No cambiar el resultado visual basándose únicamente en el clic de inicio. En la pantalla posterior al inicio, retirar los dos textos del encabezado superior, conservar la información de fase/narrador en la ventana flotante central y mover el estado de conexión a la esquina inferior derecha.
4. **Proteger composición y acceso.** Mantener conectores y nodos fuera de las zonas centrales reservadas y verificar la alineación del plano responsivo. Exponer los números como elementos de una lista accesible y ocultar de tecnología asistiva los conectores decorativos. La franja superior puede alojar nodos y conectores; el estado de conexión inferior derecho no debe quedar cubierto.
5. **Validar los tres tamaños y la recuperación.** Ejecutar `npm run typecheck`, `npm run lint` y `npm run build`; comprobar en navegador el lobby, el inicio confirmado y la recuperación por snapshot, en formato TV y con movimiento reducido. No añadir dependencias de pruebas al repositorio.

### Archivos previstos

**Nuevos:**

- `src/ImaginArte.Client.React/src/components/progress-board/ProgressRoute.tsx`
- `src/ImaginArte.Client.React/src/components/progress-board/ProgressRoute.css`
- `src/ImaginArte.Client.React/src/models/progressRouteLayouts.ts`

**Modificados:**

- `src/ImaginArte.Client.React/src/screens/tv/TvLobbyScreen.tsx`
- `src/ImaginArte.Client.React/src/screens/tv/TvLobbyScreen.css`
- `docs/07-progress-board.md`
- `docs/08-screen-specifications.md`

**Activos visuales de ejecución:**

- `src/ImaginArte.Client.React/src/assets/medallon-vacio.png`
- `src/ImaginArte.Client.React/src/assets/conector-dorado.png`

**Sin cambios:** dominio, aplicación, contratos, infraestructura, servidor, cliente Blazor, snapshots, endpoints y configuración de dependencias.

## Tutoría React

- **Objetivo observable:** al confirmarse el inicio desde la TV, aparece sobre el paisaje una ruta con 15, 25 o 35 círculos numerados, según el objetivo guardado en la sala. Antes del inicio no aparece; tampoco se añade el tablero a móvil.
- **Conceptos nuevos:** componente React reutilizable, props (datos que un componente recibe), `span`, `background-image`, posicionamiento porcentual, rotación CSS y renderizado de una lista con `map` y claves estables.
- **Analogía con Blazor:** `TvLobby.razor` muestra el estado público de la sala y `RoomClient` coordina su lectura. La capa de spans equivale a una vista hija que recibe datos y dibuja posiciones, de manera parecida a un `@foreach` con estilos por elemento.
- **Límite de la analogía:** React vuelve a ejecutar el componente al recibir nuevas props; no se actualiza una entidad del dominio ni se llama a `StateHasChanged`. El componente no decide cuándo una partida puede empezar: presenta la fase confirmada por el servidor.
- **Flujo:** `snapshot HTTP o RoomUpdated → phase y targetScore → selección del diseño geométrico → spans posicionados y numerados`; tras reconectar, un snapshot fresco vuelve a determinar la visibilidad y el tamaño.
- La explicación de implementación debe identificar los archivos y su responsabilidad, recorrer el render inicial, el inicio confirmado y la reconexión, y terminar en español con una recapitulación, un glosario breve y comandos reproducibles de verificación.

## Criterios de aceptación

- [x] Con `targetScore = 15`, se renderizan exactamente 15 nodos numerados del 1 al 15 y 14 conectores.
  - Evidencia: Playwright en `http://localhost:5173/tv/S0715TV`, 1920×1080: 15 `listitem` con texto 1–15 y 14 `.progress-route__connector`; captura `.playwright-mcp/07-tv-dynamic-progress-route/spec07-short-1920x1080.png`. `TvLobbyScreen.tsx:267-270, 384-389` selecciona y monta el layout.
- [x] Con `targetScore = 25`, se renderizan exactamente 25 nodos numerados del 1 al 25 y 24 conectores.
  - Evidencia: Playwright en `http://localhost:5173/tv/S0725TV`, 1920×1080: 25 `listitem` con texto 1–25 y 24 conectores; captura `.playwright-mcp/07-tv-dynamic-progress-route/spec07-medium-1920x1080.png`. El snapshot HTTP de prueba devolvió `targetScore: 25` y fase iniciada.
- [x] Con `targetScore = 35`, se renderizan exactamente 35 nodos numerados del 1 al 35 y 34 conectores.
  - Evidencia: Playwright en `http://localhost:5173/tv/S0735TV`, 1920×1080: 35 `listitem` con texto 1–35 y 34 conectores; captura `.playwright-mcp/07-tv-dynamic-progress-route/spec07-long-1920x1080.png`. El snapshot HTTP de prueba devolvió `targetScore: 35` y fase iniciada.
- [x] En cada diseño, cualquier par de centros de nodo está separado por al menos 100 unidades del plano de referencia; ningún medallón se solapa con otro.
  - Evidencia: cálculo de distancias por pares en `progressRouteLayouts.ts:15-95`: mínimos 162,27 (Corta, posiciones 13–15), 103,74 (Media, 20–21) y 101,00 (Larga, 27–28) unidades; todos superan 100. Los medallones se renderizan de 65×65 unidades (`ProgressRoute.css:25-37`), por lo que esas distancias evitan solapamiento.
- [x] Los conectores y nodos son elementos `<span>` con `conector-dorado.png` y `medallon-vacio.png` como fondos, respectivamente; no se usa SVG para dibujar la ruta.
  - Evidencia: `ProgressRoute.tsx:29-39, 43-55` renderiza spans; `ProgressRoute.css:13-22, 25-37` aplica las dos imágenes. `sips` confirmó 698×100 y 200×200. En el componente no hay dibujo SVG de la ruta.
- [x] Cada conector está centrado entre dos nodos consecutivos, se rota para unir sus centros, mantiene la proporción `698:100` y queda detrás de los medallones.
  - Evidencia: `ProgressRoute.tsx:21-38` calcula longitud, ángulo y punto medio entre puntos adyacentes; `ProgressRoute.css:13-22` define `aspect-ratio: 698 / 100` y `z-index: 0`, frente a `z-index: 1` de los nodos (`:25-37`). Las tres capturas TV muestran conectores detrás de medallones.
- [x] Los números son texto HTML centrado sobre los medallones; no están horneados en los PNG.
  - Evidencia: `ProgressRoute.tsx:43-55` renderiza `{number}` como contenido del span; `ProgressRoute.css:25-46` centra el contenido y usa `medallon-vacio.png` como fondo. Los números 1–35 aparecen como texto en los snapshots accesibles y las capturas.
- [x] La ruta conserva el orden y todas las coordenadas existentes excepto los centros específicos fijados para cada longitud en la actualización geométrica aprobada. El nodo final de cada ruta permanece en `(1836, 252)`. Comparte con el fondo un plano responsivo 16:9; el cambio de activos no desplaza los nodos.
  - Evidencia: `progressRouteLayouts.ts:15-95` coincide con los centros enumerados en la tabla geométrica y conserva `(1836, 252)`. `ProgressRoute.css:1-9` limita el plano 16:9 al viewport (`min`); al mostrarse la ruta, `App.css:28-30` centra el fondo con `contain` manteniendo el degradado a pantalla completa. Playwright verificó las rutas de 15, 25 y 35 nodos a 1280×800: plano `(0, 40, 1280, 720)`, sin nodos recortados; ruta larga también sin recortes a 1366×768. Capturas: `.playwright-mcp/07-tv-dynamic-progress-route/spec07-long-1280x800-fixed.png` y `.playwright-mcp/07-tv-dynamic-progress-route/spec07-long-1366x768-fixed.png`.
- [x] La composición usa solo las coordenadas y los dos PNG de ruta aprobados; no muestra `recorrido-numerado-transparente.png` ni `07-progreso-tablero-tv.png` como una imagen de pantalla.
  - Evidencia: `ProgressRoute.tsx:1-5, 59-92` consume los puntos y monta los spans; `ProgressRoute.css:18, 34` referencia únicamente los PNG aprobados para la ruta. Las capturas `.playwright-mcp/07-tv-dynamic-progress-route/spec07-{short,medium,long}-1920x1080.png` muestran la ruta compuesta sobre el fondo, no una imagen completa de mockup/guía.
- [x] La geometría mantiene una lectura continua ascendente y deja despejadas las zonas seguras centrales en las tres longitudes.
  - Evidencia: inspección visual de las tres capturas Playwright a 1920×1080 (`.playwright-mcp/07-tv-dynamic-progress-route/spec07-short-1920x1080.png`, `spec07-medium-1920x1080.png`, `spec07-long-1920x1080.png`): el recorrido sube desde la zona inferior izquierda hacia el portal y queda en la periferia, fuera de la ventana central y las áreas seguras descritas en `docs/07-progress-board.md:41-48`. Esta verificación visual corresponde a ese viewport.
- [x] El nodo final conserva la ubicación aprobada `(1836, 252)` en las tres rutas; esta revisión no lo desplaza. La franja superior puede formar parte del recorrido.
  - Evidencia: el último punto de las listas 15, 25 y 35 es `{ x: 1836, y: 252 }` (`progressRouteLayouts.ts:31, 58, 95`); las tres capturas TV muestran el nodo final en el mismo centro del plano.
- [x] Una vez iniciado el juego, no aparecen en la parte superior **ImaginArte · Sala familiar** ni **El narrador prepara la historia**; la ventana flotante central comunica fase, narrador e instrucción de espera.
  - Evidencia: snapshots Playwright de las tres rutas iniciadas muestran `header: false` y la ventana central con “Es el turno del narrador”, “Ana” y “Está eligiendo una carta y una pista.”; capturas TV 1920×1080. La condición y la pantalla están en `TvLobbyScreen.tsx:377-410, 432-434` y `TvWaitingForStorytellerScreen.tsx:14-33`.
- [x] El estado de conexión en tiempo real aparece como texto en la esquina inferior derecha y no se solapa con ningún nodo.
  - Evidencia: los snapshots Playwright de las tres rutas muestran “Conectado en tiempo real” abajo a la derecha; sus cajas (`x=1621, y=1014, 275×42` a 1920×1080) no intersectan las cajas de los nodos. Render y ubicación: `TvLobbyScreen.tsx:697-705`, `TvLobbyScreen.css:70-83`.
- [x] La ruta no aparece en `WaitingForPlayers`, ni siquiera cuando ya se alcanzó el mínimo de personas para iniciar.
  - Evidencia: en `http://localhost:5173/tv/S07WAIT`, el snapshot HTTP seguía en fase 0 (`WaitingForPlayers`) con tres jugadores; Playwright confirmó mínimo alcanzado, control de inicio habilitado, `routeCount: 0` y `nodes: 0`. Captura `.playwright-mcp/07-tv-dynamic-progress-route/spec07-waiting-1920x1080.png`; condición en `TvLobbyScreen.tsx:280-281, 384-389`.
- [x] La ruta aparece cuando el snapshot del servidor confirma el inicio; un fallo de inicio no la muestra. Una respuesta incierta se resuelve mediante snapshot, no mediante una actualización optimista.
  - Evidencia: para `S07WAIT`, Playwright simuló HTTP 503 solo en `POST /rooms/S07WAIT/start`; el `GET` real mantuvo `phase = WaitingForPlayers`, mostró el error y dejó `routeVisible: false`, `nodes: 0`. Para `VFYUNC26`, el `POST /rooms/VFYUNC26/start` llegó al servidor y respondió 200, pero el navegador descartó esa respuesta como si se hubiera perdido; el `GET` de recuperación devolvió fase iniciada y montó 25 nodos/24 conectores. Las solicitudes observadas fueron `POST /start` 200 seguido por `GET /rooms/VFYUNC26` 200. Manejo en `TvLobbyScreen.tsx:299-375`.
- [x] Después de recargar o reconectar, la visibilidad y el número de nodos corresponden al snapshot actual, sin reproducir eventos perdidos.
  - Evidencia: tras recargar `/tv/VFYUNC26`, el snapshot inicial volvió a montar 25 nodos y 24 conectores. Para la reconexión, Playwright cerró el WebSocket de SignalR; mientras el cliente estaba desconectado se inició `VFYREC27` directamente en el servidor. Al reconectar, el cliente volvió a suscribirse y solicitó un snapshot (`roomRealtime.ts:50-77`); se observó un `GET /rooms/VFYREC27`, estado “Conectado en tiempo real” y 25 nodos/24 conectores. La actualización ocurrió mientras el cliente estaba desconectado, por lo que no dependió de reproducir el evento perdido.
- [x] La capa aparece en TV sobre el fondo existente y no se renderiza en ninguna pantalla móvil.
  - Evidencia: capturas Playwright TV a 1920×1080 muestran la ruta sobre el fondo (`.playwright-mcp/07-tv-dynamic-progress-route/spec07-{short,medium,long}-1920x1080.png`; fondo en `App.css:10-25`). En la pantalla móvil `/join/S0715TV`, viewport 390×844, Playwright confirmó `routeCount: 0`, `nodes: 0` y sin overflow horizontal; captura `.playwright-mcp/07-tv-dynamic-progress-route/spec07-mobile-join-390x844.png`.
- [x] Los números son legibles en una vista TV a 2–3 m; la ruta expone una lista accesible de posiciones numeradas y los conectores decorativos no se anuncian individualmente.
  - Evidencia: Playwright expuso un `list` con `listitem` “Posición 1…” y números como texto; los conectores tienen `aria-hidden="true"` (`ProgressRoute.tsx:29-38, 43-64`) y no aparecen en el snapshot accesible. La fuente calculada es aproximadamente 25 px en 1920×1080 (`ProgressRoute.css:39-44`). El usuario confirma que la legibilidad física a 2–3 m puede darse por completada; no se realizó una medición física adicional. Con `prefers-reduced-motion: reduce` emulado, `matchMedia` confirmó la preferencia y ruta, nodo y conector reportaron `animation-name: none`, `animation-duration: 0s` y `transition-duration: 0s`; el estado visual de la ruta permaneció estático.
- [x] `npm run typecheck`, `npm run lint` y `npm run build` terminan correctamente sin añadir dependencias.
  - Evidencia: el 2026-10-07 se ejecutaron `npm run typecheck && npm run lint && npm run build` desde `src/ImaginArte.Client.React`; los tres finalizaron con código 0. `git status` no muestra cambios en `package.json` ni `package-lock.json`.

## Decisiones

- **Sí:** usar `TargetScore` como fuente autoritativa para escoger entre 15, 25 y 35 posiciones. La ruta tiene tantos nodos como puntos objetivo.
- **Sí:** reutilizar spans de nodo y conector con los PNG aprobados; superponer texto HTML para los números y variar solo los datos de coordenadas por longitud.
- **Sí:** conservar las coordenadas y el orden al sustituir el tratamiento SVG por la capa de imágenes, salvo los centros específicos fijados por longitud en la revisión geométrica aprobada.
- **Sí:** mostrar el recorrido solo tras una confirmación del servidor de que la partida comenzó; mantenerlo como capa pública de TV durante la partida.
- **Sí:** usar las imágenes de referencia solo para guiar composición, posiciones y estilo. El fondo ilustrado continúa como un activo separado.
- **Sí:** dejar libre la franja superior para el recorrido después del inicio; mostrar la información de fase/narrador en la ventana flotante central y el estado de conexión abajo a la derecha.
- **Sí:** separar los centros de todos los nodos al menos 100 unidades en el plano de referencia, incluyendo los nodos no consecutivos del remate.
- **Sí:** aplicar los centros específicos anotados para Corta, Media y Larga en la revisión geométrica del 2026-10-07, conservando el nodo final aprobado `(1836, 252)`.
- **No:** implementar fichas, marcadores de puntaje, desplazamiento o animaciones de progreso en esta entrega.
- **No:** añadir campos al snapshot, endpoints o reglas de juego para dibujar la ruta.
- **No:** mostrar un tablero en móvil ni cambiar Blazor.

## Riesgos

| Riesgo | Mitigación |
| --- | --- |
| La ruta de 35 posiciones o su remate quedan demasiado apretados para TV. | Distribuirla por la franja superior y los bordes disponibles; verificar la separación mínima de 100 unidades, conservar el centro terminal aprobado y revisar las zonas centrales seguras. |
| El fondo y la ruta pierden alineación en pantallas con proporciones diferentes. | Mantener un plano HTML 16:9 centrado con el mismo escalado y recorte que el fondo; comprobar el recorte responsivo. |
| La UI muestra un recorrido antes de que el servidor haya aceptado el inicio. | Derivar visibilidad de `phase` en el snapshot, no del evento de clic local. |
| El mockup induce a incluir fichas o un panel de progreso en este slice. | Mantener explícito el alcance de ruta solamente; fichas, puntuación y movimiento pertenecen a un spec posterior. |

## Referencias

- `specs/03-tv-lobby-live-presence.md`
- `docs/06-design-system.md`
- `docs/07-progress-board.md`
- `docs/08-screen-specifications.md`
- `docs/09-react-tutoring-guide.md`
- `docs/assets/visual-references/mockups/recorrido-numerado-transparente.png`
- `docs/assets/visual-references/mockups/07-progreso-tablero-tv.png`
- `docs/assets/visual-references/mockups/fondo-entre-cielo-y-tierra-nuevo.png`
- `src/ImaginArte.Client.React/src/App.css`
- `src/ImaginArte.Client.React/src/screens/tv/TvLobbyScreen.tsx`
- `src/ImaginArte.Client.React/src/models/roomState.ts`
- `src/ImaginArte.Client.Blazor/Pages/TvLobby.razor`
- `src/ImaginArte.Client.Blazor/Rooms/RoomClient.cs`

## Qué no incluye esta spec

- La representación o el movimiento de las fichas de jugadores.
- El panel de resumen de puntos y el avance a la ronda siguiente.
- Cambios en las reglas, los contratos HTTP/SignalR, el dominio o la persistencia.
- Una ruta visual o una copia de la pantalla de TV en los móviles.
- Migrar, actualizar o eliminar el cliente Blazor.
