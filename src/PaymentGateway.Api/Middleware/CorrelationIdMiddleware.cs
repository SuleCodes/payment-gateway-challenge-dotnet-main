using System.Text.RegularExpressions;

namespace PaymentGateway.Api.Middleware;

/// <summary>
/// Ensures every request has a correlation id so one merchant call can be traced across all log lines
/// (and, later, across the call to the bank).
/// Uses the caller's <c>X-Correlation-Id</c> if present and well-formed, otherwise generates one.
/// The id is echoed in the response header and added to the logging scope for the whole request.
/// </summary>
public partial class CorrelationIdMiddleware
{
    public const string HeaderName = "X-Correlation-Id";

    private readonly RequestDelegate _next;
    private readonly ILogger<CorrelationIdMiddleware> _logger;

    public CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = GetOrCreate(context.Request);

        context.TraceIdentifier = correlationId;
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[HeaderName] = correlationId;
            return Task.CompletedTask;
        });

        using (_logger.BeginScope("CorrelationId:{CorrelationId}", correlationId))
        {
            await _next(context);
        }
    }

    private static string GetOrCreate(HttpRequest request)
    {
        var incoming = request.Headers[HeaderName].ToString();

        // Only trust short, plain values: the id is written to logs and response headers,
        // so arbitrary input would allow log injection or oversized headers.
        return SafeValue().IsMatch(incoming) ? incoming : Guid.NewGuid().ToString();
    }

    [GeneratedRegex("^[A-Za-z0-9._-]{1,64}$")]
    private static partial Regex SafeValue();
}
