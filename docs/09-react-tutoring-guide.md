# SPEC 09 — Tutoría de React a partir de Blazor

> **Estado:** Approved
> **Depende de:** `docs/02-technical-architecture.md`, `docs/05-ai-development-workflow.md`, `docs/06-design-system.md`, `docs/07-progress-board.md`, `docs/08-screen-specifications.md`
> **Fecha:** 2026-09-24
> **Objetivo:** Establecer que cada trabajo de React se realice como una tutoría progresiva en español, usando Blazor como punto de partida para explicar conceptos, decisiones, código y verificaciones.

## Por qué existe esta especificación

La persona que aprende este proyecto conoce C# y Blazor, pero no React. La migración de la interfaz no debe limitarse a producir código que funcione: cada slice (incremento funcional pequeño) debe servir también para construir un modelo mental de React.

Las explicaciones deben partir de elementos reales del repositorio, como:

- `src/ImaginArte.Client.Blazor/Pages/JoinRoom.razor` para componentes, parámetros, formularios, eventos y renderizado condicional.
- `src/ImaginArte.Client.Blazor/Pages/TvLobby.razor` para listas, claves, estados de pantalla y presentación pública.
- `src/ImaginArte.Client.Blazor/Rooms/RoomClient.cs` para clientes de API, estado remoto y notificación de cambios.
- `src/ImaginArte.Client.Blazor/Rooms/SignalRRoomRealtimeConnection.cs` para suscripciones, reconexión y limpieza.

Una analogía ayuda a empezar, pero no debe ocultar las diferencias entre los dos modelos. Cada analogía debe indicar también dónde deja de ser válida.

## Alcance

**Incluye:**

- Tratar cada tarea de `src/ImaginArte.Client.React` como una oportunidad de aprendizaje guiado.
- Explicar en español qué se construye, por qué se construye así y cómo comprobarlo.
- Introducir un concepto nuevo de React, JavaScript o TypeScript solamente cuando el slice lo necesite.
- Relacionar los conceptos nuevos con patrones Blazor existentes y señalar las diferencias importantes.
- Avanzar mediante incrementos pequeños y ejecutables, sin presentar bloques de código inexplicados.
- Explicar el flujo completo de una pantalla: estado recibido, renderizado, intención del usuario, comando HTTP, evento SignalR y recuperación de un snapshot (estado actual devuelto por el servidor).
- Cerrar cada slice con una recapitulación, un glosario breve y una verificación reproducible.
- Mantener las reglas de dominio, la privacidad TV/móvil y los contratos HTTP/SignalR definidos por las especificaciones existentes.

**Fuera de alcance:**

- Implementar una pantalla, componente, servicio o prueba de React por el mero hecho de crear esta guía.
- Reemplazar, eliminar o reescribir el cliente Blazor.
- Convertir esta especificación en un curso completo de JavaScript, TypeScript o React independiente del proyecto.
- Traducir componentes Blazor línea por línea sin revisar el modelo mental de React.
- Añadir una librería, un patrón de estado global o una abstracción para que React imite artificialmente a Blazor.
- Mover puntuación, transiciones de fase, rotación del narrador, propiedad de cartas o decisiones de privacidad al cliente React.

## Contrato de tutoría

Toda explicación o implementación futura de React debe seguir estas reglas:

1. **Empezar por el objetivo.** Explicar en lenguaje cotidiano qué verá o podrá hacer la persona cuando termine el slice.
2. **Declarar los conceptos nuevos.** Enumerar los términos que no se han usado antes y definirlos antes de utilizarlos.
3. **Usar una analogía concreta.** Nombrar el archivo o patrón Blazor de referencia, explicar la correspondencia y describir la diferencia.
4. **Explicar el tiempo de ejecución.** Indicar qué ocurre durante el primer renderizado, ante una interacción, cuando llega un snapshot y cuando se desmonta el componente.
5. **Separar sintaxis, comportamiento y arquitectura.** JSX, `useState` y `useEffect` son mecanismos distintos; ninguno decide las reglas del juego.
6. **Trabajar en pasos pequeños.** Cada paso debe dejar la aplicación compilable o ejecutable y debe decir qué se comprueba.
7. **Explicar antes y después del cambio.** Antes de editar, listar los archivos y su responsabilidad. Después, describir el flujo de datos y las decisiones tomadas.
8. **No ocultar errores.** Si una comparación con Blazor es incompleta, una API es nueva o una decisión está pendiente, debe decirse explícitamente.
9. **Cerrar con aprendizaje.** Resumir qué concepto se ha aprendido, qué parte equivale a Blazor, qué parte no y qué experimento corto puede probarse a continuación.

