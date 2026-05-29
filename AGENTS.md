
# PROJECT KNOWLEDGE BASE

**Generated:** 2026-05-26

## OVERVIEW

Project: **CloudSoft Recruitment Portal**
Stack: **.NET 10.0**, ASP.NET Core MVC + REST API, CosmosDB, Azure Blob Storage, ASP.NET Core Identity, Azure Container Apps, Bicep, GitHub Actions, Podman

Course assignment for "Molnapplikationer Fördjupning" (course in Cloud Developer program). Currently implementing **Inlämningsuppgift 2** (Observability, REST API, File Upload, Deep Health Probes) on branch `feature/inlamningsuppgift2`.

## STRUCTURE

```
.
├── CloudSoft.slnx              # VS2022 v2 solution (no .sln file)
├── Dockerfile                  # Multi-stage aspnet:10.0 image
├── docker-compose.yml          # Local dev stack (webapp + CosmosDB emulator)
├── .dockerignore
├── .gitignore
├── AGENTS.md
├── doc/
│   ├── reports/               # Archived assignment reports (report1.md, report1.pdf, etc.)
│   ├── tasks/                 # Assignment documents (gitignored PDFs + .md)
│   └── user_stories/          # User story cards
├── infra/
│   └── main.bicep             # IaC: CosmosDB, Container Apps, Blob Storage
├── .github/workflows/
│   └── ci-cd.yml              # CI/CD: build → Docker Hub → deploy via Bicep
└── src/
    ├── CloudSoft.Domain/      # Entities, interfaces, enums, constants
    ├── CloudSoft.Data/        # CosmosDB repository, Identity context, Blob storage
    ├── CloudSoft.Services/    # Business logic (IJobPostingService)
    └── CloudSoft.Web/         # MVC + REST API entrypoint, controllers, middleware, views
```

### Project Details

| Project | Purpose |
|---|---|
| `CloudSoft.Domain` | Entities (`JobPosting`, `JobApplication`), enums (`JobPostingStatus`, `ApplicationStatus`), interfaces (`IRepository<T>`, `ICosmosEntity`, `IBlobService`), `Constants` |
| `CloudSoft.Data` | `CosmosRepository<T>` (generic CosmosDB repo), `AzureBlobService` + `NoOpBlobService`, `ApplicationDbContext` + `ApplicationUser` + `IdentitySeeder` |
| `CloudSoft.Services` | `IJobPostingService` / `JobPostingService` — CRUD + publish/close operations |
| `CloudSoft.Web` | MVC + REST API entrypoint, controllers, views, middleware, health checks, DTOs, DI extensions |

Dependency chain: **Web → Services → Data → Domain**. Web also references Domain directly.

### Controllers

| Controller | Type | Auth | Route |
|---|---|---|---|
| `HomeController` | MVC | None | `/` |
| `AccountController` | MVC | None (login/logout) | `/Account` |
| `JobPostingsController` | MVC | `[Authorize(Roles = Administrator)]` | `/JobPostings` |
| `ApiJobPostingsController` | `[ApiController]` | API Key middleware | `/api/JobPostings` |
| `ResumeUploadController` | MVC | None (public upload) | `/ResumeUpload` |
| `HealthController` | MVC | None | `/health`, `/health/live`, `/health/ready` |

### Middleware Pipeline (in order)

1. `CorrelationIdMiddleware` — generates/reuses `X-Correlation-ID`, adds to logging scope
2. `ApiKeyMiddleware` — validates `X-API-Key` header for `/api/*` routes (skips Swagger)
3. Swagger (always enabled)
4. Routing → Authentication → Authorization → MVC

### Health Checks

| Endpoint | Purpose | Dependencies |
|---|---|---|
| `/health/live` | Liveness probe — process alive? | None |
| `/health/ready` | Readiness probe — startup complete | None |
| `/health` | Detailed diagnostics | CosmosDB (`CosmosHealthCheck`), Blob Storage (`BlobHealthCheck`) |

## COMMANDS

| Action | Command |
|--------|---------|
| Build | `dotnet build src/CloudSoft.Web/CloudSoft.Web.csproj` |
| Run locally | `dotnet run --project src/CloudSoft.Web/CloudSoft.Web.csproj` |
| List secrets | `dotnet user-secrets --project src/CloudSoft.Web list` |
| Set secret | `dotnet user-secrets --project src/CloudSoft.Web set ConnectionStrings:CosmosDb "..."` |
| Build container | `podman build --format docker -t cloudsoft-recruitment .` |
| Run full stack | `podman compose up` |

