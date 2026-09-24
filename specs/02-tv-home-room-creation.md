# SPEC 02 — Inicio TV: creación y reanudación de sala

> **Estado:** Approved
> **Depende de:** SPEC 01 — Scaffold del cliente React
> **Fecha:** 2026-09-24
> **Objetivo:** Implementar el inicio de TV en React para crear una sala, recuperar la sala de esta pestaña y entregar el flujo a `/tv/{roomCode}` hasta que el lobby real se implemente en otro spec.

## Alcance

**Incluye:**

- Implementar la pantalla de inicio TV en la ruta `/tv`, usando React y CSS; el mockup es una referencia visual, no una imagen que se renderiza como pantalla.
- Adaptar la composición para mostrar una sola acción centrada. Usar **“ImaginArte”** como título, quitar **“¿Cómo quieres comenzar?”** y omitir por completo **“Abrir una sala”**.
- Mostrar una única tarjeta/botón accesible **“Crear una sala”**, con el texto secundario del mockup **“Prepara una nueva experiencia”**.
- Copiar a `src/ImaginArte.Client.React/src/assets/` el fondo `fondo-entre-cielo-y-tierra-nuevo.png` y la textura `ventana-textura-botanica.png`. Recrear los demás elementos visuales con React y CSS según `docs/06-design-system.md`.
- Generar un código de seis caracteres con el mismo alfabeto que el cliente Blazor: `ABCDEFGHJKLMNPQRSTUVWXYZ23456789`.
- Crear la sala con el contrato actual: `POST /rooms`, enviando `{ "roomCode": "..." }`. Usar la base configurable existente `VITE_API_BASE_URL`.
- Guardar solo el código creado en `sessionStorage`, bajo la clave `imaginarte.tv.currentRoomCode`.
- Al entrar en `/tv`, si hay un código guardado, consultar `GET /rooms/{roomCode}`. Si existe, ir a `/tv/{roomCode}`; si responde 404, borrar la clave y mostrar el inicio; si falla la red o el servidor, conservar la clave y ofrecer reintentar.
- Tras crear la sala correctamente, guardar el código y navegar a `/tv/{roomCode}`.
- Implementar en `/tv/{roomCode}` un placeholder temporal que muestre **“Sala creada”** y el código. No incluir QR, roster ni controles del lobby.
- En un error HTTP 409 al crear, mostrar un error y permitir reintentar con un código nuevo. Evitar envíos duplicados mientras la petición está pendiente.
- Permitir activar la acción con teclado y presentar foco visible, estados de carga y errores accesibles.
- Fijar Vite en `localhost:5173` con `strictPort`; permitir ese origen exacto en CORS de desarrollo y actualizar el README con el flujo React/API.

**Fuera de alcance:**

- La interfaz real del lobby TV —QR, roster, jugadores conectados y controles de anfitrión—, que corresponde al siguiente spec.
- Abrir o buscar salas existentes, la entrada móvil a una sala y otras opciones de experiencia.
- Sustituir el texto “ImaginArte” por el logo final.
- Cambios en las reglas, contratos, endpoints, dominio o persistencia del backend.
- React Router, SignalR, animaciones nuevas, Playwright u otra suite de pruebas.
- La configuración de CORS de producción, que se define externamente con el origen exacto del frontend desplegado.

## Modelo de datos

No se añaden entidades de dominio ni persistencia. React consume el contrato existente de creación y lectura de sala; para este flujo solo necesita leer `roomCode` de la respuesta.

El único dato guardado localmente es:

```text
sessionStorage["imaginarte.tv.currentRoomCode"] = "<código de sala>"
```

La clave es por pestaña y solo contiene el código público, no estado del juego.

## Plan de implementación

1. Copiar los assets a `src/assets/` e implementar la pantalla estática en `TvHomeScreen.tsx` y `TvHomeScreen.css`. Integrarla en `App.tsx` y ajustar los estilos globales para que muestre el título “ImaginArte” y una única acción. Verificar que React renderiza la pantalla y que el build pasa.
2. Añadir el manejo nativo de `/tv` y `/tv/{roomCode}` en `App.tsx`, sin agregar una librería de rutas. Crear `TvLobbyPlaceholder.tsx` para mostrar la confirmación mínima y el código.
3. Configurar Vite con puerto estricto `5173`, agregar ese origen exacto a `src/ImaginArte.Server/appsettings.Development.json` y actualizar `README.md` con la ejecución conjunta de servidor y cliente.
4. Crear `src/services/roomCode.ts` y `src/services/roomsApi.ts`. Conectar el botón con `POST /rooms` y representar carga, errores y conflicto 409. Al responder correctamente, guardar el código y navegar al placeholder.
5. Añadir la recuperación al entrar en `/tv`: consultar el código guardado con `GET /rooms/{roomCode}`, navegar si existe, borrar la clave ante 404 y conservarla con una opción de reintento si falla la conexión.

## Tutoría React

