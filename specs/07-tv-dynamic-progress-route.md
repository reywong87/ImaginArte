# SPEC 07 — Recorrido dinámico del tablero en TV

> **Estado:** Approved  
> **Depende de:** SPEC 03 — Lobby TV con presencia en vivo  
> **Fecha:** 2026-10-06  
> **Objetivo:** Mostrar en la TV una ruta numerada compuesta con elementos HTML y recursos PNG después de que el servidor confirme el inicio de la partida. La ruta tendrá 15, 25 o 35 nodos según el objetivo de puntos de la sala.

## Alcance

**Incluye:**

- Crear la ruta pública de TV como una capa HTML sobre el fondo ilustrado existente de “Entre cielo y tierra”, sin cambiar las coordenadas ni el orden ya definidos en `progressRouteLayouts.ts`.
- Mantener oculta la ruta durante `WaitingForPlayers`. Mostrarla cuando un snapshot autoritativo confirme que la partida salió de esa fase, incluida la respuesta de inicio, `RoomUpdated`, la carga inicial y la recuperación después de reconectar.
- Seleccionar el diseño de ruta a partir de `RoomStateResponse.TargetScore`: 15 nodos para Corta, 25 para Media y 35 para Larga.
- Renderizar cada nodo como un `<span>` con `medallon-vacio.png` como `background-image`; escalar el recurso transparente de `200 x 200` a aproximadamente `65 x 65` unidades del plano de referencia. El número se mantiene como texto HTML centrado sobre el medallón y no forma parte de la imagen.
- Renderizar cada conector como un `<span>` con `conector-dorado.png` (`698 x 100`) como `background-image`. Su eje largo une los centros de dos posiciones consecutivas: ubicarlo en el punto medio, rotarlo según el ángulo entre ellas y conservar la proporción nativa `698:100`. Renderizar todos los conectores detrás de los medallones.
- Definir una lista ordenada de coordenadas por longitud de ruta en un plano de referencia de `1920 x 1080`. Usar el recorrido numerado transparente como guía para la variante de 25 nodos y conservar su lectura orgánica ascendente en las variantes de 15 y 35. Distribuir los nodos por el espacio disponible: todos los centros deben quedar separados al menos 100 unidades para evitar solapamientos y dejar un espacio visible entre medallones.
- Mantener separadas la imagen de fondo, la geometría y los dos recursos visuales de ruta. Los PNG de medallón y conector son activos de ejecución; el mockup de progreso y la guía numerada siguen siendo referencias, no imágenes completas de pantalla.
- Mantener un plano responsivo 16:9 común para el fondo y las coordenadas HTML, con el mismo escalado y recorte del fondo para conservar la alineación en otros tamaños de TV.
- Mantener la ruta en la superficie pública de TV durante las fases de partida. La ruta es periférica y deja libres las zonas centrales definidas en `docs/07-progress-board.md`; la franja superior queda disponible para el recorrido.
- Tras confirmar el inicio, ocultar del encabezado superior los textos **ImaginArte · Sala familiar** y **El narrador prepara la historia**. La fase, la identidad del narrador y la instrucción de espera se presentan en la ventana flotante central. El estado de conexión en tiempo real se presenta en la esquina inferior derecha.
- Resolver el tramo final con una llegada clara antes del portal: los últimos nodos conservan la separación mínima, no se agrupan ni se solapan con el portal.
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

## Plan de implementación

1. **Definir los datos geométricos.** Crear listas ordenadas de centros para las rutas de 15, 25 y 35 posiciones en el plano de referencia de `1920 x 1080`. Revisar el diseño de 25 posiciones contra la guía numerada, distribuir la variante de 35 por el área disponible y resolver los remates antes del portal. Validar la separación mínima de 100 unidades y las zonas centrales seguras.
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

