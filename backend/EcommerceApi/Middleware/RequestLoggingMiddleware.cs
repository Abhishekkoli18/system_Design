using System.Diagnostics;
using EcommerceApi.Services.Logging;

namespace EcommerceApi.Middleware
{
    public class RequestLoggingMiddleware
    {
        // RequestDelegate represents "the rest of the pipeline" - everything
        // that comes after this middleware (Auth, Authorization, the Controller, etc.)
        private readonly RequestDelegate _next;

        public RequestLoggingMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        // This method runs for EVERY single request. "context" is the incoming
        // request AND the outgoing response, both wrapped in one object.
        public async Task InvokeAsync(HttpContext context, IApplicationLogger appLogger)
        {
            // ---- BEFORE the request continues down the pipeline ----
            var correlationId = Guid.NewGuid().ToString().Substring(0, 8); // short unique ID
            var startTime = DateTime.Now;
            var stopwatch = Stopwatch.StartNew();

            var method = context.Request.Method;
            var path = context.Request.Path;
            var queryString = context.Request.QueryString.ToString();
            var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

            // Attach the correlation ID to the response headers too, so the
            // frontend (or Postman/Swagger) could read it back if needed.
            context.Response.Headers["X-Correlation-ID"] = correlationId;

            // ---- Let the request continue through the ENTIRE rest of the pipeline ----
            // Authentication, Authorization, routing, the Controller method,
            // the database call, everything happens here, inside this one line.
            await _next(context);

            // ---- AFTER the response is ready, execution comes back here ----
            stopwatch.Stop();
            var statusCode = context.Response.StatusCode;
            var duration = stopwatch.ElapsedMilliseconds;

            var logLine =
                $"CorrelationId: {correlationId} | " +
                $"{method} {path}{queryString} | " +
                $"Status: {statusCode} | " +
                $"Duration: {duration}ms | " +
                $"IP: {clientIp} | " +
                $"Started: {startTime:yyyy-MM-dd HH:mm:ss}";

            await appLogger.Log(logLine);
        }
    }
}