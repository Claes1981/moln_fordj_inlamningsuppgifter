# Inlämningsuppgift 2 – Rapport

**Kurs:** Molnapplikationer Fördjupning  
**Student:** Claes  
**Datum:** 2026-05-27  
**Gren:** `feature/inlamningsuppgift2`

---

## Delmoment 1 – Observerbarhet (Structured Logging & Correlation ID)

### Implementering

**CorrelationIdMiddleware** (`src/CloudSoft.Web/Middleware/CorrelationIdMiddleware.cs`):

- Genererar en unik `X-Correlation-ID` (GUID) för varje inkommande begäran.
- Återanvänds befintlig korrelations-ID om klienten skickar `X-Correlation-ID` i headern.
- Lägger till korrelations-ID:t i loggnings-scope så att alla loggmeddelanden automatiskt kopplas till begäran.
- Placerad tidigt i middleware-pipelinen (efter exception handling) för maximal täckning.

**Strukturerad loggning** (meddelandemallar + `ILogger<T>`):

- `AccountController` och `JobPostingsController` uppgraderade från `Console.WriteLine` till `ILogger<T>` med meddelandemallar.
- Exempel: `_logger.LogInformation("User logged in: {Username}", model.Username);`
- Loggningsnivåer valda medvetet: `Information` för normal drift, `Warning` för misstänkta mönster, `Error` för fel.

**JSON-konsolloggning** (`appsettings.json`):

- `IncludeScopes: true` gör att korrelations-ID och andra scope-variabler medföljer varje loggpost.
- Standardnivå: `Warning` för rotkategorin, `Information` för `CloudSoft` och `Microsoft.AspNetCore`.
- JSON-format ger strukturerad output som Azure Monitor och andra loggsystem kan parsas automatiskt.

### Varför detta mönster?

Korrelations-ID är industristandard för distributed tracing. I en mikrotjänstarkitektur (eller Container Apps med flera repliker) gör det möjligt att följa en begäran genom hela systemet. Utan korrelations-ID är loggning nästan oanvändbar i produktion.

---

## Delmoment 2 – REST API (DTOs, Swagger, API Key Middleware)

### Implementering

**DTO-mönstret** (`src/CloudSoft.Web/Dtos/`):

- `JobPostingDto` — indata för POST/PUT (användarangivna fält).
- `JobPostingOutputDto` — utdata för GET (inkluderar servergenererade fält: `Id`, `CreatedAt`, `UpdatedAt`).
- Separation mellan API-gränssnitt och interna entiteter: klienter kan aldrig se eller ändra interna detaljer.

**ApiJobPostingsController** (`src/CloudSoft.Web/Controllers/ApiJobPostingsController.cs`):

- `[ApiController]`-attributet ger automatisk validering, binding, och felhantering.
- CRUD-operationer: `GET /api/JobPostings`, `GET /api/JobPostings/{id}`, `POST /api/JobPostings`, `PUT /api/JobPostings/{id}`, `DELETE /api/JobPostings/{id}`.
- `PATCH`-operationer för publish/close via dedikerade endpoints.
- Returnerar semantiska HTTP-statuskoder: 200, 201, 204, 400, 404.

**Swagger/OpenAPI**:

- Swagger UI tillgänglig på `/swagger` (alltid aktiverad, även i produktion, för API-dokumentation).
- `ApiKeyMiddleware` hoppar över Swagger-rutter för att möjliggöra test utan API-nyckel.

**ApiKeyMiddleware** (`src/CloudSoft.Web/Middleware/ApiKeyMiddleware.cs`):

- Validerar `X-API-Key`-headern mot konfigurerade nycklar (`ApiKey:Keys`).
- Returnerar 401 (Unauthorized) om headern saknas, 403 (Forbidden) om nyckeln är ogiltig.
- Skyddar alla `/api/*`-rutter. MVC-rutter påverkas inte.

### Varför detta mönster?

DTO-mönstret är en grundläggande arkitekturmönster som förhindrar att domänentiteter läcker ut till API-gränssnittet. API Key-middleware är en enkel men effektiv autentiseringsmekanism för service-to-service-kommunikation som täcks i kursens övningar.

---

## Delmoment 3 – Filuppladdning och Health Probes

### Filuppladdning (Azure Blob Storage)

**IBlobService** (`src/CloudSoft.Domain/IBlobService.cs`):

- Gränssnitt med två metoder: `UploadAsync` och `IsAvailableAsync`.
- Möjliggör beroendeinjektion och testbarhet.

**AzureBlobService** (`src/CloudSoft.Data/AzureBlobService.cs`):

- Använder `Azure.Storage.Blobs`-biblioteket.
- Autentisering via Managed Identity (`BlobServiceClient` med bara account URL, ingen connection string).
- Fallback till connection string om den finns konfigurerad (för lokal utveckling).

**NoOpBlobService** (`src/CloudSoft.Data/NoOpBlobService.cs`):

- No-operation implementation för lokal utveckling utan Azure Storage.
- Alla metoder returnerar omedelbart utan att göra något.

**ResumeUploadController** (`src/CloudSoft.Web/Controllers/ResumeUploadController.cs`):

- PDF-validering: kontrollerar MIME-type (`application/pdf`) och filsignatur ( `%PDF-` magic bytes).
- Maxstorlek: 10 MB.
- Unikt filnamn: `{Guid}_{originalFileName}` för att undvida kollisioner.
- Synkron upload (inom 5-sekunders timeout) — godtagbart för filer under 10 MB.

