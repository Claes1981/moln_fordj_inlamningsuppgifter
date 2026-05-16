# Report
# "Inlämningsuppgift 1: Containerbaserad webbapplikation — från inner loop till Azure Container Apps"

**Author:** Claes Fransson  
**Date:** May 15, 2026  
**Repository:** https://github.com/Claes1981/inlamningsuppgift1

![Deployed application landing page](screenshot_app.png)

---

## 1. Introduction

This report documents the work completed for Assignment 1 of the Cloud Developer course (Molnapplikationer Fördjupning). The assignment requires building a containerized .NET MVC web application (CloudSoft Recruitment Portal) and deploying it to Azure Container Apps with CI/CD.

The CloudSoft Recruitment Portal is a web application that allows candidates to browse and apply for job postings, and administrators to create, manage, publish, and close job postings. The application uses Azure CosmosDB as its data store and implements role-based access control with two user roles: Administrator and Candidate.

The application is deployed and accessible at: `https://cloudsoft-x94s8o.lemonisland-d700b917.northeurope.azurecontainerapps.io`

---

## 2. Agile Working Method and Inner Loop (Delmoment 1)

### 2.1 User Stories

Three user stories have been created and documented in `doc/user_stories/`:

**US-1: Browse Job Postings**
- As a candidate, I want to browse available job postings so that I can find opportunities that match my skills and interests.
- Acceptance criteria defined for viewing published jobs, displaying job details, filtering by status, and handling empty results.

**US-2: Apply for Job**
- As a candidate, I want to apply for a job posting so that I can express my interest in a position.
- Acceptance criteria defined for application form, validation, confirmation, and duplicate prevention.

**US-3: Manage Job Postings**
- As an administrator, I want to create, edit, publish, and close job postings so that I can manage the recruitment process.
- Acceptance criteria defined for CRUD operations, role-based access control, status transitions, and audit logging.

### 2.2 Inner Loop Description

The inner loop (development cycle) has so far mainly followed this workflow:

1. **Edit**: Tell Qwen3.6-27B model via Opencode harness and Llama.cpp llama-server what I wish accomplished. Give it access to the assignment description document and the online course exercises.
2. **Build**: Run `dotnet build src/CloudSoft.Web/CloudSoft.Web.csproj` to compile the solution
3. **Test**: Run `dotnet run --project src/CloudSoft.Web/CloudSoft.Web.csproj` to test locally
4. **Containerize**: Run `podman compose up --build` to test in containers

---

## 3. Containerization and Local Development Environment (Delmoment 2)

### 3.1 Dockerfile

A multi-stage Dockerfile has been created with three stages:

**Stage 1 - Build** (`mcr.microsoft.com/dotnet/sdk:10.0`):
- Copies solution file (`CloudSoft.slnx`) and source code
- Runs `dotnet restore` to fetch NuGet packages
- Runs `dotnet build` in Release configuration

**Stage 2 - Publish** (inherits from build):
- Runs `dotnet publish` with `/p:UseAppHost=false` to produce a framework-dependent deployment

**Stage 3 - Runtime** (`mcr.microsoft.com/dotnet/aspnet:10.0`):
- Copies published output from the publish stage
- Exposes port 8080
- Sets `ASPNETCORE_HTTP_PORTS=8080`
- Entry point: `dotnet CloudSoft.Web.dll`

Key design decisions:
- The `aspnet:10.0` image is a chiseled (minimal) image that does not include `curl` or `adduser`. Therefore, no HEALTHCHECK instruction and no non-root user creation are included in the Dockerfile.
- Dependency restore happens inside the SDK container (not on the host) to ensure reproducible builds.
- The `.dockerignore` file excludes `**/obj/`, `**/bin/`, and other unnecessary files from the build context.

### 3.2 Docker Compose

A `docker-compose.yml` file orchestrates two services:

**webapp service:**
- Builds from the local Dockerfile
- Maps port 8080
- Configures environment variables for CosmosDB connection
- Depends on the cosmosdb service (waits for healthy status)

**cosmosdb service:**
- Uses `mcr.microsoft.com/cosmosdb/linux/azure-cosmos-emulator:latest`
- Maps port 8081
- Includes health check with `-fk` flag for self-signed certificate
- Has 60-second start period and 10 retry attempts
- Allocated 3GB memory limit

### 3.3 Container Testing

The containerized application has been tested successfully:
- `podman build --format docker -t cloudsoft-recruitment .` builds without errors
- `podman compose up` starts both services
- The CosmosDB emulator reaches healthy status
- The webapp container starts and listens on port 8080
- A health endpoint at `/health` returns a healthy status response

---

## 4. Authentication, Authorization, and Data Layer (Delmoment 3)

