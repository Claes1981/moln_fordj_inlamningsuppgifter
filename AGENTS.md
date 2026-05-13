# Repo Context

Course assignment: "Inlämningsuppgift 1" for MolnFordj (Cloud Developer) program. Build a containerized .NET MVC web app (CloudSoft Recruitment Portal) and deploy it to Azure Container Apps with CI/CD.

## Architecture

4-project multi-layer solution in `src/`, wired together by `CloudSoft.slnx` (VS2022 v2 solution format — no `.sln` file exists):

| Project | Purpose |
|---|---|
| `CloudSoft.Domain` | Entities (`JobPosting`, `JobApplication`), enums (`JobPostingStatus`, `ApplicationStatus`), `IRepository<T>` interface |
| `CloudSoft.Data` | `CosmosRepository<T>` — generic CosmosDB repository |
| `CloudSoft.Services` | `IJobPostingService` / `JobPostingService` — business logic with CRUD + publish/close operations |
| `CloudSoft.Web` | MVC app entrypoint, controllers, views, DI config in `Program.cs` |

Dependency chain: Web → Services → Data → Domain. Web also references Domain directly.

Controllers: `Home`, `Account`, `JobPostings`, `Health`.

## Tech Stack

- **.NET 10.0** MVC web app with two user roles: `Candidate` and `Administrator`
- **CosmosDB** via `Microsoft.Azure.Cosmos` (v3.59.0) — `CosmosClient` singleton in `Program.cs`
- **Cookie authentication** — hardcoded credentials in `AccountController` (admin/admin123, candidate/candidate123)
- **Podman** — multi-stage Dockerfile + Podman Compose for local dev
- **Azure Container Apps** — production hosting (NOT YET SET UP)
- **GitHub Actions** — CI/CD pipeline (NOT YET CREATED)
- **Bicep** — IaC for Azure resources (NOT YET CREATED)

## Key Gotchas

- **No `.sln` file** — solution is `CloudSoft.slnx` (VS2022 v2 format). Use `dotnet build src/CloudSoft.Web/CloudSoft.Web.csproj` or `dotnet run --project src/CloudSoft.Web/CloudSoft.Web.csproj`.
- **CosmosDB connection string required** — `Program.cs` throws `InvalidOperationException` if `ConnectionStrings:CosmosDb` is missing.
- **CosmosDB emulator cert bypass** — `Program.cs` disables cert validation in Development via `ServicePointManager` + `AppContext.SetSwitch("System.Net.Http.UseSocketsHttpHandler", false)` + `DangerousAcceptAnyServerCertificateValidator` on `HttpClientFactory`. Also sets `ConnectionMode.Gateway`.
- **Auto-creates database/container** — `Program.cs` calls `CreateDatabaseIfNotExistsAsync` + `CreateContainerIfNotExistsAsync` on startup.
- **`SecurePolicy.Always` on cookies** — login POST returns 400 over HTTP. Local dev needs HTTPS or relax cookie policy.
- **JobPostingsController is admin-only** — `[Authorize(Roles = "Administrator")]` on the entire controller.
- **`aspnet:10.0` is chiseled** — no `curl`, no `adduser`. Dockerfile has no HEALTHCHECK or non-root user.
- **Assignment docs are gitignored** — `doc/task/assignment-acd-1-swe.pdf` and `.md` are in `.gitignore`. Don't commit them.
- **No tests exist** — `tests/` directory is empty.

## Commands

- `dotnet build src/CloudSoft.Web/CloudSoft.Web.csproj` — verify compilation
- `dotnet run --project src/CloudSoft.Web/CloudSoft.Web.csproj` — run locally (requires CosmosDB connection string)
- `podman build --format docker -t cloudsoft-recruitment .` — build container image
- `podman compose up` — run full stack (webapp + CosmosDB emulator)

## Configuration

- `src/CloudSoft.Web/appsettings.json` — minimal config, no connection strings
- `src/CloudSoft.Web/appsettings.Development.json` — logging only
- Connection strings via environment variables, user secrets, or `.env` (gitignored):
  - `ConnectionStrings:CosmosDb` — required
  - `CosmosDb:DatabaseName` — defaults to `CloudSoft`
  - `CosmosDb:ContainerName` — defaults to `JobPostings`

## Assignment Status

- Delmoment 1 (Agile/user stories): DONE — user stories in `doc/user_stories/`
- Delmoment 2 (Containerization): DONE — Dockerfile + docker-compose.yml, tested with podman
- Delmoment 3 (Auth + data layer): DONE — cookie auth, role control, CosmosDB repository, job postings CRUD
- Delmoment 4 (CI/CD + Azure): NOT STARTED — need GitHub Actions workflow + Bicep IaC
- Delmoment 5 (Verification): NOT STARTED — depends on deployment
- Report: IN PROGRESS — `doc/report.md` and `doc/report.pdf`

## Constraints

- Secrets must **never** be committed. Use GitHub Actions secrets or Azure Key Vault.
- `.env` files are gitignored — use them for local config only.
- Keep it simple: use only tools and patterns covered in course labs at https://cloud-dev-25.educ8.se/exercises/
- Course exercises reference: Webapp (10-webapp-development), Docker (20-docker), Deployment (3-deployment/9-cicd-to-container-apps), Cloud DB (5-cloud-databases)
