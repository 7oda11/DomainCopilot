using System.Text.RegularExpressions;

namespace DomainCopilot.Api.Middleware;

public sealed partial class CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
{
    private const string HeaderName = "X-Correlation-ID";

    public async Task InvokeAsync(HttpContext context)
    {
        var supplied = context.Request.Headers[HeaderName].FirstOrDefault();
        var correlationId = supplied is not null && IsValidCorrelationId().IsMatch(supplied)
            ? supplied
            : context.TraceIdentifier;
        context.TraceIdentifier = correlationId;
        context.Response.Headers[HeaderName] = correlationId;
        using (logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = correlationId }))
        {
            await next(context);
        }
    }

    [GeneratedRegex("^[A-Za-z0-9-]{1,64}$", RegexOptions.CultureInvariant)]
    private static partial Regex IsValidCorrelationId();
}
