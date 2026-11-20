# SPEC 14 — Espera móvil tras enviar una carta

> **Estado:** Approved  
> **Depende de:** SPEC 10 — Turno del narrador en móvil; SPEC 12 — Integración del turno del jugador en móvil  
> **Fecha:** 2026-11-13  
> **Objetivo:** Presentar una espera móvil privada y coherente con el mockup después del envío del narrador o del jugador.

## Alcance

**Incluye:**

- Crear una presentación reutilizable para las esperas del narrador y del jugador, basada en la jerarquía visual de `docs/assets/visual-references/mockups/13-esperando-tras-enviar-carta-movil.png`.
- Usar `src/ImaginArte.Client.React/src/assets/sobre.png` sobre el escenario móvil oscuro y enmarcado. La captura completa es una referencia, no un recurso de interfaz.
- Para el jugador que haya enviado durante `GuessersSubmitting`, mostrar «Carta enviada» y «Esperamos a los demás. No necesitas hacer nada por ahora.».
- Para el narrador que haya enviado, conservar «Tu carta está en camino» y «Esperamos las demás historias. No necesitas hacer nada más por ahora.», sin mostrar la pista.
- Después de la fase de envíos, conservar la composición con el sobre y mostrar «Esperando a que continúe la partida» como espera neutral. No afirmar un envío si el snapshot actual no lo confirma.
- Conservar los estados de conexión, recuperación y reintento existentes. Tras reconectar, representar el snapshot privado actualizado sin repetir automáticamente ningún envío.
- Mantener el diseño estático, sin animación, y asegurar que el contenido sea legible y accesible en pantallas móviles.
- Actualizar la sección «Waiting After Sending A Card» de `docs/08-screen-specifications.md` para sustituir la indicación del panel Paper Warm por la composición oscura acordada, conservando las restricciones de privacidad y comportamiento.

**Fuera de alcance:**

- Votación, revelación, resultados personales o nuevas acciones de juego.
- Cambios en reglas, backend, contratos, persistencia, cliente Blazor o pantalla TV.
- Guardar borradores privados, mostrar la carta enviada, la mano o la pista privada del narrador.
- Añadir dependencias de animación o instalar una suite Playwright en el repositorio.

## Modelo de datos

No se introducen estructuras persistentes ni contratos nuevos. Se reutilizan el snapshot privado y sus campos `room.phase`, `player.isCurrentStoryteller` y `player.hasSubmittedGuessCard`, además del estado de conexión existente. La presentación recibe los datos necesarios para mostrar el mensaje adecuado; no decide transiciones ni inventa una confirmación.

## Plan de implementación

1. Crear `MobileWaitingAfterSubmissionScreen.tsx` y su CSS para representar la composición oscura, el sobre y los mensajes de espera mediante datos de entrada de solo lectura. Mantener el componente sin acciones de juego ni animación.
2. Integrar la presentación en `MobileJoinScreen.tsx` para el narrador, el jugador que haya enviado y la espera neutral posterior, conservando la selección de pantalla según el snapshot y los controles de recuperación existentes.
3. Ajustar el contenido a 390 × 844 y 360 × 568, incluyendo desplazamiento cuando sea necesario, áreas seguras, contraste y estados de conexión accesibles.
4. Actualizar `docs/08-screen-specifications.md` con la composición oscura acordada, sin alterar las reglas de privacidad ni ampliar las fases implementadas.

### Archivos previstos

**Nuevos:**

- `src/ImaginArte.Client.React/src/screens/mobile/MobileWaitingAfterSubmissionScreen.tsx`: contenido visual y mensajes de espera.
- `src/ImaginArte.Client.React/src/screens/mobile/MobileWaitingAfterSubmissionScreen.css`: composición adaptable del sobre y el escenario.

**Modificados:**

- `src/ImaginArte.Client.React/src/screens/mobile/MobileJoinScreen.tsx`: integración con el snapshot y los estados de conexión existentes.
- `src/ImaginArte.Client.React/src/screens/mobile/MobileJoinScreen.css`: únicamente si se necesitan ajustes del contenedor móvil para integrar la nueva vista.
- `docs/08-screen-specifications.md`: composición y estados acordados de la espera.

**Reutilizado:** `src/ImaginArte.Client.React/src/assets/sobre.png`.

## Tutoría React

