using Microsoft.AspNetCore.Http;
using Serilog;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Application.Middlewares
{
    public class RequestLoggingMiddleware
    {
        private readonly RequestDelegate _next;

        public RequestLoggingMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            var request = context.Request;
            var clientIp = context.Connection.RemoteIpAddress?.ToString();
            var httpMethod = request.Method;
            var endpoint = request.Path;

            var clientId = context.User?.Claims
                ?.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub || c.Type == ClaimTypes.NameIdentifier)
                ?.Value ?? "anonymous";

            var originalBody = context.Response.Body;
            using var newBody = new MemoryStream();
            context.Response.Body = newBody;

            await _next(context);

            stopwatch.Stop();

            context.Response.Body.Seek(0, SeekOrigin.Begin);
            var responseBody = new StreamReader(context.Response.Body).ReadToEnd();
            context.Response.Body.Seek(0, SeekOrigin.Begin);

            Log.Information("HTTP {Method} {Endpoint} responded {StatusCode} in {Elapsed}ms | ClientIP: {IP} | ClientId: {ClientId}",
                httpMethod, endpoint, context.Response.StatusCode, stopwatch.ElapsedMilliseconds, clientIp, clientId);

            await newBody.CopyToAsync(originalBody);
        }
    }
}