El idioma de trabajo será el español. Los nombres de APIs y palabras clave se conservarán en inglés, seguidos de una explicación en español la primera vez que aparezcan.

## Mapa de analogías Blazor → React

Las siguientes equivalencias sirven como punto de entrada, no como traducción exacta.

| Blazor en este proyecto | React | Analogía útil | Diferencia que debe explicarse |
| --- | --- | --- | --- |
| Un archivo `.razor` con markup y bloque `@code` | Una función componente en `.tsx` que devuelve JSX | Ambos describen una parte reutilizable de la interfaz | React suele expresar el componente como función que se vuelve a ejecutar; no se traduce `@code` como una clase con ciclo de vida equivalente |
| `@room.RoomCode` | `{room.roomCode}` dentro de JSX | Ambas sintaxis insertan un valor en la vista | JSX usa expresiones JavaScript entre llaves y sus nombres de atributos siguen convenciones distintas de HTML/Razor |
| `[Parameter] public string RoomCode` | `props.roomCode` o un parámetro desestructurado | El padre entrega datos al hijo | Las props son de solo lectura; el hijo comunica una intención mediante una función, no modifica la prop |
| Campos privados y `StateHasChanged()` | `useState` y su función setter | Ambos conservan estado de presentación y provocan una actualización visible | En React el setter solicita el nuevo render; no se debe mutar directamente un objeto o array ni esperar un `StateHasChanged` manual |
| `@onclick="HandleAsync"` | `onClick={handleClick}` | Ambos conectan una interacción con una función | JSX usa nombres como `onClick`; la función recibe un evento del navegador y un `async` no convierte por sí solo la operación en una mutación autorizada |
| `@bind-Value` | `value` más `onChange` | El campo muestra un valor y notifica cuando cambia | React no ofrece el mismo binding y sistema de validación que `EditForm`; el estado, la validación y el envío se hacen explícitos |
| `@if`, `else if` y `@foreach` | Expresiones condicionales y `array.map(...)` | La salida depende del estado y de una colección | `map` devuelve elementos JSX y cada elemento necesita una `key` estable; no se debe usar el índice si la identidad real ya existe |
| `@key="token.PlayerId"` | `key={token.playerId}` | La clave ayuda al renderer a conservar la identidad de un elemento | `key` es una instrucción para React y no una prop que el componente hijo pueda leer normalmente |
| `OnInitializedAsync` y `OnParametersSetAsync` | A menudo un `useEffect` o una carga explícita | Ambos permiten reaccionar a datos iniciales o cambios de entrada | `useEffect` sirve para sincronizar con sistemas externos; no es un reemplazo general de todos los métodos de ciclo de vida ni debe usarse para calcular valores derivados |
| `IAsyncDisposable` y desuscripción de eventos | Función de limpieza devuelta por `useEffect` | Ambos liberan suscripciones, conexiones o recursos | La limpieza debe corresponder a la suscripción creada por ese efecto; hay que evitar conexiones duplicadas en cada renderizado |
| `[Inject]` y el contenedor de DI de Blazor | Imports de módulos, composición de servicios, contexto o un custom hook (hook personalizado) | La UI consume una dependencia sin implementar toda la integración | React no incorpora el contenedor de DI de ASP.NET; la solución concreta debe elegirse por slice y no inventar un singleton mutable para imitarlo |
| `RoomClient` y su evento `StateChanged` | Módulo/cliente de API más estado React y una suscripción controlada | Un cliente coordina comunicación y la UI muestra su estado | Una suscripción externa debe actualizar estado React y limpiarse; el componente no debe convertirse en la fuente de verdad del dominio |
| `HttpClient` y `PostAsJsonAsync` | Cliente HTTP basado en `fetch` o el adaptador aprobado por el slice | Ambos envían comandos al backend y reciben DTOs | El contrato sigue siendo el del servidor; cambiar de cliente no autoriza a calcular localmente puntuación o fases |
| `IRoomRealtimeConnection` y `OnRoomUpdated` | Suscripción a eventos SignalR desde un efecto o adaptador | Ambos reciben cambios publicados por el servidor | Tras conectar o reconectar se solicita un snapshot nuevo; no se intenta reconstruir el estado reproduciendo eventos perdidos |
| `@page` y `NavigationManager` | URL y la solución de routing que se apruebe para React | Ambos relacionan una URL con una pantalla | React no tiene automáticamente las rutas Razor; no se debe asumir React Router si el slice no lo necesita o no lo ha aprobado |
| `ElementReference` | `useRef` | Ambos permiten conservar una referencia a un elemento del DOM | Una ref no provoca un renderizado al cambiar y no debe usarse para almacenar el estado visible |
| `RenderFragment` o contenido hijo | `children` | Ambos permiten componer un componente alrededor de contenido recibido | JSX trata `children` como una prop y la composición no debe ocultar qué datos necesita el componente |

