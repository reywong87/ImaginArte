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

- [ ] El dominio genera exactamente 98 IDs temporales, desde `TEMP-001` hasta `TEMP-098`, y no genera IDs superiores.
- [ ] Con hasta ocho jugadores, el dominio puede reponer manos durante más de seis rondas al reciclar y barajar el descarte cuando el mazo disponible es insuficiente.
- [ ] El reciclaje solo mueve cartas del descarte al mazo y no crea IDs duplicados en las manos activas.
- [ ] El manifiesto tiene `schemaVersion: 1` y exactamente 98 entradas únicas.
- [ ] Para cada entrada, el número de `TEMP-NNN` coincide con el prefijo numérico del archivo asociado.
- [ ] Cada entrada incluye un `alt` legible derivado del nombre descriptivo del archivo, sin prefijo numérico ni extensión.
- [ ] El desarrollo local puede servir las imágenes existentes desde `docs/assets/cartas/` sin copiar ni versionar los JPG.
- [ ] El servidor local solo sirve archivos nombrados en el manifiesto y rechaza rutas que salgan de `docs/assets/cartas/`.
- [ ] Si falta un JPG asociado, la interfaz puede identificar la carta como no disponible y no permite seleccionarla.
- [ ] `npm run test:cards`, `npm run typecheck`, `npm run lint` y `npm run build` terminan correctamente sin nuevas dependencias.
- [ ] `dotnet build ImaginArte.sln` y `dotnet test ImaginArte.sln --no-restore -m:1` terminan correctamente.
- [ ] `vite build` no copia los JPG ignorados al resultado de producción ni incluye el middleware local de desarrollo.
- [ ] No se modifica `.gitignore` y ningún JPG de `docs/assets/cartas/` aparece como archivo nuevo o modificado de Git.

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
