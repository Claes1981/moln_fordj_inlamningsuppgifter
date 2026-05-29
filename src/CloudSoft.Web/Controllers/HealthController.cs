namespace CloudSoft.Web.Controllers;

/// <summary>
/// Health check endpoints are now handled by MapHealthChecks in Program.cs:
/// - /health/live: liveness probe (no checks, process alive = healthy)
/// - /health/ready: readiness probe (checks tagged "ready": CosmosDB + Blob Storage)
/// - /health: diagnostic endpoint with JSON breakdown for humans
///
/// This controller class is kept as a placeholder for potential future health-related actions.
/// </summary>
