# Repo Context

Course assignment: "Inlämningsuppgift 1" for MolnFordj (Cloud Developer) program. Build a containerized .NET MVC web app (CloudSoft Recruitment Portal) and deploy it to Azure Container Apps with CI/CD.

## Tech Stack

- **.NET MVC web app** with two user roles: `Candidate` and `Administrator`
- **Podman** — multi-stage Dockerfile, Podman Compose for local dev (app + CosmosDB emulator)
- **Azure Container Apps** — production hosting
- **Azure Container Registry** — image storage
- **GitHub Actions** — CI/CD pipeline (build, test, push, deploy)
- **CosmosDB** — local dev uses Podman container, production uses Azure CosmosDB
- **Bicep** — Infrastructure as Code for Azure resource provisioning

## Structure (when built)

- App code lives at the repo root (`.csproj`, `.sln`, `Program.cs`, etc.)
- `Dockerfile` and `docker-compose.yml` at the repo root
- `.github/workflows/` — CI/CD pipeline
- `doc/` — assignment materials and user stories (not part of the app)

## Assignment Overview

The assignment has **5 delmoment** (sub-tasks) and **3 cross-cutting concerns**:

### Delmoment

1. **Agilt arbetssätt och inner loop** — 3–5 user stories, inner loop description
2. **Containerisering och lokal utvecklingsmiljö** — Dockerfile, Podman Compose, local stack
3. **Autentisering, auktorisering och datalager** — user roles, auth, security, data persistence
4. **CI/CD och driftsättning på Azure** — GitHub Actions pipeline, Azure deployment
5. **Verifiering av den driftsatta lösningen** — verify deployed app works from public internet

### Cross-Cutting Concerns

- **Säkerhet** — cookie settings, role control, secret management, registry access, image provenance, network exposure
- **Infrastructure as Code** — reproducible environment via scripts, templates, or workflow steps
- **AI-assistenterna** — reflect on how AI assistants were used, where they helped or failed

### Deliverables

- **PDF report** explaining what was built and why (decisions, motivations, structure)
- **First page** must include: name, screenshot of deployed app with public URL, link to public GitHub repo
- **GitHub repo** must contain: app code, Dockerfile, docker-compose, infra scripts/templates, GitHub Actions workflow
- Diagrams must be original, code snippets must be copyable (not screenshots)

## Key Constraints

- The assignment PDF (`doc/task/assignment-acd-1-swe.pdf`) is **not tracked in Git** (license unclear). Markdown version is at `doc/task/assignment-acd-1-swe.md`.
- Secrets must **never** be committed. Use GitHub Actions secrets or Azure Key Vault.
- `.env` files are gitignored — use them for local config only.
- User stories are in `doc/user_stories/` and define the scope.
- Keep it simple: use only tools and patterns covered in the course labs. Do not introduce advanced patterns beyond what was taught.
- Describe what's in scope, what's out of scope, and justify the boundary.
- Course exercises reference: <https://cloud-dev-25.educ8.se/exercises/>

## Relevant Course Exercises

These exercises from the course cover the skills needed for this assignment:

- **Webapp Development** — MVC, forms, validation, service layer, repository pattern, CosmosDB, auth, Identity: <https://cloud-dev-25.educ8.se/exercises/10-webapp-development/>
- **Docker** — containerize app, multi-stage builds, Compose: <https://cloud-dev-25.educ8.se/exercises/20-docker/>
- **Deployment** — CI/CD to Azure Container Apps, OIDC federation: <https://cloud-dev-25.educ8.se/exercises/3-deployment/9-cicd-to-container-apps/>
- **Cloud Databases** — CosmosDB provisioning (portal, CLI, Bicep): <https://cloud-dev-25.educ8.se/exercises/5-cloud-databases/>
- **Code Collaboration** — Git, Jira, PR workflow: <https://cloud-dev-25.educ8.se/exercises/15-code-collaboration/>

## Useful Commands

- `dotnet run` — run the app locally (requires database available per connection string)
- `dotnet build` — verify the project compiles
- `dotnet test` — run tests (if test project exists)
- `podman compose up` — run full stack locally (app + database)
- `podman build -t <image> .` — build container image
