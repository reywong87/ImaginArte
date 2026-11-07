# SPEC 11 — Maqueta del turno del jugador en móvil

> **Estado:** Implemented  
> **Depende de:** SPEC 09 — Catálogo local de cartas; SPEC 10 — Turno del narrador en móvil  
> **Fecha:** 2026-11-04  
> **Objetivo:** Maquetar el turno móvil del jugador con datos de ejemplo y selección local, sin enviar cartas ni alterar la partida.

## Alcance

**Incluye:**

- Extender el flujo móvil existente `/join/{roomCode}` sin crear una ruta paralela.
- Mostrar la maqueta únicamente en desarrollo cuando el snapshot privado indique que el jugador no es narrador, la fase es `GuessersSubmitting` y `hasSubmittedGuessCard` es falso.
- Identificar de forma visible la pantalla y sus datos como ejemplo. Mostrar la pista fija «Donde termina la noche» y una mano de IDs fijos de ejemplo resueltos mediante el catálogo de SPEC 09; no presentar esa mano como la mano real.
- Seguir la composición de `docs/assets/visual-references/mockups/12-turno-jugador-movil.png` y las reglas de `docs/08-screen-specifications.md`: pista visible, cartas `2:3`, etiquetas fuera de las ilustraciones y acción accesible sobre el área segura inferior.
- Permitir elegir una carta disponible y cambiarla antes de pulsar **Enviar carta**. Una ilustración ausente se identifica y no permite seleccionar esa carta.
- Al pulsar **Enviar carta** con selección, informar accesiblemente que el envío aún no está disponible; no hacer una petición ni mostrar una confirmación de envío.
- Mantener la selección solo en memoria y descartarla al salir de la maqueta o recargar. Reevaluar su visibilidad ante cambios del estado autoritativo.
- Mantener el comportamiento actual en producción y para los demás roles, fases y jugadores que ya enviaron.

**Fuera de alcance:**

- Pista y mano reales, envío real, confirmación de recepción y espera del jugador: corresponden a SPEC 12.
- Cambios en backend, contratos, reglas, persistencia, SignalR, TV o Blazor.
- Votación, revelación, resultados, publicación de imágenes o nuevas dependencias de animación y pruebas.

## Modelo de datos

La pantalla reutiliza el snapshot existente solo para decidir cuándo mostrar la maqueta. La pista y la mano son datos de ejemplo locales; `selectedCardId: string | null` es estado efímero de presentación, no una selección confirmada por el servidor. No se añade persistencia ni un contrato nuevo.

## Plan de implementación

1. Crear una pantalla móvil con pista y mano de ejemplo, resolución mediante el catálogo existente y tratamiento de ilustraciones ausentes. Comprobar que el cliente compila y que los datos están identificados como ficticios.
2. Añadir selección local reversible, etiquetas accesibles y botón con aviso de envío no disponible. Comprobar que no se producen mutaciones HTTP.
3. Integrar la pantalla en `MobileJoinScreen.tsx` bajo la condición de desarrollo, rol, fase y envío pendiente; conservar el comportamiento de producción y descartar la selección al salir o recargar.
4. Ajustar el diseño móvil según el mockup y las restricciones de accesibilidad, desplazamiento y área segura. Revisar la pantalla en navegador y ejecutar las comprobaciones del cliente.

### Archivos previstos

**Nuevos:**

- `src/ImaginArte.Client.React/src/screens/mobile/MobilePlayerTurnScreen.tsx`: pantalla y estado local de selección.
- `src/ImaginArte.Client.React/src/screens/mobile/MobilePlayerTurnScreen.css`: composición móvil y estados visuales.

**Modificado:**

- `src/ImaginArte.Client.React/src/screens/mobile/MobileJoinScreen.tsx`: selección de la maqueta en desarrollo a partir del estado existente.

**Reutilizados sin cambios previstos:** `src/ImaginArte.Client.React/src/services/cardAssets.ts`, el manifiesto de SPEC 09 y el flujo de sesión y snapshots existente.

## Tutoría React