## CODING STANDARDS

- **Language**: C# 12 / .NET 10.0, top-level statements in `Program.cs`
- **Style**: Expression-bodied members where concise, braces on all control flow, XML doc comments on public APIs
- **Patterns**:
  - Repository pattern via `IRepository<T>` constrained to `ICosmosEntity`
  - Service layer (`IJobPostingService`) between controllers and data
  - DI organized into extension methods (`CosmosExtensions`, `IdentityExtensions`)
  - Structured logging with `ILogger<T>` message templates
  - `CancellationToken` propagated through all async methods
  - DTOs for REST API boundaries (`JobPostingDto`, `JobPostingOutputDto`)
  - Interface-based blob storage (`IBlobService`) with conditional DI (`AzureBlobService` or `NoOpBlobService`)
- **No linter/formatter config** — relies on Visual Studio defaults
- **No tests** — `tests/` directory is empty

## WHERE TO LOOK

- **Source**: `src/` (4 projects)
- **Tests**: `tests/` (empty)
- **Docs**: `doc/` (reports in `doc/reports/`, tasks in `doc/tasks/`)
- **Infrastructure**: `infra/main.bicep`
- **CI/CD**: `.github/workflows/ci-cd.yml`
- **Container**: `Dockerfile`, `docker-compose.yml`

## KEY GOTCHAS

- **No `.sln` file** — solution is `CloudSoft.slnx` (VS2022 v2 format). Always target `.csproj` directly.
- **CosmosDB connection string required** — `CosmosExtensions.AddCosmosDb` throws `InvalidOperationException` if `ConnectionStrings:CosmosDb` is missing. Configure via user secrets or env vars.
- **CosmosDB emulator cert bypass** — `CosmosExtensions.AddCosmosDb` handles this in Development: disables `SocketsHttpHandler`, sets `DangerousAcceptAnyServerCertificateValidator`, uses `ConnectionMode.Gateway`.
- **Auto-creates database/container** — `EnsureCosmosDbAsync()` called in `Program.cs` on startup.
- **`SecurePolicy.None` in Development** — `IdentityExtensions` relaxes cookie security for local HTTP dev; `Always` in production.
- **`SameSiteMode.Strict`** — used instead of `SameSite.Lax` (compilation error in .NET 10). Deprecation warning is acceptable.
- **`aspnet:10.0` is chiseled** — no `curl`, no `adduser`. Dockerfile has no HEALTHCHECK or non-root user.
- **Assignment docs are gitignored** — `doc/*/` patterns in `.gitignore`. Don't commit PDFs or task docs.
- **PartitionKey required** — All CosmosDB entities implement `ICosmosEntity` with `Id` and `PartitionKey`. `CosmosRepository<T>` reads the partition key value dynamically from `entity.PartitionKey`.
- **Blob Storage is conditional** — `AzureBlobService` registered only if `BlobStorage` config exists; otherwise `NoOpBlobService` is used for local dev.
- **API Key middleware** — validates `X-API-Key` header for `/api/*` routes. Configured via `ApiKey:Keys` in app settings. Skips Swagger routes.
- **`appsettings.Development.json` is gitignored** — use user secrets or environment variables for local config.
- **`appsettings.json` uses JSON console logging** — `IncludeScopes: true` for correlation ID tracing.
- **`ApplicationUser` is empty** — extends `IdentityUser` with no custom properties.
- **`ApplicationDbContext` seeds roles in `OnModelCreating`** — hardcoded role data for EF migrations. `IdentitySeeder` handles runtime seeding (idempotent).
- **Environment variable convention** — double-underscore (`__`) for nested config (e.g., `ConnectionStrings__CosmosDb`, `BlobStorage__AccountUrl`).

## CONFIGURATION

- `src/CloudSoft.Web/appsettings.json` — JSON console logging config, AllowedHosts
- `src/CloudSoft.Web/appsettings.Development.json` — gitignored; seed credentials
- All config via environment variables, user secrets, or `.env` (gitignored):