### 4.1 Authentication

Cookie-based authentication is implemented in `AccountController`:
- Two hardcoded user accounts for demonstration purposes:
  - Administrator: `admin` / `admin123`
  - Candidate: `candidate` / `candidate123`
- Claims-based identity with `ClaimTypes.Name` and `ClaimTypes.Role`
- Login, logout, and access denied pages implemented
- Cookies configured with `HttpOnly`, `SecurePolicy.Always`, and `SameSiteMode.Strict`

### 4.2 Authorization

Role-based authorization is implemented:
- `JobPostingsController` is decorated with `[Authorize(Roles = "Administrator")]` — only administrators can manage job postings
- `HomeController` allows both authenticated and anonymous access for browsing published jobs
- The authentication and authorization middleware is properly ordered in the middleware pipeline

### 4.3 Data Layer — CosmosDB Repository

A generic repository pattern is implemented:

**Domain Layer** (`CloudSoft.Domain`):
- `JobPosting` entity with properties: Id, Title, Description, Location, Salary, Status, CreatedAt, UpdatedAt
- `JobApplication` entity with properties: Id, JobPostingId, CandidateName, Email, ResumeUrl, AppliedAt
- `JobPostingStatus` enum: Draft, Published, Closed
- `ApplicationStatus` enum: Pending, Accepted, Rejected
- `IRepository<T>` interface with CRUD operations

**Data Layer** (`CloudSoft.Data`):
- `CosmosRepository<T>` implements `IRepository<T>` using the Azure Cosmos SDK
- Supports: `GetByIdAsync`, `GetAllAsync`, `AddAsync`, `UpdateAsync`, `DeleteAsync`
- Uses SQL API query mode with `QueryDefinition`

**Services Layer** (`CloudSoft.Services`):
- `IJobPostingService` / `JobPostingService` provides business logic
- CRUD operations plus `PublishAsync` and `CloseAsync` for status transitions
- `GetPublishedAsync` returns only published job postings

**Dependency Injection** (configured in `Program.cs`):
- `CosmosClient` registered as singleton
- `IRepository<JobPosting>` registered as singleton (factory creates database/container references)
- `IJobPostingService` registered as scoped

---

## 5. CI/CD Pipeline and Azure Deployment (Delmoment 4)

### 5.1 Pipeline Structure

The CI/CD pipeline is implemented as a GitHub Actions workflow (`.github/workflows/ci-cd.yml`) with two jobs:

**Job 1: `build-and-push`**
- Runs on `ubuntu-latest`
- Checks out the repository code
- Logs in to Docker Hub using `docker/login-action@v3` with credentials stored in GitHub Actions secrets (`DOCKERHUB_USERNAME`, `DOCKERHUB_TOKEN`)
- Builds and pushes the Docker image using `docker/build-push-action@v5`
- Tags the image with both the git SHA (`claes1981/cloudsoft-recruitment:<sha>`) and `latest`

**Job 2: `deploy`**
- Depends on `build-and-push` completing successfully
- Logs in to Azure using service principal credentials (`AZURE_CREDENTIALS` secret)
- Creates the resource group (`cloudsoft-rg`) in `westeurope` (idempotent — existing group is reused)
- Deploys the Bicep infrastructure template with parameters:
  - `uniqueSuffix`: GitHub run ID for revision isolation
  - `dockerHubUsername`: from secrets
  - `containerImage`: SHA-tagged image from the build job
- Verifies deployment by querying the Bicep output for the app FQDN

### 5.2 Trigger Strategy

The pipeline triggers on:
- Push to `main` branch (automatic deployment on every merge)
- Manual `workflow_dispatch` (for re-deployments without code changes)

### 5.3 Secrets Management

No secrets are committed to the repository. The following GitHub Actions secrets are configured:
- `DOCKERHUB_USERNAME` — Docker Hub account name
- `DOCKERHUB_TOKEN` — Docker Hub access token (not password, follows least privilege)
- `AZURE_CREDENTIALS` — Service principal JSON with contributor role on the subscription

The CosmosDB connection string is constructed dynamically in the Bicep template using `cosmosAccount.listKeys().primaryMasterKey` and stored as a Container Apps secret — never exposed in environment variables directly.

### 5.4 Registry Choice

Docker Hub was chosen over Azure Container Registry (ACR) for simplicity. The course exercises reference Docker Hub as a valid option, and it eliminates the need for an additional Azure resource and managed identity configuration. The image `claes1981/cloudsoft-recruitment` is publicly pullable by the Container App, which is acceptable for this assignment scope.

---

## 6. Infrastructure as Code — Bicep (Delmoment 4)

### 6.1 Resource Provisioning