## Fundamentos de JavaScript y TypeScript que se explicarán cuando aparezcan

No se presupone experiencia previa en JavaScript. Cuando un slice lo requiera, se explicará solamente el mínimo necesario:

| Concepto | Puente desde C#/Blazor | Precaución |
| --- | --- | --- |
| `const` y `let` | Variables locales y referencias | `const` impide reasignar la referencia, no vuelve inmutable el objeto apuntado |
| Objetos, arrays y desestructuración | DTOs, propiedades y asignación de valores | Actualizar estado normalmente implica crear un nuevo objeto o array, no mutar el anterior |
| `null` y `undefined` | Valores ausentes o anulables | No son siempre intercambiables; la explicación debe indicar cuál usa el contrato y cuál produce el navegador |
| Funciones flecha y callbacks | Delegados y lambdas | Una función puede pasarse como dato y ejecutarse más tarde; esto es central para props de eventos |
| `map`, `filter` y `find` | LINQ de uso frecuente | Devuelven nuevos valores; no son comandos del backend ni sustituyen una regla de dominio |
| `Promise` y `async/await` | `Task` y `await` | La ejecución ocurre en el navegador y los errores deben representarse en el estado de la interfaz |
| `import` y `export` | `using` más referencias entre proyectos | Los módulos JavaScript se resuelven en el build; no son inyección de dependencias de ASP.NET |
| `type` e `interface` de TypeScript | Tipos y contratos de C# | TypeScript ayuda durante el build, pero sus tipos no validan por sí solos el JSON recibido en tiempo de ejecución |
| JSX | Markup Razor con expresiones embebidas | JSX es sintaxis JavaScript transformada por el build; no es HTML que el navegador entienda directamente |

## Arquitectura que debe aprenderse junto con React

Las analogías nunca deben romper las fronteras del proyecto:

- Un componente React **renderiza estado y recoge intenciones**. No calcula puntuaciones, decide fases legales ni rota narradores.
- Las mutaciones autorizadas usan comandos HTTP. SignalR entrega cambios en tiempo real, pero no es la fuente de verdad.
- Después de una conexión, reconexión o recuperación, el cliente solicita un snapshot público o privado nuevo.
- La vista TV pública y el controlador móvil privado usan modelos y pantallas separados. Las manos, la propiedad de cartas, las identidades pendientes y los votos individuales nunca se muestran donde `docs/08-screen-specifications.md` los prohíbe.
- La analogía correcta para `RoomClient` es un adaptador de comunicación o una fuente de estado controlada, no una copia de `GameRoom` dentro de React.
- La explicación debe distinguir estado local de interfaz —por ejemplo, una selección aún no confirmada— y estado autoritativo recibido del servidor.

## Orden de aprendizaje recomendado

Cada etapa debe apoyarse en la anterior y puede convertirse en un slice independiente:

1. **Componente y JSX.** Sustituir un placeholder estático y comparar una función `.tsx` con un `.razor` sencillo.
2. **Props y composición.** Pasar datos y callbacks desde un componente padre, relacionándolo con `[Parameter]` y contenido hijo.
3. **Estado y eventos.** Implementar una interacción local y compararla con un campo privado más `StateHasChanged`.
4. **Formularios controlados.** Construir una entrada reversible y contrastarla con `EditForm`, `@bind-Value` y validación explícita.
5. **Renderizado condicional y listas.** Mostrar estados de carga, error y contenido; usar identidades estables y accesibilidad.
6. **HTTP asíncrono.** Cargar un snapshot, representar carga/error/éxito y enviar un comando sin duplicarlo.
7. **SignalR y limpieza.** Suscribirse a eventos, desmontar correctamente y recuperar un snapshot tras reconectar.
8. **Pantallas TV y móvil.** Aplicar los límites públicos/privados de las especificaciones de pantalla.
9. **Movimiento y pruebas de navegador.** Introducir Motion o Playwright únicamente cuando el slice correspondiente lo requiera y después de explicar su propósito.

