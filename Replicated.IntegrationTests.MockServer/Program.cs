using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseUrls("https://localhost:5001");

builder.Services.AddCors();

var app = builder.Build();

app.UseHttpsRedirection();

app.UseCors(policy => policy
    .AllowAnyOrigin()
    .AllowAnyMethod()
    .AllowAnyHeader());

// ── Helpers ───────────────────────────────────────────────────────────────────

static string GetStatus(HttpContext context)
{
    if (context.Request.Headers.TryGetValue("X-Test-Status", out var headerStatus))
        return headerStatus.ToString();
    var status = context.Request.Query["status"].ToString();
    return string.IsNullOrEmpty(status) ? "" : status;
}

static int GetDelay(HttpContext context)
{
    if (context.Request.Headers.TryGetValue("X-Test-Delay", out var headerDelay) &&
        int.TryParse(headerDelay.ToString(), out var headerValue))
        return headerValue;
    return int.TryParse(context.Request.Query["delay"].ToString(), out var queryValue) ? queryValue : 0;
}

static string GetRetryAfter(HttpContext context)
{
    if (context.Request.Headers.TryGetValue("X-Test-Retry-After", out var headerRetry))
        return headerRetry.ToString();
    return context.Request.Query["retryAfter"].ToString();
}

static async Task<IResult> ErrorResponse(HttpContext context, string defaultStatus, object successBody)
{
    var status = GetStatus(context);
    var delay = GetDelay(context);
    if (delay > 0) await Task.Delay(delay);

    if (status == "429")
    {
        var retryAfter = GetRetryAfter(context);
        if (!string.IsNullOrEmpty(retryAfter))
            context.Response.Headers["Retry-After"] = retryAfter;
        return Results.Json(new { message = "Rate limit exceeded", code = "RATE_LIMIT" }, statusCode: 429);
    }

    if (!string.IsNullOrEmpty(status) && int.TryParse(status, out var code) && code != 200)
    {
        return code switch
        {
            401 => Results.Json(new { message = "Unauthorized", code = "AUTH" }, statusCode: 401),
            403 => Results.Json(new { message = "Forbidden", code = "FORBIDDEN" }, statusCode: 403),
            404 => Results.Json(new { message = "Not Found", code = "NOT_FOUND" }, statusCode: 404),
            400 => Results.Json(new { message = "Bad Request", code = "INVALID_REQUEST" }, statusCode: 400),
            500 => Results.Json(new { message = "Internal Server Error", code = "SERVER_ERROR" }, statusCode: 500),
            502 => Results.Json(new { message = "Bad Gateway" }, statusCode: 502),
            503 => Results.Json(new { message = "Service Unavailable" }, statusCode: 503),
            504 => Results.Json(new { message = "Gateway Timeout" }, statusCode: 504),
            _ => Results.Json(new { message = $"Unexpected status: {status}" }, statusCode: 500)
        };
    }

    return Results.Json(successBody);
}

// ── GET /api/v1/app/info ──────────────────────────────────────────────────────

app.MapGet("/api/v1/app/info", async (HttpContext context) =>
    await ErrorResponse(context, "200", new
    {
        instanceID = "inst_" + Guid.NewGuid().ToString("N")[..8],
        appSlug = "test-app",
        appName = "Test Application",
        appStatus = "ready",
        helmChartURL = (string?)null,
        currentRelease = new
        {
            versionLabel = "1.0.0",
            channelName = "Stable",
            createdAt = "2026-01-01T00:00:00Z",
            releaseNotes = "Initial release"
        }
    }));

// ── GET /api/v1/app/status ────────────────────────────────────────────────────

app.MapGet("/api/v1/app/status", async (HttpContext context) =>
    await ErrorResponse(context, "200", new
    {
        updatedAt = DateTime.UtcNow.ToString("o"),
        sequence = 1L,
        resources = Array.Empty<object>()
    }));

// ── GET /api/v1/app/updates ───────────────────────────────────────────────────