**Villkorlig DI** (`Program.cs`):

- `AzureBlobService` registreras endast om `BlobStorage`-konfiguration finns.
- Annars används `NoOpBlobService` automatiskt.

### Health Probes (Deep Probes)

**Tre endpoints** (via `MapHealthChecks` i `Program.cs`):

| Endpoint | Syfte | Kontroll |
|---|---|---|
| `/health/live` | Liveness probe — process alive? | Inga kontroller (returnerar 200 om processen svarar) |
| `/health/ready` | Readiness probe — startup komplett? | Kontroller taggade `"ready"` (CosmosDB + Blob Storage) |
| `/health` | Detaljerad diagnostik | Alla kontroller med JSON-svar |

**CosmosHealthCheck** (`src/CloudSoft.Web/HealthChecks/CosmosHealthCheck.cs`):

- Utför `ReadContainerAsync` för att verifiera faktisk connectivity.
- 5-sekunders timeout med `CancellationTokenSource`.
- Returnerar `Degraded` vid timeout, `Unhealthy` vid fel.

**BlobHealthCheck** (`src/CloudSoft.Web/HealthChecks/BlobHealthCheck.cs`):

- Använder `IBlobService.IsAvailableAsync` för att verifiera Blob Storage.
- 5-sekunders timeout med `CancellationTokenSource`.
- Returnerar `Degraded` vid timeout, `Unhealthy` vid fel.

### Bugfix: Rekursivt OperationCanceledException

Båda health checks hade en kritisk bug där `catch (OperationCanceledException)` anropade `CheckHealthAsync` rekursivt, vilket ledde till oändlig rekursion. Fixad genom att returnera `HealthCheckResult.Degraded()` vid timeout.

---

## Delmoment 4 – Arkitekturgranskning

### Bicep-uppdateringar (`infra/main.bicep`)

**Blob Storage-provisionering:**

- Storage Account (`Standard_LRS`, `StorageV2`) med `allowSharedKeyAccess: false` (kräver Managed Identity).
- Blob container `resumes` med `publicAccess: None` (ingen anonym åtkomst).

**Managed Identity:**

- Container App får `SystemAssigned` managed identity.
- `CosmosDB Built-in Data Contributor`-roll på CosmosDB-kontot.
- `Storage Blob Data Contributor`-roll på Storage Account.
- Rolltilldelningar använder implicita beroenden via `containerApp.identity.principalId`.

**Container App-konfiguration:**

- `BlobStorage__AccountUrl` environment variable med Blob Storage endpoint.
- CosmosDB connection string fortfarande via secret (Managed Identity för CosmosDB kräver att appen byter från connection string till token-baserad auth, vilket är en större ändring).

**Säkerhetsförbättringar:**

- `disableLocalAuth: true` på CosmosDB (kräver Managed Identity eller RBAC).
- `transport: 'auto'` på Container Apps ingress (tvingar TLS).
- Storage Account med `allowSharedKeyAccess: false` (kräver Managed Identity).

### CI/CD (`.github/workflows/ci-cd.yml`)

- Smoke test uppdaterad att använda `/health/live` (liveness probe utan beroendekontroller).
- `RESOURCE_GROUP` och `LOCATION` parametriserade som miljövariabler.
- Location korrigerad till `northeurope` (matchar befintlig resource group).

---

## Lektioner & Reflektioner

### Managed Identity vs. Connection Strings

Det viktigaste lärdomarna från denna uppgift:

1. **Managed Identity är ett måste i produktion.** Connection strings innehåller hemliga nycklar som måste roteras och skyddas. Managed Identity eliminerar detta problem helt.

2. **Bicep rolltilldelningar har en tidsskillnad.** Efter deployment kan det ta 5-10 minuter innan rolltilldelningar propagerar. Detta är en känd Azure-begränsning.

3. **Health probes ska vara snabba och pålitliga.** `/health/live` ska aldrig göra externa anrop — det ska bara verifiera att processen lever. `/health/ready` kan göra djupare kontroller men ska ha kort timeout.

4. **DTO-mönstret är en enkel men kraftfull separation.** Genom att aldrig exponera domänentiteter direkt i API-svar minskar vi risken för oavsiktlig informationsläckage och gör API-et mer stabilt över tid.

### Vad jag skulle göra annorlunda

- Använd `MapHealthChecks` från början istället för en custom `HealthController`. ASP.NET Core:s inbyggda health check-system är vältestat och välunderhållet.
- Implementera Strict TDD med tests från början, inte som eftertanke.
- Använd `parent`-syntax i Bicep för att undvika `use-parent-property`-varningar.

---

## Källor

- Kursens övningar: <https://cloud-dev-25.educ8.se/exercises/>
  - `3-deployment/10-logging-and-monitoring/1-structured-logging-ilogger/`
  - `4-services-and-apis/1-rest-api-and-dtos/1-rest-controllers-and-dtos/`
  - `4-services-and-apis/1-rest-api-and-dtos/3-api-key-middleware/`
  - `6-storage-and-resilience/1-uploads-and-deep-probes/1-mvc-uploads-and-pdf-validation/`
  - `6-storage-and-resilience/1-uploads-and-deep-probes/2-cosmos-and-blob-via-managed-identity/`
  - `6-storage-and-resilience/1-uploads-and-deep-probes/3-deep-health-probes-and-cleanup/`
