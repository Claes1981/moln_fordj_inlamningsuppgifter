# Repo Context

Course assignment: "Inlämningsuppgift 1" for MolnFordj (Cloud Developer) program. Build a containerized .NET MVC web app (CloudSoft Recruitment Portal) and deploy it to Azure Container Apps with CI/CD.

## Architecture

4-project multi-layer solution in `src/`, wired together by `CloudSoft.slnx` (VS2022 v2 solution format — no `.sln` file exists):

| Project | Purpose |
|---|---|
| `CloudSoft.Domain` | Entities (`JobPosting`, `JobApplication`), enums (`JobPostingStatus`, `ApplicationStatus`), `IRepository<T>` interface, `Constants` class |
| `CloudSoft.Data` | `CosmosRepository<T>` — generic CosmosDB repository; `ApplicationDbContext` + `ApplicationUser` + `IdentitySeeder` for ASP.NET Core Identity |
| `CloudSoft.Services` | `IJobPostingService` / `JobPostingService` — business logic with CRUD + publish/close operations |
| `CloudSoft.Web` | MVC app entrypoint, controllers, views, DI config in `Program.cs`, extension methods in `Extensions/` |

Dependency chain: Web → Services → Data → Domain. Web also references Domain directly.

Controllers: `Home`, `Account`, `JobPostings`, `Health`.

DI is organized into extension methods:
- `CosmosExtensions.AddCosmosDb` — registers `CosmosClient`, `IRepository<JobPosting>`, and `EnsureCosmosDbAsync`
- `IdentityExtensions.AddCloudSoftIdentity` — registers Identity with InMemory/SQLite store, cookie auth config

`Program.cs` is slim (~35 lines) — delegates all setup to extension methods.

## Tech Stack

- **.NET 10.0** MVC web app with two user roles: `Candidate` and `Administrator`
- **ASP.NET Core Identity** — `SignInManager`-based auth (replaced hardcoded cookie auth); InMemory store by default, SQLite switchable via `IdentityStore:Provider` config
- **CosmosDB** via `Microsoft.Azure.Cosmos` (v3.59.0) — `CosmosClient` singleton registered in `CosmosExtensions`
- **Entity Framework Core** — InMemory (`Microsoft.EntityFrameworkCore.InMemory`) or SQLite (`Microsoft.EntityFrameworkCore.Sqlite`) for Identity store
- **Podman** — multi-stage Dockerfile + Podman Compose for local dev
- **Azure Container Apps** — production hosting (DEPLOYED)
- **GitHub Actions** — CI/CD pipeline (`.github/workflows/ci-cd.yml`)
- **Bicep** — IaC for Azure resources (`infra/main.bicep`)
- **User Secrets** — `UserSecretsId=cloudsoft-web-dev` in csproj for local config

## Key Gotchas

- **No `.sln` file** — solution is `CloudSoft.slnx` (VS2022 v2 format). Use `dotnet build src/CloudSoft.Web/CloudSoft.Web.csproj` or `dotnet run --project src/CloudSoft.Web/CloudSoft.Web.csproj`.
- **CosmosDB connection string required** — `CosmosExtensions.AddCosmosDb` throws `InvalidOperationException` if `ConnectionStrings:CosmosDb` is missing. Configure via user secrets: `dotnet user-secrets --project src/CloudSoft.Web set ConnectionStrings:CosmosDb "..."`.
- **CosmosDB emulator cert bypass** — merged into `CosmosExtensions.AddCosmosDb` for Development: disables `SocketsHttpHandler` via `AppContext.SetSwitch`, sets `DangerousAcceptAnyServerCertificateValidator` on `HttpClientFactory`, and uses `ConnectionMode.Gateway`.
- **Auto-creates database/container** — `EnsureCosmosDbAsync()` called in `Program.cs` on startup.
- **`SecurePolicy.None` in Development** — `IdentityExtensions` relaxes cookie security for local HTTP dev; `Always` in production.
- **`SameSiteMode.Strict`** — used instead of `SameSite.Lax` (compilation error in .NET 10). Deprecation warning is acceptable.
- **JobPostingsController is admin-only** — `[Authorize(Roles = Constants.AdministratorRole)]` on the entire controller.
- **`aspnet:10.0` is chiseled** — no `curl`, no `adduser`. Dockerfile has no HEALTHCHECK or non-root user.
- **Assignment docs are gitignored** — `doc/task/assignment-acd-1-swe.pdf` and `.md` are in `.gitignore`. Don't commit them.
- **No tests exist** — `tests/` directory is empty.
- **PartitionKey required** — Both `JobPosting` and `JobApplication` entities must have a `PartitionKey` property matching the CosmosDB container's partition key path (`/PartitionKey`). The `CosmosRepository` defaults to `Constants.PartitionKey`.
- **`ApplicationUser` is empty** — extends `IdentityUser` with no custom properties.
- **`ApplicationDbContext` seeds roles in `OnModelCreating`** — hardcoded role data for EF migrations. `IdentitySeeder` handles runtime seeding (idempotent).
- **`appsettings.Development.json` is gitignored** — contains hardcoded seed credentials. Use user secrets or environment variables for local dev.

