# ImaginArte.Server

Hosts the ASP.NET Core backend.

This project should expose HTTP endpoints, SignalR hubs, composition root wiring, and MVP hosting for the Blazor frontend when useful. It delegates game behavior to application services and should not own domain rules.

## Render + Neon configuration

Production is prepared to use PostgreSQL. Configure these environment variables in Render:

```text
Persistence__Provider=PostgreSQL
Persistence__ConnectionString=<Neon connection string>
RoomCleanup__Enabled=true
Cors__AllowedOrigins__0=<frontend origin>
```

Use the secure Neon connection string from the Neon dashboard and keep it out of source control. Local development continues to use SQLite through `appsettings.Development.json`.

Render can use `GET /health` as a lightweight liveness endpoint. It only checks that the ASP.NET Core app is running; it does not create rooms or execute game logic.
