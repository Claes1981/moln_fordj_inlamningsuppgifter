# Repo Context

Course assignment: "Inlämningsuppgift 1" for MolnFordj (Cloud Developer) program. Build a containerized .NET MVC web app (CloudSoft Recruitment Portal) and deploy it to Azure Container Apps with CI/CD.

## Tech Stack

- **.NET MVC web app** with two user roles: `Candidate` and `Administrator`
- **Podman** — multi-stage Dockerfile, Podman Compose for local dev (app + CosmosDB emulator)
- **Azure Container Apps** — production hosting
- **Azure Container Registry** — image storage
- **GitHub Actions** — CI/CD pipeline (build, test, push, deploy)
- **CosmosDB** — local dev uses Podman container, production uses Azure CosmosDB

## Structure (when built)

- App code lives at the repo root (`.csproj`, `.sln`, `Program.cs`, etc.)
- `Dockerfile` and `docker-compose.yml` at the repo root
- `.github/workflows/` — CI/CD pipeline
- `doc/` — assignment materials and user stories (not part of the app)

## Key Constraints

- The assignment PDF (`doc/task/assignment-acd-1-swe.pdf`) is **not tracked in Git** (license unclear). Assignment text is in the commit history if needed.
- Secrets must **never** be committed. Use GitHub Actions secrets or Azure Key Vault.
- `.env` files are gitignored — use them for local config only.
- User stories are in `doc/user_stories/` and define the scope.
- Keep it simple: use only tools and patterns covered in the course labs. Do not introduce advanced patterns beyond what was taught.

## Useful Commands

- `dotnet run` — run the app locally (requires database available per connection string)
- `dotnet build` — verify the project compiles
- `dotnet test` — run tests (if test project exists)
- `podman compose up` — run full stack locally (app + database)
- `podman build -t <image> .` — build container image