- [ ] Con `targetScore = 15`, se renderizan exactamente 15 nodos numerados del 1 al 15 y 14 conectores.
- [ ] Con `targetScore = 25`, se renderizan exactamente 25 nodos numerados del 1 al 25 y 24 conectores.
- [ ] Con `targetScore = 35`, se renderizan exactamente 35 nodos numerados del 1 al 35 y 34 conectores.
- [ ] En cada diseño, cualquier par de centros de nodo está separado por al menos 100 unidades del plano de referencia; ningún medallón se solapa con otro.
- [ ] Los conectores y nodos son elementos `<span>` con `conector-dorado.png` y `medallon-vacio.png` como fondos, respectivamente; no se usa SVG para dibujar la ruta.
- [ ] Cada conector está centrado entre dos nodos consecutivos, se rota para unir sus centros, mantiene la proporción `698:100` y queda detrás de los medallones.
- [ ] Los números son texto HTML centrado sobre los medallones; no están horneados en los PNG.
- [ ] La ruta conserva las coordenadas y el orden existentes en las tres listas. Comparte con el fondo un plano responsivo 16:9; el cambio de activos no desplaza los nodos.
- [ ] La composición usa solo las coordenadas y los dos PNG de ruta aprobados; no muestra `recorrido-numerado-transparente.png` ni `07-progreso-tablero-tv.png` como una imagen de pantalla.
- [ ] La geometría mantiene una lectura continua ascendente, termina antes del portal y deja despejadas las zonas seguras centrales en las tres longitudes.
- [ ] El tramo final de las tres rutas está espaciado, no se amontona ni invade el portal. La franja superior puede formar parte del recorrido.
- [ ] Una vez iniciado el juego, no aparecen en la parte superior **ImaginArte · Sala familiar** ni **El narrador prepara la historia**; la ventana flotante central comunica fase, narrador e instrucción de espera.
- [ ] El estado de conexión en tiempo real aparece como texto en la esquina inferior derecha y no se solapa con ningún nodo.
- [ ] La ruta no aparece en `WaitingForPlayers`, ni siquiera cuando ya se alcanzó el mínimo de personas para iniciar.
- [ ] La ruta aparece cuando el snapshot del servidor confirma el inicio; un fallo de inicio no la muestra. Una respuesta incierta se resuelve mediante snapshot, no mediante una actualización optimista.
- [ ] Después de recargar o reconectar, la visibilidad y el número de nodos corresponden al snapshot actual, sin reproducir eventos perdidos.
- [ ] La capa aparece en TV sobre el fondo existente y no se renderiza en ninguna pantalla móvil.
- [ ] Los números son legibles en una vista TV a 2–3 m; la ruta expone una lista accesible de posiciones numeradas y los conectores decorativos no se anuncian individualmente.
- [ ] `npm run typecheck`, `npm run lint` y `npm run build` terminan correctamente sin añadir dependencias.

## Decisiones

- **Sí:** usar `TargetScore` como fuente autoritativa para escoger entre 15, 25 y 35 posiciones. La ruta tiene tantos nodos como puntos objetivo.
- **Sí:** reutilizar spans de nodo y conector con los PNG aprobados; superponer texto HTML para los números y variar solo los datos de coordenadas por longitud.
- **Sí:** conservar las coordenadas y el orden al sustituir el tratamiento SVG por la capa de imágenes.
- **Sí:** mostrar el recorrido solo tras una confirmación del servidor de que la partida comenzó; mantenerlo como capa pública de TV durante la partida.
- **Sí:** usar las imágenes de referencia solo para guiar composición, posiciones y estilo. El fondo ilustrado continúa como un activo separado.
- **Sí:** dejar libre la franja superior para el recorrido después del inicio; mostrar la información de fase/narrador en la ventana flotante central y el estado de conexión abajo a la derecha.
- **Sí:** separar los centros de todos los nodos al menos 100 unidades en el plano de referencia, incluyendo los nodos no consecutivos del remate.
- **No:** implementar fichas, marcadores de puntaje, desplazamiento o animaciones de progreso en esta entrega.
- **No:** añadir campos al snapshot, endpoints o reglas de juego para dibujar la ruta.
- **No:** mostrar un tablero en móvil ni cambiar Blazor.

## Riesgos

| Riesgo | Mitigación |
| --- | --- |
| La ruta de 35 posiciones o su remate quedan demasiado apretados para TV. | Distribuirla por la franja superior y los bordes disponibles; verificar la separación mínima de 100 unidades, el remate antes del portal y las zonas centrales seguras. |
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