app.MapGet("/api/v1/app/updates", async (HttpContext context) =>
    await ErrorResponse(context, "200", Array.Empty<object>()));

// ── GET /api/v1/app/history ───────────────────────────────────────────────────

app.MapGet("/api/v1/app/history", async (HttpContext context) =>
    await ErrorResponse(context, "200", new[]
    {
        new { versionLabel = "1.0.0", createdAt = "2026-01-01T00:00:00Z", releaseNotes = "Initial release" }
    }));

// ── POST /api/v1/app/custom-metrics ──────────────────────────────────────────

app.MapPost("/api/v1/app/custom-metrics", async (HttpContext context) =>
    await ErrorResponse(context, "200", new { }));

// ── PATCH /api/v1/app/custom-metrics ─────────────────────────────────────────

app.MapMethods("/api/v1/app/custom-metrics", new[] { "PATCH" }, async (HttpContext context) =>
    await ErrorResponse(context, "200", new { }));

// ── DELETE /api/v1/app/custom-metrics/{metricName} ───────────────────────────

app.MapDelete("/api/v1/app/custom-metrics/{metricName}", async (HttpContext context) =>
    await ErrorResponse(context, "200", new { }));

// ── POST /api/v1/app/instance-tags ───────────────────────────────────────────

app.MapPost("/api/v1/app/instance-tags", async (HttpContext context) =>
    await ErrorResponse(context, "200", new { }));

// ── GET /api/v1/license/info ──────────────────────────────────────────────────

app.MapGet("/api/v1/license/info", async (HttpContext context) =>
    await ErrorResponse(context, "200", new
    {
        licenseID = "lic_" + Guid.NewGuid().ToString("N")[..8],
        licenseType = "prod",
        customerName = "Test Customer",
        customerEmail = "test@example.com",
        channelName = "Stable",
        entitlements = Array.Empty<object>()
    }));

// ── GET /api/v1/license/fields ────────────────────────────────────────────────

app.MapGet("/api/v1/license/fields", async (HttpContext context) =>
    await ErrorResponse(context, "200", new[]
    {
        new { name = "num-seats", description = "Number of seats", value = "10", signature = (string?)null }
    }));

// ── GET /api/v1/license/fields/{fieldName} ────────────────────────────────────

app.MapGet("/api/v1/license/fields/{fieldName}", async (HttpContext context) =>
{
    var fieldName = context.Request.RouteValues["fieldName"]?.ToString() ?? "unknown";
    return await ErrorResponse(context, "200", new
    {
        name = fieldName,
        description = $"License field: {fieldName}",
        value = "10",
        signature = (string?)null
    });
});

// ── Utility ───────────────────────────────────────────────────────────────────

app.MapGet("/health", () => Results.Json(new { status = "healthy", timestamp = DateTime.UtcNow }));

app.MapGet("/", () => Results.Json(new
{
    message = "Replicated In-Cluster API Mock Server",
    version = "1.0.0",
    endpoints = new[]
    {
        "GET  /api/v1/app/info",
        "GET  /api/v1/app/status",
        "GET  /api/v1/app/updates",
        "GET  /api/v1/app/history",
        "POST /api/v1/app/custom-metrics",
        "PATCH /api/v1/app/custom-metrics",
        "DELETE /api/v1/app/custom-metrics/{metricName}",
        "POST /api/v1/app/instance-tags",
        "GET  /api/v1/license/info",
        "GET  /api/v1/license/fields",
        "GET  /api/v1/license/fields/{fieldName}",
        "GET  /health"
    },
    behaviorControl = new
    {
        statusHeader = "X-Test-Status: 401|403|404|429|400|500|502|503|504",
        delayHeader = "X-Test-Delay: <milliseconds>",
        retryAfterHeader = "X-Test-Retry-After: <seconds> (used with 429)"
    }
}));

Console.WriteLine("Replicated Mock Server starting...");
Console.WriteLine("URL: https://localhost:5001");
Console.WriteLine("Docs: GET /");

app.Run();