- **Objetivo observable:** tras enviar, el narrador o el jugador ve un sobre y un mensaje de espera sin que aparezcan datos privados de la carta.
- **Conceptos:** una prop es un dato de entrada de solo lectura; el renderizado condicional selecciona una vista según el estado; un snapshot es una fotografía del estado actual del servidor.
- **Analogía con Blazor:** `src/ImaginArte.Client.Blazor/Pages/JoinRoom.razor` también muestra contenido condicional según el estado privado. En React, el componente vuelve a ejecutarse cuando cambian sus datos o estado; no se usa `StateHasChanged` para actualizar el estado local de React.
- **Flujo:** snapshot privado → estado de `MobileJoinScreen` → props de la espera → contenido visible. No hay intención ni comando de juego desde la espera. Tras conectar o reconectar se solicita un snapshot actualizado.
- Explicar en español los archivos, el primer renderizado, las actualizaciones, la recuperación y el desmontaje; cerrar con verificación y recapitulación didáctica.

## Criterios de aceptación

- [x] Durante `GuessersSubmitting`, un jugador cuyo snapshot confirma el envío ve «Carta enviada» y «Esperamos a los demás. No necesitas hacer nada por ahora.» junto al sobre.
  **Evidencia:** `MobileJoinScreen.tsx:687-690,750-752` condiciona el estado a la fase y `hasSubmittedGuessCard`; `MobileWaitingAfterSubmissionScreen.tsx:13-15,26-33` renderiza el texto y el sobre. Playwright MCP confirmó el snapshot privado y ambos textos; capturas 390 × 844 y 360 × 568 en `.playwright-mcp/14-mobile-waiting-after-submission/spec14-player-390x844.png` y `.playwright-mcp/14-mobile-waiting-after-submission/spec14-player-360x568.png`.
- [x] El narrador ve «Tu carta está en camino» y «Esperamos las demás historias. No necesitas hacer nada más por ahora.» en su espera tras el envío, sin ver la pista.
  **Evidencia:** `MobileJoinScreen.tsx:684-686,752-754` elige la variante narrador; `MobileWaitingAfterSubmissionScreen.tsx:8-11` define el mensaje. Playwright MCP verificó `GuessersSubmitting`, el rol de narrador, ambos textos y la ausencia de la pista privada en las capturas `.playwright-mcp/14-mobile-waiting-after-submission/spec14-narrator-390x844.png` y `.playwright-mcp/14-mobile-waiting-after-submission/spec14-narrator-360x568.png`.
- [x] Después de la fase de envíos se muestra el sobre y «Esperando a que continúe la partida» sin afirmar un envío que el snapshot actual no confirme.
  **Evidencia:** `MobileJoinScreen.tsx:691-695,754-756` selecciona espera neutral fuera de las fases previas; `MobileWaitingAfterSubmissionScreen.tsx:17-20,33` presenta el texto y el sobre. El snapshot real en `Voting` indicó `hasSubmittedGuessCard=false` para el narrador; Playwright MCP mostró solo la espera neutral en `.playwright-mcp/14-mobile-waiting-after-submission/spec14-neutral-nonsubmitter-390x844.png` y `.playwright-mcp/14-mobile-waiting-after-submission/spec14-neutral-nonsubmitter-360x568.png`.
- [x] Las esperas no muestran la mano, la carta enviada, la selección ni borradores privados, y no ofrecen acciones de juego ni otro envío.
  **Evidencia:** `MobileWaitingAfterSubmissionScreen.tsx:4-5,23-34` recibe únicamente una variante y renderiza texto e imagen decorativa; `MobileJoinScreen.tsx:748-756` no pasa datos de cartas. Playwright MCP comprobó en ambos roles que el DOM visible no contiene la pista de prueba, una mano, identificadores de carta ni controles de juego; el estado de espera tiene cero botones o campos.
- [x] La composición utiliza `sobre.png`, el escenario oscuro y el marco móvil; no usa el mockup completo como imagen de interfaz.
  **Evidencia:** `MobileWaitingAfterSubmissionScreen.tsx:1,33` importa y presenta `sobre.png`; `MobileJoinScreen.css:1-45` define el escenario Night Wash y el marco dorado. La captura muestra la composición en ambos tamaños, y el componente no referencia el mockup completo.
