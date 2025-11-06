using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// Configure HTTPS
builder.WebHost.UseUrls("https://localhost:5001");

// Add CORS services
builder.Services.AddCors();

var app = builder.Build();

// Enable HTTPS redirection
app.UseHttpsRedirection();

// CORS for testing
app.UseCors(policy => policy
    .AllowAnyOrigin()
    .AllowAnyMethod()
    .AllowAnyHeader());

// Helper method for delayed responses
static async Task<IResult> DelayedResponse(int statusCode, object? body = null, int delayMs = 0)
{
    if (delayMs > 0)
    {
        await Task.Delay(delayMs);
    }
    return Results.Json(body ?? new { }, statusCode: statusCode);
}

// Helper to get status from header or query parameter
static string GetStatus(HttpContext context)
{
    // Check header first (preferred for integration tests)
    if (context.Request.Headers.TryGetValue("X-Test-Status", out var headerStatus))
    {
        return headerStatus.ToString();
    }
    // Check query parameter
    var status = context.Request.Query["status"].ToString();
    if (!string.IsNullOrEmpty(status))
    {
        return status;
    }
    // Check if status is in the referer URL (for base URL with query params)
    var referer = context.Request.Headers.Referer.ToString();
    if (!string.IsNullOrEmpty(referer) && Uri.TryCreate(referer, UriKind.Absolute, out var refererUri))
    {
        var query = System.Web.HttpUtility.ParseQueryString(refererUri.Query);
        return query["status"] ?? "";
    }
    return "";
}

static int GetDelay(HttpContext context)
{
    if (context.Request.Headers.TryGetValue("X-Test-Delay", out var headerDelay))
    {
        if (int.TryParse(headerDelay.ToString(), out var headerValue))
            return headerValue;
    }
    return int.TryParse(context.Request.Query["delay"].ToString(), out var queryValue) ? queryValue : 0;
}

static string GetRetryAfter(HttpContext context)
{
    if (context.Request.Headers.TryGetValue("X-Test-Retry-After", out var headerRetry))
    {
        return headerRetry.ToString();
    }
    return context.Request.Query["retryAfter"].ToString();
}

// Customer endpoint - POST /v3/customer
app.MapPost("/v3/customer", async (HttpContext context) =>
{
    var status = GetStatus(context);
    var delay = GetDelay(context);
    var retryAfter = GetRetryAfter(context);

    // Handle rate limiting with custom headers
    if (status == "429")
    {
        if (delay > 0) await Task.Delay(delay);
        
        var response = Results.Json(new { message = "Rate limit exceeded", code = "RATE_LIMIT" }, statusCode: 429);
        
        // Set Retry-After header manually
        if (!string.IsNullOrEmpty(retryAfter))
        {
            context.Response.Headers["Retry-After"] = retryAfter;
        }
        
        return response;
    }

    // For integration tests, we'll use a simple approach:
    // - If no status header, return success
    // - If status header is provided, return that status
    if (string.IsNullOrEmpty(status))
    {
        return await DelayedResponse(200, new { 
            customer = new { 
                id = "cust_" + Guid.NewGuid().ToString("N")[..8], 
                email = "install@example.com",
                name = "Test Installation"
            } 
        }, delay);
    }
    
    return status switch
    {
        "401" => await DelayedResponse(401, new { message = "Unauthorized", code = "AUTH" }, delay),
        "403" => await DelayedResponse(403, new { message = "Forbidden", code = "FORBIDDEN" }, delay),
        "404" => await DelayedResponse(404, new { message = "Not Found", code = "NOT_FOUND" }, delay),
        "400" => await DelayedResponse(400, new { message = "Bad Request", code = "INVALID_REQUEST" }, delay),
        "500" => await DelayedResponse(500, new { message = "Internal Server Error", code = "SERVER_ERROR" }, delay),
        "502" => await DelayedResponse(502, new { message = "Bad Gateway" }, delay),
        "503" => await DelayedResponse(503, new { message = "Service Unavailable" }, delay),
        "504" => await DelayedResponse(504, new { message = "Gateway Timeout" }, delay),
        _ => await DelayedResponse(200, new { 
            customer = new { 
                id = "cust_" + Guid.NewGuid().ToString("N")[..8], 
                email = "install@example.com",
                name = "Test Installation"
            } 
        }, delay)
    };
});