## Commands

- `dotnet build src/CloudSoft.Web/CloudSoft.Web.csproj` — verify compilation
- `dotnet run --project src/CloudSoft.Web/CloudSoft.Web.csproj` — run locally (requires CosmosDB connection string via user secrets or env)
- `dotnet user-secrets --project src/CloudSoft.Web list` — view configured secrets
- `podman build --format docker -t cloudsoft-recruitment .` — build container image
- `podman compose up` — run full stack (webapp + CosmosDB emulator)

## Configuration

- `src/CloudSoft.Web/appsettings.json` — minimal config (logging, AllowedHosts), no connection strings
- `src/CloudSoft.Web/appsettings.Development.json` — gitignored; may contain seed credentials
- All config via environment variables, user secrets, or `.env` (gitignored):
  - `ConnectionStrings:CosmosDb` — required
  - `CosmosDb:DatabaseName` — defaults to `CloudSoft` (via `Constants.DefaultDatabaseName`)
  - `CosmosDb:ContainerName` — defaults to `JobPostings` (via `Constants.DefaultContainerName`)
  - `IdentityStore:Provider` — `"inmemory"` (default) or `"sqlite"`
  - `ConnectionStrings:IdentityDb` — SQLite connection string (defaults to `cloudsoft_identity.db`)
  - `AdminSeed:Username`, `AdminSeed:Password`, `AdminSeed:Email` — seed admin credentials (defaults: `admin` / `Admin123!` / `admin@cloudsoft.com`)

## Constants (Centralized in `CloudSoft.Domain.Constants`)

| Constant | Value | Used By |
|---|---|---|
| `PartitionKey` | `/PartitionKey` | CosmosRepository, CosmosExtensions, Bicep |
| `DefaultDatabaseName` | `CloudSoft` | CosmosExtensions, Bicep |
| `DefaultContainerName` | `JobPostings` | CosmosExtensions, Bicep |
| `AdministratorRole` | `Administrator` | JobPostingsController, IdentitySeeder, ApplicationDbContext |
| `CandidateRole` | `Candidate` | IdentitySeeder, ApplicationDbContext |
| `Roles[]` | `{ Administrator, Candidate }` | IdentitySeeder |

## Deployment

- **Deployed URL**: `https://cloudsoft-x94s8o.lemonisland-d700b917.northeurope.azurecontainerapps.io`
- **Resource Group**: `cloudsoft-rg` in `northeurope`
- **Bicep**: `infra/main.bicep` provisions CosmosDB (autoscale 1000 RU), Container Apps environment (Azure Monitor logging), and web app (1-3 replicas, 0.5 CPU, 1Gi RAM)
- **CI/CD**: `.github/workflows/ci-cd.yml` builds Docker image to Docker Hub (tagged by SHA + `latest`) and deploys via Bicep
- **Container Image**: `claes1981/cloudsoft-recruitment:latest` on Docker Hub
- **Cosmos connection string** injected as Container Apps secret → env var

## Assignment Status

- Delmoment 1 (Agile/user stories): DONE — user stories in `doc/user_stories/`
- Delmoment 2 (Containerization): DONE — Dockerfile + docker-compose.yml, tested with podman
- Delmoment 3 (Auth + data layer): DONE — ASP.NET Core Identity, role control, CosmosDB repository, job postings CRUD
- Delmoment 4 (CI/CD + Azure): DONE — GitHub Actions workflow + Bicep IaC, deployed to Azure
- Delmoment 5 (Verification): IN PROGRESS — app accessible, CRUD operations need testing
- Report: IN PROGRESS — `doc/report.md` needs update for Delmoment 4-5

## Constraints

- Secrets must **never** be committed. Use GitHub Actions secrets, Azure Key Vault, or user secrets.
- `.env` files are gitignored — use them for local config only.
- Keep it simple: use only tools and patterns covered in course labs at https://cloud-dev-25.educ8.se/exercises/
- Course exercises reference: Webapp (10-webapp-development), Docker (20-docker), Deployment (3-deployment/9-cicd-to-container-apps), Cloud DB (5-cloud-databases)
