# SPEC 16 — Votación de cartas en móvil

> **Estado:** Approved  
> **Depende de:** SPEC 09 — Catálogo local de cartas; SPEC 14 — Espera móvil tras enviar una carta; SPEC 15 — Votación de cartas en TV  
> **Fecha:** 2026-11-19  
> **Objetivo:** Permitir que el jugador vote en privado desde el móvil y recupere el estado de su voto sin revelar qué carta eligió.

## Alcance

**Incluye:**

- Durante `Voting`, mostrar al jugador que aún no votó la pista y las cartas en orden ascendente de `displayNumber`, con los mismos números que en TV. Mostrar su carta visible pero deshabilitada, con la etiqueta «Tu carta» y una explicación. El narrador permanece en espera, sin controles de voto.
- Usar `docs/assets/visual-references/mockups/14-votar-carta-movil.png` como referencia para el marco oscuro, la pista visible y la cuadrícula desplazable de dos columnas. Si hay un número impar de cartas, la última queda en la columna izquierda. Mantener el arte en proporción `2:3`, con números y etiquetas fuera de la ilustración.
- Mantener el botón de confirmación en una franja inferior que respete el área segura sin tapar las cartas. Mostrar la selección con texto y contorno, no solo color. Permitir cambiarla antes de confirmar.
- Enviar el voto al servidor solo al confirmar. Bloquear envíos simultáneos y, tras confirmarse, retirar la cuadrícula y mostrar «Voto enviado», sin indicar el destino.
- Si se pierde la conexión o la respuesta del envío, consultar el estado privado actualizado sin reenviar el voto. Si el servidor confirma que no hubo voto y la fase sigue abierta, permitir un nuevo intento explícito. Si la fase cerró sin confirmación de voto, mostrar una espera neutral.

**Fuera de alcance:**

- Diseño completo del mockup 15, revelación y resultados.
- Cambios de comportamiento en TV o Blazor, nuevas reglas, endpoints, persistencia o dependencias.

## Modelo de datos

- El servidor añade `HasVoted` a la respuesta privada. Lo obtiene del voto registrado en la ronda actual, sin añadir un campo persistente. El indicador no se incorpora al snapshot público ni incluye el destino del voto.
- React lee `hasVoted`, `isCurrentStoryteller`, `submittedVotingCardId`, `room.phase`, `room.clue` y `room.votingCards`. Usa `votingCardId` para identificar cada opción y la carta propia, `cardId` para resolver la ilustración y `displayNumber` para ordenar y numerar.
- La selección sin confirmar y el estado de envío son temporales. No se conservan entre sesiones ni se usan como prueba de que el servidor registró el voto.

## Plan de implementación

1. Añadir `HasVoted` a `src/ImaginArte.Application/Rooms/GetPlayerPrivateState.cs` y `src/ImaginArte.Contracts/Rooms/RoomHttpContracts.cs`. Mapearlo en `src/ImaginArte.Server/RoomHttpMappings.cs`. Añadir cobertura en las suites de Application y Server para el estado antes y después de votar, el reinicio entre rondas y la ausencia del indicador en el estado público.
2. Actualizar `src/ImaginArte.Client.React/src/models/roomState.ts` para leer el indicador privado y `src/ImaginArte.Client.React/src/services/roomsApi.ts` para usar el comando existente `POST /rooms/{roomCode}/votes`.
3. Crear `src/ImaginArte.Client.React/src/screens/mobile/MobileVotingScreen.tsx` y `MobileVotingScreen.css` en el mismo directorio. Construir la vista con datos de entrada y acciones de selección y confirmación. Reutilizar `src/ImaginArte.Client.React/src/services/cardAssets.ts` para las imágenes.
4. Integrar la vista en `src/ImaginArte.Client.React/src/screens/mobile/MobileJoinScreen.tsx`, distinguiendo votación, confirmación básica y espera neutral. Reutilizar la conexión móvil y la recuperación por snapshot existentes. Actualizar `docs/08-screen-specifications.md` con la composición acordada y la transición provisional tras votar.