- **Objetivo observable:** desde `/tv`, la persona crea una sala y llega al placeholder con el código; al regresar a `/tv`, la app recupera la sala de esa pestaña.
- **Conceptos principales:** componente funcional y JSX, estado local de carga/error con `useState`, petición inicial con `useEffect` y acción POST desde el evento del botón.
- **Analogía con Blazor:** `TvRoomCreationPresentation.cs` sirve de referencia para el código y la ruta; `RoomClient.CreateRoomAsync` para la llamada HTTP. Un setter de React provoca un nuevo render, pero no equivale exactamente a actualizar campos y llamar manualmente a `StateHasChanged`.
- La carga GET inicial sincroniza la pantalla con el servidor; el POST pertenece al evento del botón y no a un efecto. La UI representa el estado remoto y no decide reglas del juego.
- La explicación de implementación debe describir el primer render, la recuperación al volver a `/tv`, el envío del comando y los estados de error. Debe cerrar en español con una recapitulación y un glosario breve.

## Criterios de aceptación

- [ ] `/tv` muestra el fondo indicado y una sola tarjeta/botón “Crear una sala”; no muestra “Entre cielo y tierra”, “¿Cómo quieres comenzar?” ni “Abrir una sala”.
- [ ] “ImaginArte” aparece como título de texto, sin incorporar el logo final.
- [ ] Los assets de ejecución están en `src/ImaginArte.Client.React/src/assets/` y la UI se compone con React/CSS, no con el PNG del mockup.
- [ ] La acción tiene foco visible y se activa con Tab más Enter/Espacio; el foco no depende de hover o color solamente.
- [ ] Crear una sala envía un código de seis caracteres al `POST /rooms`; una respuesta correcta guarda el código en la clave acordada y navega a `/tv/{roomCode}`.
- [ ] Un 409 muestra un error y permite un nuevo intento con otro código.
- [ ] El placeholder muestra “Sala creada” y el código, sin UI de lobby.
- [ ] Al entrar de nuevo en `/tv`, un GET exitoso navega al placeholder; un 404 borra la clave y deja disponible la creación.
- [ ] Ante un error de red durante la recuperación, se conserva la clave y se ofrece reintentar sin crear automáticamente otra sala.
- [ ] El servidor de desarrollo permite solo el origen local React exacto `http://localhost:5173`; Vite falla si ese puerto está ocupado en vez de cambiar de puerto.
- [ ] `npm run typecheck`, `npm run lint` y `npm run build` finalizan correctamente.
- [ ] La prueba manual desde React y el servidor verifica creación, recuperación, 404 y error de conexión.

## Decisiones

- **Sí:** usar `/tv` y `/tv/{roomCode}`, alineado con las rutas documentadas en `README.md`.
- **Sí:** conservar el contrato de creación actual y el formato de código ya usado en Blazor.
- **Sí:** usar `sessionStorage`, guardar solo el código y consultar el servidor para recuperar la sala.
- **Sí:** usar una navegación nativa simple, sin incorporar React Router para estas dos rutas.
- **Sí:** permitir exclusivamente el origen React local exacto; producción se configura mediante variables del entorno.
- **No:** implementar ahora el lobby real ni el flujo para abrir salas existentes.
- **No:** guardar el estado de la sala en el navegador como fuente de verdad.
- **No:** instalar librerías de pruebas o animación para este slice.

## Riesgos

| Riesgo | Mitigación |
|---|---|
| El código guardado pertenece a una sala ya eliminada. | En 404 se borra la clave y se vuelve a mostrar el inicio. |
| Un fallo de red se interpreta como sala inexistente y se crea otra. | Conservar la clave y ofrecer reintento; no iniciar una creación automática. |
| Vite cambia de puerto y el origen deja de coincidir con CORS. | Fijar `5173` y usar `strictPort`. |
| El fondo compite con el texto o se recorta al adaptar el viewport. | Mantener paneles de contraste, foco y controles legibles en composición TV. |

## Qué **no** incluye este spec

- El lobby TV completo.
- La acción “Abrir una sala”.
- El cliente móvil o un flujo para unirse a una sala.
- El logo definitivo.
- Cambios de reglas, contratos o lógica de dominio.
- CORS abierto o configuración de producción.

## Referencias

- `docs/09-react-tutoring-guide.md`
- `docs/06-design-system.md`
- `docs/08-screen-specifications.md`
- `docs/assets/visual-references/mockups/01-inicio-seleccion-experiencia-tv.png`
- `docs/assets/visual-references/mockups/fondo-entre-cielo-y-tierra-nuevo.png`
- `docs/assets/ui/ventana-textura-botanica.png`
- `src/ImaginArte.Client.Blazor/Rooms/TvRoomCreationPresentation.cs`
- `src/ImaginArte.Client.Blazor/Rooms/RoomClient.cs`
- React oficial: `useState` y `useEffect` (`react.dev/reference/react/useState`, `react.dev/reference/react/useEffect`).
- Vite oficial: opciones del servidor de desarrollo (`vite.dev/config/server-options`), incluyendo `server.port` y `server.strictPort`.
