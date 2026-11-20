# SPEC 09 — Catálogo local de cartas

> **Estado:** Approved  
> **Depende de:** SPEC 01 — Scaffold del cliente React  
> **Fecha:** 2026-10-17  
> **Objetivo:** Limitar el mazo temporal a las 98 cartas ilustradas, asociar `TEMP-001`–`TEMP-098` a sus archivos mediante un manifiesto versionado y reciclar el descarte cuando el mazo disponible no baste.

## Alcance

**Incluye:**

- Mantener los IDs temporales `TEMP-001` a `TEMP-098`, y dejar de generar IDs superiores a `TEMP-098`.
- Asociar cada ID con la imagen cuyo prefijo numérico coincide con el ordinal: `TEMP-001` con `01-…jpg` y `TEMP-098` con `98-…jpg`.
- Crear `src/ImaginArte.Client.React/public/cards/manifest.v1.json`, con `schemaVersion: 1` y los campos `cardId`, `fileName` y `alt` por carta. El `alt` contiene el nombre descriptivo del archivo sin el ordinal ni la extensión, convertido a texto legible.
- Servir los JPG desde `docs/assets/cartas/` solamente durante el desarrollo local con Vite. No copiar, versionar ni empaquetar esos archivos. No modificar `.gitignore`.
- Si un JPG asociado no está disponible localmente, indicar «Ilustración no disponible» y marcar esa carta como no seleccionable en la interfaz que la consuma.
- Cuando el mazo disponible no alcance para una reposición, barajar el descarte acumulado, devolverlo al mazo y continuar robando. Solo se reciclan cartas que ya fueron jugadas y descartadas; las cartas que siguen en manos no se duplican.
- Actualizar las reglas de juego y el README para describir el tamaño temporal del mazo y el reciclaje del descarte.
- Mantener una frontera de resolución de activos que pueda sustituirse por el alojamiento Cloudflare R2/CDN en una spec futura.

**Fuera de alcance (para futuras specs):**

- Implementar la pantalla móvil del narrador o cualquier otra pantalla que consuma las cartas; corresponde a SPEC 10 o a futuras specs móviles.
- Sustituir los IDs temporales por IDs permanentes de un catálogo de producción.
- Subir imágenes a Cloudflare, definir sus URL públicas, configurar un CDN o crear un proceso de publicación de activos.
- Añadir, editar, regenerar o copiar imágenes a una carpeta versionada del cliente.
- Cambiar las reglas de selección de carta, narración, puntuación o transición de fases.
- Añadir dependencias de terceros o una suite Playwright al repositorio.

## Modelo de datos

El dominio conserva `CardId` como identidad de juego. Esta spec modifica la cantidad de cartas temporales, pero no agrega campos a los snapshots ni guarda rutas de imagen en el dominio o la persistencia.

El manifiesto es metadato de presentación versionado, no una fuente de reglas:

```json
{
  "schemaVersion": 1,
  "cards": [
    {
      "cardId": "TEMP-001",
      "fileName": "01-relicario-botella-navegante.jpg",
      "alt": "Relicario botella navegante"
    },
    {
      "cardId": "TEMP-098",
      "fileName": "98-bailarina-taza-rota.jpg",
      "alt": "Bailarina taza rota"
    }
  ]
}
```

El archivo real contiene las 98 entradas. No existen entradas para IDs superiores a `TEMP-098`. El adaptador local convierte el nombre del archivo en una URL atendida únicamente por el servidor de desarrollo de Vite. El build de producción no incluye las ilustraciones locales.

## Plan de implementación

1. Cambiar el tamaño del mazo temporal de 120 a 98 y probar que el reparto genera exactamente `TEMP-001`–`TEMP-098`, sin IDs superiores.
2. Reciclar y barajar el descarte solamente cuando una operación de robo no pueda completarse con el mazo disponible. Añadir pruebas de dominio para partidas de hasta ocho jugadores que superen seis reposiciones y comprobar que no se duplican cartas en manos activas. Actualizar `docs/01-game-rules.md` y `README.md`.
3. Crear el manifiesto JSON versionado y una validación sin dependencias externas que compruebe su versión, las 98 entradas, la unicidad de IDs y archivos, la correspondencia ordinal y los `alt` no vacíos.
4. Añadir al servidor de desarrollo de Vite la resolución local de archivos bajo `docs/assets/cartas/`, limitada a nombres incluidos en el manifiesto y sin habilitarla en `vite build`. Mostrar como no disponible un activo local ausente. Documentar cómo comprobarlo en desarrollo sin añadir ni copiar JPG al repositorio.