- [x] El diseño es estático y la confirmación se comunica mediante texto, no solo color o imagen.
  **Evidencia:** `MobileWaitingAfterSubmissionScreen.tsx:8-34` comunica cada estado con encabezado y texto; su CSS no declara animaciones (`MobileWaitingAfterSubmissionScreen.css:1-41`). Playwright MCP encontró `role="status"` y texto accesible; con `prefers-reduced-motion: reduce` emulado no hubo animaciones activas. La capa general de Motion respeta la preferencia en `App.tsx:48,76-93`.
- [x] Los estados de desconexión y recuperación siguen siendo visibles; tras reconectar se representa el snapshot privado actual sin repetir automáticamente un envío.
  **Evidencia:** `mobileRoomRealtime.ts:70-110` publica estados de reconexión y vuelve a suscribir/consultar el snapshot; `MobileJoinScreen.tsx:672-675,787-807` muestra el estado y el control de reintento. Playwright MCP mostró desconexión y «Reintentar conexión», permitió enfocarlo con Tab y, tras recuperar el servidor, observó una consulta GET del snapshot sin POST a los endpoints de envío; capturas `.playwright-mcp/14-mobile-waiting-after-submission/spec14-neutral-disconnected-390x844.png` y `.playwright-mcp/14-mobile-waiting-after-submission/spec14-neutral-disconnected-360x568.png`.
- [x] A 390 × 844 y 360 × 568 el contenido es legible, no se recorta ni desborda horizontalmente, y la recuperación permanece accesible.
  **Evidencia:** Playwright MCP verificó capturas y snapshots de narrador, jugador y espera neutral en ambos viewports; las dimensiones del documento coincidieron con el viewport. En el estado desconectado el botón quedó visible a 390 × 844 y 360 × 568, sin overflow horizontal; el desplazamiento vertical está habilitado en `MobileJoinScreen.css:52-68`.
- [x] La guía `docs/08-screen-specifications.md` describe la nueva composición y conserva las restricciones de privacidad y comportamiento.
  **Evidencia:** `docs/08-screen-specifications.md:403-433` describe la composición Night Wash con sobre, los mensajes por snapshot, la información prohibida, la espera neutral, la recuperación y los dos tamaños móviles.
- [x] Pasan `npm run typecheck`, `npm run lint` y `npm run build` desde `src/ImaginArte.Client.React`; la revisión en navegador cubre ambos roles, cambio de fase, privacidad y reconexión sin instalar Playwright en el repositorio.
  **Evidencia:** los tres comandos terminaron con código 0. Pasaron los tests enfocados de snapshot privado: `dotnet test tests/ImaginArte.Application.Tests/ImaginArte.Application.Tests.csproj --no-restore --filter FullyQualifiedName~GetPlayerPrivateState` (5/5) y `dotnet test tests/ImaginArte.Server.Tests/ImaginArte.Server.Tests.csproj --no-restore --filter FullyQualifiedName~GetPlayerPrivateState_ReturnsPublicRoomAndPrivatePlayerSnapshot` (1/1). Playwright MCP verificó narrador, jugador, cambio a `Voting`, privacidad y reconexión; no se añadió Playwright al cliente.

## Decisiones

- **Sí:** reutilizar el diseño para narrador y jugador, con mensajes diferenciados mientras corresponda.
- **Sí:** usar la composición oscura del mockup y actualizar la guía, en lugar del panel claro que esta describía.
- **Sí:** conservar una espera neutral con el sobre después de la fase de envíos, sin inventar una confirmación.
- **Sí:** mantener el diseño estático, sin dependencias de animación.
- **No:** mostrar la carta enviada ni introducir acciones o fases posteriores.
- **No:** convertir el mockup completo en un recurso de interfaz.

## Riesgos

| Riesgo | Mitigación |
| --- | --- |
| La espera posterior afirma un envío que el snapshot no confirma. | Usar un mensaje neutral y derivar la vista del estado autoritativo. |
| El nuevo diseño oculta una desconexión o el reintento. | Conservar y verificar los estados y controles de recuperación existentes. |
| El sobre o los textos se recortan en teléfonos pequeños. | Comprobar ambos tamaños previstos, desplazamiento y áreas seguras. |

## Qué **no** incluye esta spec

- Votación, revelación, resultados, reglas nuevas ni modificaciones del backend.
- Imágenes de la carta enviada, borradores privados, nuevas dependencias o cambios en Blazor y TV.