- **Objetivo observable:** ver la pista de ejemplo, elegir otra carta y recibir un aviso en vez de enviar datos.
- **Conceptos:** un componente es una función que describe la interfaz; las props son datos de entrada de solo lectura; el estado local conserva una elección temporal entre renderizados.
- **Analogía:** `src/ImaginArte.Client.Blazor/Pages/JoinRoom.razor` también combina estado y representación. La diferencia es que React vuelve a ejecutar la función del componente al actualizar su estado; no se llama a `StateHasChanged` para ese estado.
- **Flujo:** snapshot → condición de visualización → datos ficticios → selección local → aviso; no hay comando ni cambio autoritativo. Al desmontar se descarta la selección.
- Explicar en español los archivos, el primer renderizado, las interacciones y la verificación; cerrar con una recapitulación y un ejercicio breve.

## Criterios de aceptación

- [x] En desarrollo, la maqueta aparece en `/join/{roomCode}` solo para un jugador no narrador en `GuessersSubmitting` que aún no haya enviado carta.
  **Evidencia:** `src/ImaginArte.Client.React/src/screens/mobile/MobileJoinScreen.tsx:678-683` comprueba `import.meta.env.DEV`, rol, fase y envío pendiente; Playwright MCP en `/join/X6U3TK` mostró la maqueta con fase `2` y `hasSubmittedGuessCard=false`.
- [x] En producción y para otros roles, fases o jugadores que ya enviaron, permanece el comportamiento previo.
  **Evidencia:** la selección y el fallback anterior están en `MobileJoinScreen.tsx:675-687`; en el build de producción `/join/YEF84B` mantuvo «Esperando a que continúe la partida» sin maqueta; en desarrollo el narrador conservó «Tu carta está en camino», y tras el envío del jugador y el paso a fase `Voting` la maqueta dejó de mostrarse.
- [x] La pantalla indica que los datos son de ejemplo y muestra «Donde termina la noche» sin presentarla como pista real.
  **Evidencia:** `src/ImaginArte.Client.React/src/screens/mobile/MobilePlayerTurnScreen.tsx:26-35` muestra «Maqueta de desarrollo · Datos de ejemplo» y «Pista de ejemplo»; screenshot Playwright 390 × 844: `.playwright-mcp/page-2026-11-04T17-39-39-155Z.png`.
- [x] Las cartas de ejemplo se resuelven con el catálogo existente; las ilustraciones ausentes se identifican y no se pueden seleccionar.
  **Evidencia:** `MobilePlayerTurnScreen.tsx:5,13-23,41-58` usa `resolveCardAsset`; `src/ImaginArte.Client.React/src/services/cardAssets.ts:44-67,70-133` resuelve manifiesto e ilustración y desactiva ausencias. `npm run test:cards` pasó 4/4; en Playwright un 404 deliberado produjo «Ilustración no disponible» y radio deshabilitado.
- [x] Las cartas son `2:3`, sus etiquetas de selección no cubren la ilustración y la pista permanece visible mientras se consulta la mano.
  **Evidencia:** `MobilePlayerTurnScreen.css:64-74,109-124` y `MobilePlayerTurnScreen.tsx:60-81`; Playwright midió 153,2 × 229,8 px a 390 × 844 y, tras desplazar la mano hasta el final tanto a 390 × 844 como a 360 × 568, la pista siguió visible. Screenshots: `.playwright-mcp/page-2026-11-04T17-42-00-204Z.png` y `.playwright-mcp/page-2026-11-04T17-50-55-858Z.png`.
- [x] La selección es única y reversible, se comunica mediante texto y contorno, y permite operar con teclado y foco visible.
  **Evidencia:** radio group controlado y texto de estado en `MobilePlayerTurnScreen.tsx:37-59,80`; estilos de foco/selección en `MobilePlayerTurnScreen.css:76-107`. Playwright seleccionó la primera carta y luego cambió a la segunda con `ArrowRight`; el snapshot mostró una sola radio marcada, «Seleccionada» y foco con contorno sólido.
- [x] Sin carta seleccionada no se puede activar el envío; con selección, pulsarlo muestra un aviso accesible de indisponibilidad y no envía ninguna petición ni confirma recepción.
  **Evidencia:** `MobilePlayerTurnScreen.tsx:11,87-91` deshabilita el botón sin selección y usa `role="status"`; Playwright confirmó el estado disabled inicial, mostró el aviso tras seleccionar y pulsar, no encontró texto de recepción y el filtro de solicitudes `guess-cards` no devolvió ninguna petición.
