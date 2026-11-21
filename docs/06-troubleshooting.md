# 6. Troubleshooting

| Symptom | Cause / fix |
|---|---|
| `dotnet restore` fails with 401 | A machine-level NuGet feed needs credentials. Repo `nuget.config` clears inherited sources; keep it. |
| `dotnet test` complains about the runner | .NET 10 needs `"test": {"runner": "Microsoft.Testing.Platform"}` in `global.json` (already set). Use `--solution` and `--filter-trait`. |
| "ConnectionStrings:CatalogDb is not set" | Set user secrets or `ConnectionStrings__CatalogDb`. |
| Seeder says migrations are pending / error 208 | Run `dotnet ef database update` first. After `migrations add`, run `dotnet build` before any `--no-build` command or the old assembly is used. |
| `docker compose up` fails: daemon not running | Start Docker Desktop. |
| `MSSQL_SA_PASSWORD` error | Copy `.env.example` to `.env` and set a strong password (8+ chars, mixed). |
| Container unhealthy / exits | Check `docker compose logs sqlserver`; image needs x64 or x86 emulation and ~2 GB RAM. |
| Login failed | Check user/password and that port matches `MSSQL_PORT` (default 14330). |
| Integration tests skipped | Set `CQRSDEMO_TEST_SQLSERVER` (server connection string, no database). |