// Instance endpoint - POST /v3/instance
app.MapPost("/v3/instance", async (HttpContext context) =>
{
    var status = GetStatus(context);
    var delay = GetDelay(context);

    return status switch
    {
        "401" => await DelayedResponse(401, new { message = "Unauthorized", code = "AUTH" }, delay),
        "403" => await DelayedResponse(403, new { message = "Forbidden", code = "FORBIDDEN" }, delay),
        "404" => await DelayedResponse(404, new { message = "Not Found", code = "NOT_FOUND" }, delay),
        "429" => await DelayedResponse(429, new { message = "Rate limit exceeded", code = "RATE_LIMIT" }, delay),
        "400" => await DelayedResponse(400, new { message = "Bad Request", code = "INVALID_REQUEST" }, delay),
        "500" => await DelayedResponse(500, new { message = "Internal Server Error", code = "SERVER_ERROR" }, delay),
        "502" => await DelayedResponse(502, new { message = "Bad Gateway" }, delay),
        "503" => await DelayedResponse(503, new { message = "Service Unavailable" }, delay),
        "504" => await DelayedResponse(504, new { message = "Gateway Timeout" }, delay),
        "" or null => await DelayedResponse(200, new { 
            instance_id = "inst_" + Guid.NewGuid().ToString("N")[..8] 
        }, delay),
        _ => await DelayedResponse(500, new { message = $"Unknown status: {status}" }, delay)
    };
});

// Metrics endpoint - POST /application/custom-metrics
app.MapPost("/application/custom-metrics", async (HttpContext context) =>
{
    var status = GetStatus(context);
    var delay = GetDelay(context);

    return status switch
    {
        "401" => await DelayedResponse(401, new { message = "Unauthorized", code = "AUTH" }, delay),
        "403" => await DelayedResponse(403, new { message = "Forbidden", code = "FORBIDDEN" }, delay),
        "404" => await DelayedResponse(404, new { message = "Not Found", code = "NOT_FOUND" }, delay),
        "429" => await DelayedResponse(429, new { message = "Rate limit exceeded", code = "RATE_LIMIT" }, delay),
        "400" => await DelayedResponse(400, new { message = "Bad Request", code = "INVALID_REQUEST" }, delay),
        "500" => await DelayedResponse(500, new { message = "Internal Server Error", code = "SERVER_ERROR" }, delay),
        "502" => await DelayedResponse(502, new { message = "Bad Gateway" }, delay),
        "503" => await DelayedResponse(503, new { message = "Service Unavailable" }, delay),
        "504" => await DelayedResponse(504, new { message = "Gateway Timeout" }, delay),
        "" or null => await DelayedResponse(200, new { ok = true }, delay),
        _ => await DelayedResponse(500, new { message = $"Unknown status: {status}" }, delay)
    };
});

// Instance info endpoint - GET /kots_metrics/license_instance/info
app.MapGet("/kots_metrics/license_instance/info", async (HttpContext context) =>
{
    var status = GetStatus(context);
    var delay = GetDelay(context);

    return status switch
    {
        "401" => await DelayedResponse(401, new { message = "Unauthorized", code = "AUTH" }, delay),
        "403" => await DelayedResponse(403, new { message = "Forbidden", code = "FORBIDDEN" }, delay),
        "404" => await DelayedResponse(404, new { message = "Not Found", code = "NOT_FOUND" }, delay),
        "429" => await DelayedResponse(429, new { message = "Rate limit exceeded", code = "RATE_LIMIT" }, delay),
        "500" => await DelayedResponse(500, new { message = "Internal Server Error", code = "SERVER_ERROR" }, delay),
        "502" => await DelayedResponse(502, new { message = "Bad Gateway" }, delay),
        "503" => await DelayedResponse(503, new { message = "Service Unavailable" }, delay),
        "504" => await DelayedResponse(504, new { message = "Gateway Timeout" }, delay),
        "" or null => await DelayedResponse(200, new { 
            instance_id = "inst_" + Guid.NewGuid().ToString("N")[..8],
            status = "running",
            version = "1.0.0"
        }, delay),
        _ => await DelayedResponse(500, new { message = $"Unknown status: {status}" }, delay)
    };
});

// Health check endpoint
app.MapGet("/health", () => Results.Json(new { status = "healthy", timestamp = DateTime.UtcNow }));

// Root endpoint with usage instructions
app.MapGet("/", () => Results.Json(new
{
    message = "Replicated API Mock Server",
    version = "1.0.0",
    endpoints = new[]
    {
        "POST /v3/customer?status={code}&delay={ms}&retryAfter={seconds}",
        "POST /v3/instance?status={code}&delay={ms}",
        "POST /application/custom-metrics?status={code}&delay={ms}",
        "GET /kots_metrics/license_instance/info?status={code}&delay={ms}",
        "GET /health"
    },
    statusCodes = new[] { "200", "400", "401", "403", "404", "429", "500", "502", "503", "504" },
    examples = new[]
    {
        "POST /v3/customer?status=401",
        "POST /v3/customer?status=429&retryAfter=60",
        "POST /v3/instance?status=500&delay=2000"
    }
}));

Console.WriteLine("🚀 Replicated Mock Server starting...");
Console.WriteLine("📍 URL: https://localhost:5001");
Console.WriteLine("🔧 Usage: Add ?status=401&delay=1000 to any endpoint");
Console.WriteLine("📖 Documentation: GET /");

app.Run();