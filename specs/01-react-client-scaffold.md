# SPEC 01 — Scaffold del cliente React

> **Estado:** Approved
> **Depende de:** Ninguna
> **Fecha:** 2026-09-24
> **Objetivo:** Crear en `src/ImaginArte.Client.React` una aplicación independiente con React, TypeScript, Vite y sus herramientas base, lista para desarrollar la UI de ImaginArte.

## Alcance

**Incluye:**

- Crear el cliente en `src/ImaginArte.Client.React`, separado de los ensamblados .NET y de la solución .NET.
- Usar React, TypeScript, Vite y npm, con `package-lock.json`.
- Declarar Node `>=22.12.0` en `package.json`.
- Instalar `motion` como dependencia de ejecución.
- Configurar ESLint con formato flat, reglas recomendadas para JavaScript y TypeScript, y reglas de React Hooks.
- Crear una pantalla mínima que muestre que el cliente está listo, sin implementar pantallas del juego.
- Añadir `.env.example` con `VITE_API_BASE_URL=http://localhost:5080`.
- Añadir scripts de desarrollo, build, preview, lint y comprobación de tipos.
- Actualizar `.gitignore` y `README.md` con los comandos y la configuración local del cliente.
- Mantener intactos Blazor y los proyectos .NET existentes.

**Fuera de alcance:**

- Conectar servicios HTTP o SignalR.
- Crear rutas, pantallas o flujos de juego.
- Implementar reglas, estado, catálogo de cartas o manejo de activos del juego.
- Añadir React Router, Playwright, otro framework de pruebas o Prettier.
- Migrar o eliminar Blazor, o cambiar la solución .NET.
- Configurar despliegue o CORS.

## Modelo de datos

Esta especificación no introduce estructuras de datos de juego ni persistencia. Solo añade metadatos del proyecto y configuración de build.

## Plan de implementación

1. Crear el scaffold React + TypeScript de Vite en `src/ImaginArte.Client.React`, reemplazar la demo inicial por un placeholder accesible de ImaginArte y generar la instalación de npm. El cliente debe poder arrancar con `npm run dev`.
2. Añadir `motion`, declarar el requisito de Node, crear `.env.example` e ignorar dependencias, builds y archivos de entorno locales. Mantener versionado el ejemplo `.env.example`.
3. Configurar ESLint y los scripts `lint`, `typecheck` y `build`. `build` debe comprobar TypeScript y generar el build de Vite.
4. Actualizar `README.md` con Node requerido, instalación, comandos para ejecutar y compilar, y el procedimiento para copiar `.env.example` a `.env.local`.

**Archivos previstos:**

- Nuevos: `src/ImaginArte.Client.React/package.json`, `package-lock.json`, `index.html`, `vite.config.ts`, `eslint.config.js`, `tsconfig.json`, `tsconfig.app.json`, `tsconfig.node.json`, `.env.example`, `src/main.tsx`, `src/App.tsx`, `src/App.css`, `src/index.css` y `src/vite-env.d.ts`.
- Modificados: `.gitignore` y `README.md`.
- Sin cambios: `ImaginArte.sln`, Blazor y los proyectos .NET.

## Criterios de aceptación

- [ ] `package.json` declara React, React DOM y Motion como dependencias; y Vite, su plugin React, TypeScript, tipos y herramientas ESLint como dependencias de desarrollo.
- [ ] `package-lock.json` está presente y `npm ci` funciona con Node 22.12 o superior.
- [ ] `npm run dev` inicia la aplicación y muestra el placeholder de ImaginArte sin errores de consola.
- [ ] `npm run typecheck`, `npm run lint` y `npm run build` finalizan correctamente.
- [ ] `npm run preview` puede servir el build generado.
- [ ] `.env.example` contiene `VITE_API_BASE_URL=http://localhost:5080`; el README explica copiarlo a `.env.local`.
- [ ] `.gitignore` excluye `node_modules`, los builds y los archivos de entorno locales, pero permite versionar `.env.example`.
- [ ] El README documenta cómo instalar y ejecutar el cliente React.
- [ ] Blazor y la solución .NET siguen presentes y sin cambios.

## Decisiones

- **Sí:** Vite + React + TypeScript, en consonancia con la arquitectura y el README del proyecto.
- **Sí:** npm y su lockfile, porque el repositorio aún no tiene otro gestor de paquetes de JavaScript.
- **Sí:** instalar Motion al crear el proyecto, como ya prescribe el README. No añadir animaciones al placeholder.
- **Sí:** ESLint y comprobación de tipos; sin Prettier ni suite de pruebas en este scaffold.
- **Sí:** preparar `VITE_API_BASE_URL` para configuración de build; el placeholder no conecta con la API.
- **No:** añadir SignalR, routing o flujos de juego en esta fase; corresponden a slices posteriores.

## Riesgos

| Riesgo | Mitigación |
| --- | --- |
| Las variables `VITE_*` se incorporan al cliente y son visibles en el navegador. | Documentar que `VITE_API_BASE_URL` es configuración pública y no debe contener secretos. |
| Vite requiere una versión compatible de Node. | Declarar `>=22.12.0` y documentar el requisito en `README.md`. |

## Qué **no** incluye esta especificación

- Conexión HTTP o SignalR.
- Pantallas, rutas o reglas de juego.
- Playwright, router u otros frameworks de pruebas.
- Cambios a Blazor o a la solución .NET.
- Despliegue, CORS o eliminación del cliente existente.
