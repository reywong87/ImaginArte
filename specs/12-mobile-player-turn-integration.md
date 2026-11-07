# SPEC 12 — Integración del turno del jugador en móvil

> **Estado:** Approved  
> **Depende de:** SPEC 09 — Catálogo local de cartas; SPEC 10 — Turno del narrador en móvil; SPEC 11 — Maqueta del turno del jugador en móvil  
> **Fecha:** 2026-11-04  
> **Objetivo:** Conectar el turno móvil del jugador al estado autoritativo y al envío existente de cartas, con una espera privada tras la confirmación.

## Alcance

**Incluye:**

- Sustituir los datos de ejemplo de SPEC 11 por la pista publicada y la mano privada del jugador obtenidas desde el servidor en `/join/{roomCode}`.
- Presentar el turno a quien no sea narrador durante `GuessersSubmitting` y aún no haya enviado carta, usando el estado autoritativo existente.
- Mantener la selección reversible hasta la acción explícita **Enviar carta** y resolver las ilustraciones con el catálogo existente.
- Enviar la carta mediante `POST /rooms/{roomCode}/guess-cards` y el contrato existente, sin conectar React directamente a la base de datos.
- Tras un envío confirmado, mostrar una espera privada sin mano ni carta enviada y evitar otro envío.
- Considerar carga, error, desconexión y recuperación mediante snapshots actuales, sin reproducir eventos perdidos ni repetir automáticamente una mutación incierta.
- Retirar el acceso de desarrollo a datos ficticios introducido en SPEC 11 cuando la pantalla real ocupe su lugar.

**Fuera de alcance:**

- Votación móvil, revelación, resultados, nuevas reglas o cambios de pantalla TV.
- Guardar borradores privados entre sesiones, publicar imágenes o modificar el cliente Blazor.
- Crear una conexión directa del navegador a la base de datos.

## Modelo de datos

Se reutilizan `GetPlayerPrivateStateResponse`, `room.clue`, `room.phase`, `player.hand`, `player.isCurrentStoryteller` y `player.hasSubmittedGuessCard`, además de `SubmitGuessCardRequest`. La selección local `selectedCardId: string | null` no representa una mutación confirmada. No se prevén nuevas estructuras persistentes.

## Plan de implementación

1. Revisar el contrato y el comportamiento efectivo del endpoint existente y de los snapshots para definir las respuestas y las condiciones de recuperación sin introducir reglas de dominio en React.
2. Conectar la pantalla de SPEC 11 a la pista y a la mano privadas recibidas por el flujo existente, conservando una selección local reversible y la presentación de imágenes ausentes.
3. Añadir la operación HTTP al cliente de salas y conectar el envío explícito con estados de operación y error accesibles.
4. Representar la confirmación y la espera sin datos privados, y resolver desconexiones y resultados inciertos consultando el estado autoritativo antes de permitir otro intento.
5. Retirar la maqueta ficticia del flujo, actualizar `docs/08-screen-specifications.md` con los comportamientos acordados y verificar los flujos móvil y TV en navegador.

### Archivos previstos

**Modificados:**

- `src/ImaginArte.Client.React/src/screens/mobile/MobilePlayerTurnScreen.tsx` y su CSS: datos reales, selección y estados de operación.
- `src/ImaginArte.Client.React/src/screens/mobile/MobileJoinScreen.tsx`: selección de la pantalla y espera a partir del estado autoritativo.
- `src/ImaginArte.Client.React/src/services/roomsApi.ts`: comando HTTP existente.
- `docs/08-screen-specifications.md`: precisión de la pantalla de jugador y su espera.

Se revisará `src/ImaginArte.Client.React/src/services/mobileRoomRealtime.ts` y se modificará solo si el flujo actual no entrega o recupera el snapshot privado necesario. No se prevén cambios en dominio, backend, contratos ni persistencia; cualquier necesidad se debe aclarar antes de ampliar el alcance.

## Tutoría React

- **Objetivo observable:** el jugador ve su pista y mano reales, confirma una carta y recibe una espera privada.
- **Conceptos:** snapshot es una fotografía del estado actual del servidor; `async/await` permite esperar una operación HTTP; una suscripción comunica actualizaciones mientras el componente está activo.
- **Analogía:** `src/ImaginArte.Client.Blazor/Rooms/RoomClient.cs` coordina datos remotos y notifica cambios. React, a diferencia de Blazor, actualiza el estado de su componente y debe limpiar cualquier suscripción propia al desmontarse.
- **Flujo:** snapshot privado → estado → pantalla → selección local → POST → actualización o snapshot autoritativo → espera. Tras reconectar se obtiene un snapshot nuevo.
- Explicar en español las responsabilidades, el primer renderizado, las interacciones, errores, recuperación, desmontaje y verificación; cerrar con recapitulación didáctica.

## Criterios de aceptación

- [ ] El jugador elegible ve la pista publicada y únicamente los IDs de su propia mano privada, resueltos por el catálogo existente.
- [ ] El narrador, los jugadores que ya enviaron y las fases distintas no reciben la pantalla de selección pendiente.
- [ ] La selección puede cambiarse antes de enviar y no provoca una petición por sí misma.
- [ ] El envío explícito usa el endpoint y contrato existentes y no admite solicitudes duplicadas mientras haya una operación en curso.
- [ ] La confirmación y espera no muestran la mano ni la carta enviada y no ofrecen otra acción de envío.
- [ ] Tras conectar o reconectar se consulta el snapshot privado actual y se representa el rol y la fase correspondientes.
- [ ] Ante resultado incierto, no se repite automáticamente el POST; se consulta el estado autoritativo antes de permitir un nuevo intento.
- [ ] La TV no revela manos, propietarios de cartas enviadas ni identidad de jugadores pendientes.
- [ ] La pantalla mantiene pista visible, selección accesible, cartas `2:3`, controles sin solapamientos y área segura en móviles.
- [ ] `npm run typecheck`, `npm run lint` y `npm run build` pasan, y la revisión en navegador cubre envío, espera, errores, recuperación y privacidad.

## Decisiones

- **Sí:** usar el backend y los contratos existentes como fuente de verdad; React solo presenta estado y envía intenciones.
- **Sí:** ofrecer espera privada posterior al envío, sin implementar votación en esta spec.
- **Sí:** separar esta integración de la maqueta de SPEC 11.
- **No:** acceder directamente a la base de datos, repetir automáticamente un envío incierto o persistir borradores privados.

## Decisiones pendientes antes de aprobar

- Confirmar si, ante un error de envío confirmado, debe conservarse la selección local, siguiendo el precedente de SPEC 10.
- Confirmar el texto y los estados exactos de la espera del jugador, incluidos los cambios posteriores a `GuessersSubmitting`.
- Confirmar cómo se comunica una pista ausente o una carta cuya ilustración no esté disponible y qué comprobaciones de errores y reconexión serán obligatorias.

## Riesgos

| Riesgo | Mitigación |
| --- | --- |
| Una respuesta perdida ocasiona un envío duplicado. | Consultar el snapshot privado antes de reintentar y no reenviar automáticamente. |
| La mano o la selección se filtran a otra superficie. | Limitar los datos al móvil propietario y verificar la TV y la espera. |
| La fase cambia durante una desconexión. | Recuperar el snapshot y representar la fase autoritativa en lugar de reproducir eventos. |

## Qué **no** incluye esta spec

- Votación, revelación, resultados personales o reglas adicionales.
- Acceso directo a la base de datos, persistencia de borradores o cambios en Blazor.