| Setting | Default | Used By |
|---|---|---|
| `ConnectionStrings:CosmosDb` | *(required)* | CosmosExtensions |
| `CosmosDb:DatabaseName` | `CloudSoft` | CosmosExtensions, Bicep |
| `CosmosDb:ContainerName` | `JobPostings` | CosmosExtensions, Bicep |
| `IdentityStore:Provider` | `inmemory` | IdentityExtensions |
| `ConnectionStrings:IdentityDb` | `cloudsoft_identity.db` | IdentityExtensions (SQLite) |
| `AdminSeed:Username` | `admin` | IdentitySeeder |
| `AdminSeed:Password` | `Admin123!` | IdentitySeeder |
| `AdminSeed:Email` | `admin@cloudsoft.com` | IdentitySeeder |
| `BlobStorage__AccountUrl` | *(none)* | AzureBlobService (Managed Identity) |
| `ConnectionStrings:BlobStorage` | *(none)* | AzureBlobService (connection string) |
| `ApiKey:Keys` | *(none)* | ApiKeyMiddleware |

## CONSTANTS (Centralized in `CloudSoft.Domain.Constants`)

| Constant | Value | Used By |
|---|---|---|
| `PartitionKeyPath` | `/PartitionKey` | CosmosRepository, CosmosExtensions, Bicep |
| `DefaultDatabaseName` | `CloudSoft` | CosmosExtensions, Bicep |
| `DefaultContainerName` | `JobPostings` | CosmosExtensions, Bicep |
| `AdministratorRole` | `Administrator` | JobPostingsController, IdentitySeeder, ApplicationDbContext |
| `CandidateRole` | `Candidate` | IdentitySeeder, ApplicationDbContext |
| `Roles[]` | `{ Administrator, Candidate }` | IdentitySeeder |

## DEPLOYMENT

- **Deployed URL**: `https://cloudsoft-x94s8o.lemonisland-d700b917.northeurope.azurecontainerapps.io`
- **Resource Group**: `cloudsoft-rg` in `northeurope`
- **Bicep**: `infra/main.bicep` provisions CosmosDB (autoscale 1000 RU, `disableLocalAuth: true`), Container Apps environment (Azure Monitor logging), Blob Storage, and web app (1-3 replicas, 0.5 CPU, 1Gi RAM, TLS via `transport: 'auto'`)
- **CI/CD**: `.github/workflows/ci-cd.yml` builds Docker image to Docker Hub (tagged by SHA + `latest`) and deploys via Bicep
- **Container Image**: `claes1981/cloudsoft-recruitment:latest` on Docker Hub
- **Secrets injected as Container Apps secrets → env vars**: `ConnectionStrings:CosmosDb`, `BlobStorage__AccountUrl`

## ASSIGNMENT STATUS

- **Assignment 1**: ✅ Submitted — archived in `doc/reports/` (`report1.md`, `report1.pdf`, `screenshot_app1.png`)
- **Assignment 2**: 🔄 In progress on `feature/inlamningsuppgift2`
  - ✅ Delmoment 1: Observability (structured logging, correlation ID)
  - ✅ Delmoment 2: REST API (DTOs, Swagger, API key middleware)
  - ✅ Delmoment 3: File upload + health probes (Azure Blob, Managed Identity, deep probes)
  - 🔄 Delmoment 4: Architecture review (Bicep Blob Storage provisioning, report)

## CONSTRAINTS

- Secrets must **never** be committed. Use GitHub Actions secrets, Azure Key Vault, or user secrets.
- `.env` files are gitignored — use them for local config only.
- Keep it simple: use only tools and patterns covered in course labs at https://cloud-dev-25.educ8.se/exercises/
- Course exercises referenced:
  - Structured logging: `3-deployment/10-logging-and-monitoring/1-structured-logging-ilogger/`
  - Container logs to Log Analytics: `3-deployment/10-logging-and-monitoring/2-container-logs-to-log-analytics/`
  - REST API & DTOs: `4-services-and-apis/1-rest-api-and-dtos/1-rest-controllers-and-dtos/`
  - API Key Middleware: `4-services-and-apis/1-rest-api-and-dtos/3-api-key-middleware/`
  - MVC Uploads & PDF validation: `6-storage-and-resilience/1-uploads-and-deep-probes/1-mvc-uploads-and-pdf-validation/`
  - Cosmos/Blob via Managed Identity: `6-storage-and-resilience/1-uploads-and-deep-probes/2-cosmos-and-blob-via-managed-identity/`
  - Deep health probes: `6-storage-and-resilience/1-uploads-and-deep-probes/3-deep-health-probes-and-cleanup/`

## CONTEXT FILES

- No `.cursorrules`, `CLAUDE.md`, or other AI config files found in this repository.