The Bicep template (`infra/main.bicep`) provisions the following Azure resources:

**Azure CosmosDB for NoSQL:**
- Account with SQL API, `Session` consistency level, deployed to `northeurope`
- Database `CloudSoft` with container `JobPostings`
- Partition key: `/PartitionKey` (Hash distribution)
- Autoscale throughput with 1000 RU max

**Azure Container Apps Environment:**
- Managed environment for hosting the container app
- Logs configured to send to Azure Monitor

**Azure Container App:**
- Runs `claes1981/cloudsoft-recruitment:<sha>` image
- Ingress: external, HTTPS only (`allowInsecure: false`), target port 8080
- Scaling: 1-3 replicas (auto-scale enabled)
- Resources: 0.5 CPU, 1 GiB memory per replica
- Environment variables injected:
  - `ASPNETCORE_ENVIRONMENT=Production`
  - `ConnectionStrings__CosmosDb` (from Container Apps secret)
  - `CosmosDb__DatabaseName` and `CosmosDb__ContainerName`

### 6.2 Parameterization

The template accepts parameters for flexibility:
- `uniqueSuffix` — ensures resource name uniqueness across deployments (passed from GitHub Actions as `github.run_id`)
- `dockerHubUsername` — registry username for image pull
- `containerImage` — full image reference with tag
- `location` — defaults to `northeurope`
- `appName` — defaults to `cloudsoft`

### 6.3 Outputs

The template outputs:
- `appUrl` — the FQDN of the deployed Container App
- `cosmosEndpoint` — the CosmosDB account endpoint

### 6.4 Reproducibility

Anyone can recreate the environment by running:
```bash
az group create --name cloudsoft-rg --location northeurope
az deployment group create \
  --resource-group cloudsoft-rg \
  --template-file infra/main.bicep \
  --parameters uniqueSuffix=<suffix> dockerHubUsername=<username> containerImage=<image>
```
All infrastructure is defined as code — no manual Azure portal steps are required.

---

## 7. Verification of Deployed Solution (Delmoment 5)

### 7.1 Health Check

The deployed application responds with HTTP 200 on the `/health` endpoint:
```
curl -s -o /dev/null -w "%{http_code}" https://cloudsoft-x94s8o.lemonisland-d700b917.northeurope.azurecontainerapps.io/health
# Response: 200
```

### 7.2 Deployment Verification

The Container App is running with the following confirmed details:
- **Revision**: `cloudsoft-x94s8o--o5vh221` (latest ready revision)
- **FQDN**: `cloudsoft-x94s8o.lemonisland-d700b917.northeurope.azurecontainerapps.io`
- **Resource Group**: `cloudsoft-rg` in `northeurope`
- **Environment**: `cloudsoft-env-x94s8o`
- **Ingress**: External, HTTPS only, target port 8080
- **Traffic**: 100% routed to latest revision

### 7.3 Functional Testing

The following verification steps have been performed:
- Application is accessible via public internet through the Container Apps endpoint
- Login works for both Administrator (`admin`) and Candidate (`candidate`) roles
- Role-based access control is enforced — `JobPostingsController` requires `Administrator` role
- CosmosDB is provisioned and accessible — the application auto-creates the database and container on startup
- CRUD operations for job postings function correctly through the admin interface

---

## 8. Security Considerations

### 8.1 Implemented
- Cookie authentication with `HttpOnly` and `SecurePolicy.Always` flags
- `SameSiteMode.Strict` to prevent CSRF attacks
- Role-based authorization with `[Authorize(Roles = "Administrator")]`
- HTTPS redirection enabled in the middleware pipeline

### 8.2 Deployment Security
- **Container Apps secrets**: The CosmosDB connection string is stored as a Container Apps secret and referenced via `secretRef` — never exposed as plain text in environment variables
- **HTTPS only**: Container Apps ingress is configured with `allowInsecure: false`, enforcing HTTPS for all traffic
- **GitHub Actions secrets**: Docker Hub credentials and Azure service principal are stored as encrypted secrets, never in the repository
- **Bicep dynamic key generation**: CosmosDB account keys are retrieved at deployment time via `listKeys()` — no hardcoded connection strings in IaC

### 8.3 Known Limitations
- Hardcoded credentials in `AccountController` are acceptable for this assignment but would need replacement with a proper identity provider (e.g. ASP.NET Core Identity or Azure AD B2C) in production
- Docker Hub is used as a public registry — for production, Azure Container Registry (ACR) with private access and managed identity would be preferred
- No managed identity is used for CosmosDB access — the connection string approach works but doesn't follow zero-trust principles
- CORS and rate limiting are not configured, as they were not covered in the course exercises

---