### Archivos previstos

**Nuevos:**

- `src/ImaginArte.Client.React/public/cards/manifest.v1.json`
- `src/ImaginArte.Client.React/src/services/cardAssets.ts`
- `src/ImaginArte.Client.React/scripts/card-manifest.test.mjs`

**Modificados:**

- `src/ImaginArte.Domain/Room.cs`
- `tests/ImaginArte.Domain.Tests/RoomTests.cs`
- `src/ImaginArte.Client.React/vite.config.ts`
- `src/ImaginArte.Client.React/package.json`
- `docs/01-game-rules.md`
- `README.md`

**Sin cambios:**

- `docs/assets/cartas/` y sus JPG; son activos locales ignorados.
- `.gitignore`, contratos HTTP/SignalR, snapshots, persistencia, endpoints y cliente Blazor.
- Imágenes en PostgreSQL, el cliente React de producción y la configuración de Cloudflare.

## Tutoría React

- **Objetivo observable:** el ID de carta recibido del servidor se resuelve a una ilustración local durante el desarrollo, sin convertir una ruta de archivo en una regla del juego.
- **Conceptos:** manifiesto JSON versionado, resolución de URL y diferencia entre el servidor de desarrollo y los archivos incluidos en un build.
- **Analogía con Blazor:** `public/` se parece a `wwwroot` como ubicación de recursos estáticos. El límite es que los JPG locales viven fuera del cliente y el middleware de desarrollo no los copia al build como si fueran contenido publicado.
- **Flujo:** `CardId` del servidor → entrada del manifiesto → URL local de desarrollo → imagen o estado «no disponible». El catálogo no cambia el ID que se envía al dominio.
- La implementación explicará la responsabilidad del manifiesto y del adaptador, y verificará el flujo local y el build sin depender de las ilustraciones ignoradas para compilar.

## Criterios de aceptación

- [x] El dominio genera exactamente 98 IDs temporales, desde `TEMP-001` hasta `TEMP-098`, y no genera IDs superiores.
- [x] Con hasta ocho jugadores, el dominio puede reponer manos durante más de seis rondas al reciclar y barajar el descarte cuando el mazo disponible es insuficiente.
- [x] El reciclaje solo mueve cartas del descarte al mazo y no crea IDs duplicados en las manos activas.
- [x] El manifiesto tiene `schemaVersion: 1` y exactamente 98 entradas únicas.
- [x] Para cada entrada, el número de `TEMP-NNN` coincide con el prefijo numérico del archivo asociado.
- [x] Cada entrada incluye un `alt` legible derivado del nombre descriptivo del archivo, sin prefijo numérico ni extensión.
- [x] El desarrollo local puede servir las imágenes existentes desde `docs/assets/cartas/` sin copiar ni versionar los JPG.
- [x] El servidor local solo sirve archivos nombrados en el manifiesto y rechaza rutas que salgan de `docs/assets/cartas/`.
- [x] Si falta un JPG asociado, la interfaz puede identificar la carta como no disponible y no permite seleccionarla.
- [x] `npm run test:cards`, `npm run typecheck`, `npm run lint` y `npm run build` terminan correctamente sin nuevas dependencias.
- [x] `dotnet build ImaginArte.sln` y `dotnet test ImaginArte.sln --no-restore -m:1` terminan correctamente.
- [x] `vite build` no copia los JPG ignorados al resultado de producción ni incluye el middleware local de desarrollo.
- [x] No se modifica `.gitignore` y ningún JPG de `docs/assets/cartas/` aparece como archivo nuevo o modificado de Git.

## Verificación de criterios de aceptación — 2026-11-03