- [x] Tras recargar o salir de la maqueta se pierde la selección ficticia y se vuelve a evaluar la condición de visualización.
  **Evidencia:** selección en estado local `MobilePlayerTurnScreen.tsx:8-10`; ruta con clave en `src/ImaginArte.Client.React/src/App.tsx:68-73`. Playwright seleccionó una carta, salió a `/tv/X6U3TK` y volvió (cero seleccionadas, botón deshabilitado); repitió la selección y recargó `/join/X6U3TK` con el mismo resultado y la condición aún elegible.
- [x] En 390 × 844 y un viewport móvil bajo, la mano es desplazable y la acción permanece accesible sobre el área segura inferior.
  **Evidencia:** scroll de cartas y ajuste para altura baja en `MobilePlayerTurnScreen.css:64-74,142-149`; insets en `MobileJoinScreen.css:52-68,496-503`. Playwright verificó 390 × 844 y 360 × 568; a 360 × 568 la mano llegó a `scrollTop=496` (773 px de contenido/277 px visibles), la pista siguió visible y el botón quedó en y=488–532 dentro de la región segura y del viewport. Screenshot: `.playwright-mcp/page-2026-11-04T17-50-55-858Z.png`.
- [x] La interfaz respeta movimiento reducido, no revela datos privados de terceros y no altera la TV.
  **Evidencia:** `src/ImaginArte.Client.React/src/App.tsx:48,76-93` configura `MotionConfig reducedMotion="user"`; Playwright emuló `prefers-reduced-motion: reduce` (true) y no encontró animaciones activas. La pantalla móvil solo mostró ejemplos y no nombres/datos de los otros jugadores; los cambios de trabajo preexistentes se limitan a los dos archivos móviles de esta pantalla y no incluyen `screens/tv/`; screenshot público `/tv/X6U3TK`: `.playwright-mcp/page-2026-11-04T17-44-05-714Z.png` (la ruta disponible mostró el lobby, no una pantalla TV de fase en curso).
- [x] `npm run typecheck`, `npm run lint` y `npm run build` terminan correctamente; la revisión de navegador cubre selección, aviso, accesibilidad y ausencia de envío.
  **Evidencia:** desde `src/ImaginArte.Client.React`, `npm run typecheck`, `npm run lint`, `npm run build` y `npm run test:cards` terminaron correctamente; los tests enfocados `dotnet test tests/ImaginArte.Application.Tests/ImaginArte.Application.Tests.csproj --no-restore --filter FullyQualifiedName~GetPlayerPrivateState` (5/5) y `dotnet test tests/ImaginArte.Server.Tests/ImaginArte.Server.Tests.csproj --no-restore --filter FullyQualifiedName~GetPlayerPrivateState_ReturnsPublicRoomAndPrivatePlayerSnapshot` (1/1) también pasaron. Playwright MCP verificó selección con teclado, aviso accesible, botones/radios y ausencia de POST de envío.

## Decisiones

- **Sí:** dividir maqueta e integración en specs diferentes para verificar la presentación antes de conectar mutaciones reales.
- **Sí:** activar la maqueta solo en desarrollo y distinguir sus datos ficticios para evitar que se interpreten como una partida real.
- **Sí:** reutilizar el catálogo local y sus estados de ausencia, sin copiar imágenes ignoradas.
- **No:** simular éxito, persistir la selección o usar el PNG completo como interfaz.

## Riesgos

| Riesgo | Mitigación |
| --- | --- |
| La maqueta se confunde con el juego real. | Limitarla a desarrollo, identificar los ejemplos y no confirmar envíos. |
| Faltan imágenes locales ignoradas por Git. | Mostrar el estado no disponible y deshabilitar únicamente las cartas afectadas. |
| Controles o pista quedan ocultos en móviles bajos. | Reservar área segura y revisar desplazamiento en varios tamaños. |

## Qué **no** incluye esta spec

- Conectar datos reales o enviar cartas; se aborda en SPEC 12.
- Modificar la lógica de juego, persistencia, TV, Blazor o implementar fases posteriores.