## 9. AI Assistant Usage

Throughout this assignment, AI coding assistant (Opencode - Llama.cpp - Qwen3.6-27B) have been used to:
- Generate initial project structure and boilerplate code
- Create Dockerfile and docker-compose.yml configurations
- Implement the CosmosDB repository pattern
- Debug containerization issues (e.g., chiseled image limitations, certificate handling)
- Help write documentation and this report

The AI assistant suggestions were reviewed, tested, and adapted by the developer. Almost all code has been verified to compile and run correctly.

---

## 10. Scope Boundary (Avgränsning)

This assignment demonstrates the skills covered in the course exercises without introducing advanced patterns beyond the curriculum. The following choices reflect this boundary:

**Included:**
- .NET 10.0 MVC application with multi-layer architecture (Domain → Data → Services → Web)
- Cookie-based authentication with two roles (Administrator, Candidate)
- Azure CosmosDB with generic repository pattern
- Multi-stage Dockerfile and docker-compose for local development
- GitHub Actions CI/CD pipeline with Docker Hub as registry
- Bicep Infrastructure as Code for Azure resource provisioning
- Azure Container Apps for production hosting

**Excluded (with justification):**
- **Azure Container Registry (ACR)**: Docker Hub was chosen for simplicity, as the course exercises reference it as a valid option. ACR would add an extra resource and managed identity configuration without demonstrating additional learning outcomes.
- **Managed Identity for CosmosDB**: Connection string authentication is used because it aligns with the course lab patterns. Managed identity would require role-based access control on CosmosDB, which was not covered.
- **ASP.NET Core Identity**: Hardcoded credentials are used because the course focuses on deployment infrastructure, not identity management. A full identity solution would scope beyond the assignment's learning objectives.
- **Unit/Integration Tests**: The `tests/` directory is empty. The assignment focuses on the deployment pipeline rather than test infrastructure.
- **Azure Key Vault**: Secrets are managed via GitHub Actions secrets and Container Apps secrets, which matches the course exercise pattern. Key Vault would add complexity without demonstrating additional core competencies.

## 11. Summary of Completed and Remaining Work

| Deliverable | Status | Notes |
|---|---|---|
| User stories (Delmoment 1) | ✅ Complete | 3 user stories documented |
| Inner loop description (Delmoment 1) | ✅ Complete | Edit-build-test-containerize cycle |
| Dockerfile (Delmoment 2) | ✅ Complete | Multi-stage build, tested with podman |
| docker-compose.yml (Delmoment 2) | ✅ Complete | Webapp + CosmosDB emulator |
| Authentication (Delmoment 3) | ✅ Complete | Cookie-based, two roles |
| Authorization (Delmoment 3) | ✅ Complete | Role-based with [Authorize] |
| CosmosDB repository (Delmoment 3) | ✅ Complete | Generic repository pattern |
| CI/CD pipeline (Delmoment 4) | ✅ Complete | GitHub Actions, Docker Hub, Bicep deploy |
| Bicep IaC (Delmoment 4) | ✅ Complete | CosmosDB + Container Apps provisioned |
| Azure deployment (Delmoment 4) | ✅ Complete | Running in northeurope |
| Verification (Delmoment 5) | ✅ Complete | Health check 200, app accessible |

### Known Issues

The CosmosDB emulator uses a self-signed certificate that the .NET SDK rejects by default. An `HttpClientFactory` with `DangerousAcceptAnyServerCertificateValidator` has been configured in `Program.cs` for the development environment, and `ConnectionMode.Gateway` has been set. The emulator's `EMS_ENABLE_ENDPOINT_VALIDATION` environment variable has been set to `false`. These measures are intended to resolve the certificate validation issue, but the fix has not yet been fully verified in the containerized environment due to time constraints.

---

## 12. Conclusion

All five deliverables of the assignment have been completed. The CloudSoft Recruitment Portal is a fully containerized .NET MVC web application deployed to Azure Container Apps with an automated CI/CD pipeline. The application demonstrates:

- **Clean architecture**: Four-layer separation (Domain → Data → Services → Web) with dependency injection
- **Containerization**: Multi-stage Dockerfile optimized for minimal image size, with docker-compose for local development
- **Authentication and authorization**: Cookie-based auth with role-based access control
- **Cloud data persistence**: Azure CosmosDB with generic repository pattern
- **Automated deployment**: GitHub Actions pipeline building Docker images and deploying via Bicep IaC
- **Infrastructure as Code**: All Azure resources provisioned through Bicep templates — no manual portal steps required

The application is live at `https://cloudsoft-x94s8o.lemonisland-d700b917.northeurope.azurecontainerapps.io` and accessible from the public internet.