| # | Estado | Evidencia |
| ---: | --- | --- |
| 1 | VERIFICADO | `Room.cs:9,679-691,732-737` fija y genera el mazo de 98 cartas; `RoomTests.cs:382-403` contrasta los IDs esperados `TEMP-001`–`TEMP-098`. |
| 2 | VERIFICADO | `RoomTests.cs:1516-1569` ejecuta 35 rondas con 3–8 jugadores y comprueba reciclaje; el test enfocado pasó sus 6 casos. |
| 3 | VERIFICADO | `Room.cs:565-577,693-729` descarta cartas jugadas y recicla solo `_discardPile`; `RoomTests.cs:1483-1514,1523-1569` comprueba manos y zonas del mazo sin duplicados. |
| 4 | VERIFICADO | `public/cards/manifest.v1.json:1-3`; `scripts/card-manifest.test.mjs:13-26` valida versión, cantidad e IDs/archivos únicos; 4 tests pasaron. |
| 5 | VERIFICADO | `scripts/card-manifest.test.mjs:28-37` compara ordinales de ID y archivo; el test pasó. |
| 6 | VERIFICADO | Auditoría local de las 98 entradas: `alt` coincide con el nombre descriptivo sin ordinal ni extensión (98 coincidencias, 0 discrepancias); el test de `scripts/card-manifest.test.mjs:40-45` valida que no esté vacío. |
| 7 | VERIFICADO | `vite.config.ts:8-15,33-40,101-119` sirve desde `docs/assets/cartas/`; se encontraron 98 JPG ignorados y un GET local devolvió `200`, `image/jpeg`, 877001 bytes. |
| 8 | VERIFICADO | `vite.config.ts:14-16,43-76,82-99` aplica lista permitida y verifica ruta real; Vite devolvió `404` para nombre no listado, traversal codificado/literal y ruta raíz. |
| 9 | VERIFICADO | `cardAssets.ts:70-82,96-132` convierte un HEAD 404 en `Ilustración no disponible` e `isSelectable: false`; `MobileNarratorTurnScreen.tsx:26-30,90-103,117-155,182-184` lo refleja en una radio deshabilitada, la etiqueta `No disponible` y la validación del envío. Playwright MCP en `http://localhost:5176/join/ABC234`, viewport 390×844, con snapshot privado y SignalR simulados e interceptación HEAD 404 para `TEMP-001`: la radio accesible quedó deshabilitada, el texto de disponibilidad fue visible y `Enviar carta y pista` siguió deshabilitado con pista válida y ninguna selección. El intento de selección expiró sobre el control deshabilitado; no hubo solicitud `storyteller-card`. `TEMP-002` respondió 200 y habilitó el envío al seleccionarla. Screenshots: `.playwright-mcp/09-local-card-catalog/spec09-card-unavailable-unselected-390x844.png` y `.playwright-mcp/09-local-card-catalog/spec09-card-unavailable-390x844.png`. |
| 10 | VERIFICADO | `package.json:9-15`; `test:cards` usa `node:test`. Los cuatro comandos de cliente indicados abajo terminaron correctamente; no se requiere dependencia nueva para esa prueba. |
| 11 | VERIFICADO | `dotnet build ImaginArte.sln` terminó con 0 advertencias/errores. El 2026-11-03, con Docker disponible, `dotnet test ImaginArte.sln --no-restore -m:1` pasó 336 tests, incluidos los 24 de infraestructura. |
| 12 | VERIFICADO | `npm run build` completó `vite build`; `dist` contiene el manifiesto, no contiene JPG y no incluye `__local-cards`. `vite.config.ts:33-40` limita el middleware a `apply: 'serve'`. |
| 13 | VERIFICADO | `.gitignore:21` ignora `assets/`; `git check-ignore` lo confirmó. Sin diff de `.gitignore`, sin estado Git para `docs/assets/cartas/` y cero JPG rastreados. |

### Comprobaciones ejecutadas

