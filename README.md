# OmniHogar Backend

ASP.NET Core 10 (.NET 10 LTS) API, Clean Architecture, backing [`omnihogar-frontend`](../omnihogar-frontend) (Angular 22).

## Solution layout

```
src/
  OmniHogar.Domain          entities, exceptions, no external deps
  OmniHogar.Application     use cases (MediatR CQRS), validation (FluentValidation), DTOs, mapping
  OmniHogar.Infrastructure  EF Core + Npgsql, ASP.NET Core Identity, JWT issuing
  OmniHogar.WebApi          controllers, Swagger, CORS, composition root (Program.cs)
tests/
  OmniHogar.Domain.Tests
  OmniHogar.Application.Tests
```

Dependency direction: `WebApi -> Infrastructure -> Application -> Domain`. Domain has zero dependencies; Application depends only on Domain + EF Core's `DbSet<>` abstraction via `IApplicationDbContext`.

## Prerequisites

- .NET SDK 10.0.400+
- PostgreSQL running locally (or via Docker)
- `dotnet tool install -g dotnet-ef` (already installed on this machine)

## First run

```bash
# start postgres however you prefer, e.g.:
docker run --name omnihogar-db -e POSTGRES_PASSWORD=postgres -e POSTGRES_DB=omnihogar -p 5432:5432 -d postgres:16

# apply migrations
dotnet ef database update --project src/OmniHogar.Infrastructure --startup-project src/OmniHogar.WebApi

# run the API
dotnet run --project src/OmniHogar.WebApi
```

Swagger UI at `https://localhost:<port>/swagger` in Development.

## Secrets

`appsettings.json` / `appsettings.Development.json` are tracked in git and must only ever contain placeholder values (they do — `Jwt:Secret` is a placeholder string, and the default DB credentials are throwaway local-dev values). Never overwrite those placeholders with real values in a tracked file.

Real secrets go two places, neither of which git ever sees:

- **Local dev**: `dotnet user-secrets` (already set up for this project — the real `Jwt:Secret` lives in `~/.microsoft/usersecrets/<UserSecretsId>/secrets.json`, outside the repo). Set it yourself with:
  ```bash
  dotnet user-secrets set "Jwt:Secret" "$(openssl rand -base64 48)" --project src/OmniHogar.WebApi
  ```
- **Staging/prod**: environment variables (`Jwt__Secret`, `ConnectionStrings__DefaultConnection`) or your host's secret manager (Azure Key Vault, AWS Secrets Manager, etc). Never in `appsettings.Production.json` — that filename is gitignored precisely so nobody's tempted to.

Before every push: `git status` / `git diff --staged` and eyeball for real connection strings, tokens, or keys — .gitignore covers the obvious filenames but can't catch a real secret pasted into a tracked file by hand.

## Connecting the Angular frontend

CORS is configured for `http://localhost:4200` by default (see `Cors:AllowedOrigins` in `appsettings.json`). Auth endpoints (`/api/auth/register`, `/api/auth/login`) return a JWT the Angular app should attach as `Authorization: Bearer <token>`.

## Adding a new feature

Follow the `Products` slice in `src/OmniHogar.Application/Features/Products` as the template: one file per use case (query/command + handler + validator), a DTO, an EF Core entity configuration in Infrastructure, and a thin controller in WebApi that just calls `ISender.Send(...)`.