No se saltará directamente a contextos, stores globales, memoización, optimización o abstracciones avanzadas si el slice puede resolverse con componentes, props, estado local y adaptadores simples.

## Proceso obligatorio para cada tarea React

La respuesta o plan de cada tarea debe seguir esta secuencia:

1. **Contexto.** Citar las especificaciones que gobiernan el comportamiento y señalar el equivalente Blazor existente.
2. **Objetivo observable.** Describir qué verá la persona en el navegador y qué no se modificará.
3. **Conceptos nuevos.** Definir las APIs, sintaxis o decisiones de arquitectura que se van a introducir.
4. **Modelo mental.** Mostrar, con palabras o un diagrama corto, el flujo `snapshot → estado → renderizado` y `intención → comando → nuevo estado/evento`.
5. **Plan de archivos.** Nombrar cada archivo que se creará o cambiará y explicar su responsabilidad.
6. **Implementación incremental.** Realizar el cambio más pequeño que deje la aplicación en un estado ejecutable; después explicar las líneas relevantes.
7. **Verificación.** Ejecutar los comandos disponibles del cliente, como `npm run typecheck`, `npm run lint` y `npm run build`, y explicar qué demuestra cada uno. Para comportamiento de navegador, usar pruebas accesibles cuando el slice ya tenga suite Playwright.
8. **Cierre didáctico.** Resumir la analogía con Blazor, la diferencia principal, los errores evitados y un pequeño ejercicio o pregunta de comprobación opcional.

La plantilla de comunicación recomendada es:

```text
## Objetivo
## Conceptos nuevos
## Analogía con Blazor
## Diferencia importante
## Archivos y pasos
## Verificación
## Lo aprendido
```

La plantilla orienta la explicación; no obliga a crear un archivo de apuntes por cada tarea.

## Ejemplo de explicación esperada

Ante el bloque Blazor de `JoinRoom.razor` que decide entre formulario y lobby:

```razor
@if (_lobby is null)
{
    <JoinForm />
}
else
{
    <LobbyPanel />
}
```

La explicación React debe decir algo equivalente a:

```tsx
return lobby === null ? <JoinForm /> : <LobbyPanel />;
```

Y debe aclarar que:

- ambas versiones eligen qué interfaz mostrar a partir del estado;
- React vuelve a ejecutar el componente cuando se actualiza el estado mediante su setter;
- no se necesita llamar manualmente a `StateHasChanged` para ese estado local;
- la selección de la pantalla no decide si una acción de juego es legal; esa decisión continúa en el backend.

Este ejemplo es ilustrativo. Las implementaciones reales deben respetar los contratos, la privacidad y la composición definida por el slice correspondiente.

## Modelo de datos

Esta especificación no introduce datos de juego, persistencia, endpoints ni eventos nuevos. Define un protocolo de explicación y aprendizaje. Los modelos React deben seguir siendo representaciones TypeScript de los contratos existentes y no copias de las entidades del dominio.

## Plan de aplicación

1. Añadir esta guía como `docs/09-react-tutoring-guide.md` sin modificar código de aplicación.
2. Antes de cada slice React, leer esta guía y las especificaciones de producto, arquitectura, diseño y pantalla que correspondan.
3. Seleccionar un único concepto principal y un ejemplo Blazor concreto que permita explicarlo.
4. Dividir la implementación en pasos ejecutables, documentando archivos, flujo de datos y verificación.
5. Revisar que el componente React solo represente estado y emita intenciones, y que los límites TV/móvil se mantengan.
6. Cerrar la tarea con pruebas, resumen didáctico y diferencias que la persona debe recordar.

**Archivo previsto para esta especificación:**

- Nuevo: `docs/09-react-tutoring-guide.md`.
- Sin cambios: Blazor, backend, contratos, dominio, infraestructura y configuración de la solución .NET.

## Criterios de aceptación