## Tutoría React

- **Objetivo observable:** elegir una carta en el móvil, confirmar el voto y recuperar «Voto enviado» tras recargar, sin mostrar cuál fue la carta.
- **Analogía con Blazor:** `src/ImaginArte.Client.Blazor/Pages/JoinRoom.razor` usa un campo local para recordar que votó durante la vida del componente. En React, el estado local también sirve para recordar la selección mientras la vista está abierta, pero no prueba que el voto quedó registrado. React vuelve a ejecutar la vista cuando cambia su estado, sin llamar a `StateHasChanged`.
- **Flujo:** snapshot privado → estado de `MobileJoinScreen` → pantalla. Selección local → confirmación explícita → comando HTTP → resultado. Conexión o reconexión → snapshot nuevo → pantalla vigente, sin reproducir eventos perdidos ni repetir el comando.
- Explicar en español qué ocurre en el primer renderizado, al seleccionar y confirmar, al recuperar la conexión y al salir de la pantalla. Cerrar con una comprobación reproducible y un resumen de la diferencia entre selección local y voto registrado.

## Criterios de aceptación

- [ ] En una sala real, móvil y TV coinciden en orden y números. Solo el propietario ve su carta marcada «Tu carta» y no puede seleccionarla. El narrador no ve controles de voto.
- [ ] Seleccionar y cambiar de carta no envía ningún comando. Confirmar emite un único `POST` aunque se pulse repetidamente durante el envío.
- [ ] Tras el éxito, incluido el último voto de la ronda, se muestra «Voto enviado» sin cuadrícula, número ni miniatura del destino.
- [ ] Recargar tras votar, reconectar o perder la respuesta de un voto registrado recupera «Voto enviado» sin repetir el `POST`. Si no se registró y la fase sigue abierta, se permite reintentar explícitamente. Si ya cerró, no se ofrece votar ni se inventa una confirmación.
- [ ] `HasVoted` se limita al jugador y la ronda actuales. El snapshot público no lo incluye y, durante `Voting`, no expone votos individuales. La pantalla móvil no muestra votos ajenos ni identifica la carta del narrador.
- [ ] A 390 × 844 y 360 × 568, con hasta ocho cartas, no hay desbordamiento horizontal ni solapes. La última carta impar queda a la izquierda. Pista y confirmación siguen accesibles al desplazar la cuadrícula.
- [ ] La selección y la confirmación funcionan con teclado, tienen foco visible y ofrecen objetivos táctiles de al menos 44 px. Etiquetas y números no invaden las ilustraciones.
- [ ] Pasan las pruebas de Application y Server pertinentes y `npm run typecheck`, `npm run lint` y `npm run build` desde `src/ImaginArte.Client.React`. La revisión en navegador comprueba la pérdida de respuesta y la ausencia de reenvío.

## Decisiones

- **Sí:** voto real desde este incremento, con el comando HTTP existente y un indicador privado autoritativo.
- **Sí:** confirmación básica sin destino; el mockup 15 se reserva para otro incremento.
- **Sí:** dos columnas con la última carta impar a la izquierda y confirmación en una franja inferior que no tape el contenido.
- **No:** deducir si el jugador votó del conteo público, persistir la selección en el navegador o reenviar automáticamente un voto.

## Riesgos

| Riesgo | Mitigación |
| --- | --- |
| El servidor registra el voto pero se pierde la respuesta. | Consultar el snapshot privado antes de volver a ofrecer un envío. |
| El último voto cambia la fase mientras llega la confirmación. | Usar la confirmación del servidor para mostrar la espera sin destino; no deducirla solo del cambio de fase. |
| La franja inferior tapa cartas en pantallas pequeñas. | Separarla del área desplazable y verificar ambos tamaños con el máximo de cartas. |

## Qué **no** incluye esta spec

- Diseño completo de la espera tras votar, revelación o resultados personales.
- Cambios en TV o Blazor, nuevas reglas, endpoints, persistencia o dependencias.