- Desde `src/ImaginArte.Client.React`: `npm run test:cards && npm run typecheck && npm run lint && npm run build` — correcto; pasaron los 4 tests de manifiesto, typecheck, lint y build.
- Desde la raíz: `dotnet restore ImaginArte.sln` — correcto; `dotnet build ImaginArte.sln` — correcto, 0 advertencias/errores; `dotnet test tests/ImaginArte.Domain.Tests/ImaginArte.Domain.Tests.csproj --no-restore --filter FullyQualifiedName~AdvanceRound_RecyclesDiscardForLongGamesWithoutDuplicatingCards` — 6 pasaron; `dotnet test tests/ImaginArte.Domain.Tests/ImaginArte.Domain.Tests.csproj --no-restore -m:1` — 108 pasaron; `dotnet test ImaginArte.sln --no-restore -m:1` — 336 pasaron al repetirse con Docker disponible el 2026-11-03.
- Vite: `curl -I http://127.0.0.1:5173/__local-cards/01-relicario-botella-navegante.jpg` devolvió `200`; `curl -I http://127.0.0.1:5173/__local-cards/not-listed.jpg` devolvió `404`.
- Auditoría de `alt` con Node: 98 entradas, 98 coincidencias. Tras `npm run build`, `find src/ImaginArte.Client.React/dist -type f -iname '*.jpg'` y la búsqueda de `__local-cards` en `dist` no encontraron resultados; el manifiesto sí está presente.
- Playwright MCP: en `http://localhost:5176/join/ABC234`, 390×844, se simuló un snapshot privado con `TEMP-001` y `TEMP-002`, conexión SignalR y un HEAD 404 para la imagen de `TEMP-001`. La captura de accesibilidad mostró la radio `Carta 1: Relicario botella navegante` deshabilitada, `Ilustración no disponible`, `No disponible` y el botón de envío deshabilitado sin selección. `locator.check()` expiró al intentar seleccionar la radio; la red no registró una solicitud `storyteller-card`. La radio de `TEMP-002` permaneció disponible y, al seleccionarla con una pista válida, el botón se habilitó. Capturas inspeccionadas visualmente: `.playwright-mcp/09-local-card-catalog/spec09-card-unavailable-unselected-390x844.png` y `.playwright-mcp/09-local-card-catalog/spec09-card-unavailable-390x844.png`.

**Bloqueo de entorno resuelto:** la ejecución anterior no pudo acceder a Docker para los tests de PostgreSQL; la repetición con Docker disponible completó la suite correctamente.

## Decisiones

- **Sí:** usar los IDs temporales `TEMP-001`–`TEMP-098`. La cantidad coincide con las ilustraciones locales disponibles y no se inventan cartas sin imagen.
- **Sí:** reciclar y barajar cartas descartadas cuando el mazo no alcance. Así una partida larga no falla al reponer manos y las cartas que siguen en juego no se duplican.
- **Sí:** usar un manifiesto JSON con versión y nombres de archivo. Se puede trasladar a un origen Cloudflare sin cambiar `CardId`.
- **Sí:** derivar `alt` del nombre descriptivo del archivo. La pantalla puede añadir además la posición de la carta en la mano.
- **Sí:** servir los JPG ignorados solo con Vite en desarrollo. Se respeta la decisión de no subirlos a Git y Cloudflare queda pendiente.
- **Sí:** deshabilitar una carta si falta su archivo local, en lugar de aceptar una selección que no puede identificarse visualmente.
- **No:** mantener los IDs `TEMP-099`–`TEMP-120` con imágenes de respaldo. No hay ilustraciones disponibles para esos IDs.
- **No:** subir imágenes, configurar Cloudflare o hacer del manifiesto una nueva fuente de verdad del dominio.
- **No:** modificar `.gitignore`, incluir imágenes en el build ni añadir dependencias de pruebas.

## Riesgos

| Riesgo | Mitigación |
| --- | --- |
| Los JPG están ignorados y no estarán en un clon limpio ni en producción antes de Cloudflare. | Mantener el origen local solo para desarrollo, informar cuando falte una imagen y documentar que la publicación de activos es una spec posterior. |
| El reciclaje cambia el ciclo del mazo y puede reintroducir una imagen ya jugada en una ronda anterior. | Reciclar solo el descarte, barajarlo y probar reposiciones largas con IDs únicos en manos activas. |
| El nombre del archivo no describe por completo la ilustración. | Mantener el `alt` explícito en el manifiesto para poder corregir cada descripción sin cambiar los IDs. |

## Referencias

- `docs/01-game-rules.md`
- `docs/06-design-system.md`
- `docs/09-react-tutoring-guide.md`
- `README.md`
- `docs/assets/cartas/` (98 imágenes locales ignoradas)
- `src/ImaginArte.Domain/Room.cs`
- `src/ImaginArte.Domain/CardId.cs`
- `src/ImaginArte.Client.React/vite.config.ts`
- `src/ImaginArte.Client.React/src/models/roomState.ts`

## Qué **no** incluye esta spec

- Implementar la pantalla del turno del narrador; se especifica en SPEC 10.
- Publicar imágenes en R2/CDN, cambiar a IDs permanentes o crear un catálogo de producción.
- Versionar los JPG, cambiar `.gitignore` o almacenar imágenes en la base de datos.
- Cambiar otras reglas, contratos, pantallas móviles, TV o cliente Blazor.