- [ ] Existe `docs/09-react-tutoring-guide.md` como documento numerado `09`.
- [ ] El documento establece explícitamente que el trabajo React debe realizarse como tutoría para una persona sin conocimientos previos de React.
- [ ] El documento exige explicaciones en español y conserva los términos técnicos React/JavaScript/TypeScript con una definición comprensible.
- [ ] Incluye analogías con Blazor basadas en archivos concretos del repositorio.
- [ ] La tabla de analogías identifica tanto la correspondencia como el límite de cada comparación.
- [ ] Se explican, cuando sean necesarios, los fundamentos de JSX, props, estado, eventos, formularios, listas, efectos, limpieza, HTTP y SignalR.
- [ ] El proceso obliga a explicar objetivo, conceptos, archivos, pasos, verificación y aprendizaje final.
- [ ] Se mantiene la regla de que React no es la fuente de verdad de las reglas del juego.
- [ ] Se mantienen la recuperación por snapshot tras reconectar y la separación de información TV/móvil.
- [ ] La especificación no requiere cambios de código, dependencias, endpoints ni persistencia.

## Decisiones

- **Sí:** usar Blazor y C# como punto de partida, porque es el conocimiento que ya existe en el proyecto y en la persona que aprende.
- **Sí:** explicar en español y conservar los nombres oficiales de las APIs en inglés, para que la documentación y los mensajes de error sigan siendo localizables.
- **Sí:** acompañar cada analogía con su diferencia. React no es una sintaxis alternativa de Razor.
- **Sí:** aprender mediante slices verticales pequeños y funcionales, en lugar de estudiar React completo antes de construir.
- **Sí:** usar la documentación oficial de React como referencia complementaria, pero traducir sus conceptos al contexto de ImaginArte.
- **Sí:** explicar `useEffect` principalmente como sincronización con sistemas externos y limpieza, no como un sustituto automático de todos los ciclos de vida de Blazor.
- **No:** añadir una capa de compatibilidad que haga que React parezca Blazor; eso escondería los conceptos que se quieren aprender.
- **No:** introducir un store global, un router u otra librería solo para seguir un patrón conocido de Blazor.
- **No:** reescribir el cliente Blazor ni convertir esta guía en una orden de migración completa.
- **No:** colocar reglas de negocio en componentes React para que el ejemplo resulte más corto.

## Riesgos

| Riesgo | Mitigación |
| --- | --- |
| Una analogía demasiado literal provoca que se use React como si fuera Blazor | Mostrar siempre la diferencia operativa y un ejemplo React nativo |
| La cantidad de conceptos nuevos abruma | Introducir un concepto principal por slice y explicar JavaScript/TypeScript solo cuando sea necesario |
| Se aprende sintaxis pero no el flujo de datos | Dibujar el flujo snapshot → estado → render y separar comandos de eventos |
| Un ejemplo de UI termina duplicando reglas del dominio | Revisar cada componente contra `docs/01-game-rules.md`, la arquitectura y los contratos |
| Una suscripción realtime queda activa después del desmontaje | Explicar y verificar la limpieza de efectos y la recuperación por snapshot |
| Las referencias visuales inducen a mostrar información privada | Validar siempre contra `docs/08-screen-specifications.md`, no contra el mockup aislado |
| La documentación de React cambia | Consultar la documentación oficial vigente cuando se introduzca una API nueva y registrar la versión o recomendación relevante en el slice |

## Referencias de aprendizaje

- `docs/02-technical-architecture.md`
- `docs/05-ai-development-workflow.md`
- `docs/06-design-system.md`
- `docs/07-progress-board.md`
- `docs/08-screen-specifications.md`
- `src/ImaginArte.Client.Blazor/Pages/JoinRoom.razor`
- `src/ImaginArte.Client.Blazor/Pages/TvLobby.razor`
- `src/ImaginArte.Client.Blazor/Rooms/RoomClient.cs`
- `src/ImaginArte.Client.Blazor/Rooms/SignalRRoomRealtimeConnection.cs`
- Documentación oficial de aprendizaje de React: <https://react.dev/learn>

## Qué **no** incluye esta especificación

- Una implementación de React.
- La migración o eliminación de Blazor.
- Un curso independiente completo de JavaScript, TypeScript o React.
- Nuevos endpoints, contratos, modelos de dominio, persistencia o dependencias.
- Reglas de juego dentro de componentes, hooks o servicios de la interfaz.
- Una traducción mecánica de Razor a JSX.

Los slices de implementación React deberán usar esta guía como contrato pedagógico además de sus propios criterios funcionales.
