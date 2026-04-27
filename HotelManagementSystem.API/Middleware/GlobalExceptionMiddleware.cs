using System.Net;
using System.Text.Json;
using HotelManagementSystem.Shared.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;

namespace HotelManagementSystem.API.Middleware
{
    /// <summary>
    /// Catches all unhandled exceptions and returns a RFC 7807 ProblemDetails response
    /// with a user-friendly title/detail. Stack traces are only exposed in Development.
    /// </summary>
    public class GlobalExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionMiddleware> _logger;
        private readonly IHostEnvironment _env;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public GlobalExceptionMiddleware(
            RequestDelegate next,
            ILogger<GlobalExceptionMiddleware> logger,
            IHostEnvironment env)
        {
            _next = next;
            _logger = logger;
            _env = env;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                await HandleAsync(context, ex);
            }
        }

        private async Task HandleAsync(HttpContext context, Exception ex)
        {
            var (status, title, detail) = ex switch
            {
                NotFoundException nf =>
                    ((int)HttpStatusCode.NotFound, "Resource not found", nf.Message),
                ValidationException ve =>
                    ((int)HttpStatusCode.BadRequest, "Validation failed", ve.Message),
                ConflictException ce =>
                    ((int)HttpStatusCode.Conflict, "Conflict", ce.Message),
                UnauthorizedAccessException =>
                    ((int)HttpStatusCode.Unauthorized, "Unauthorized",
                        "You are not authorized to perform this action."),
                _ => ((int)HttpStatusCode.InternalServerError,
                        "An unexpected error occurred",
                        _env.IsDevelopment()
                            ? ex.Message
                            : "Something went wrong on our end. Please try again later.")
            };

            _logger.LogError(ex,
                "Unhandled exception (status {Status}) while processing {Method} {Path}",
                status, context.Request.Method, context.Request.Path);

            var problem = new ProblemDetails
            {
                Status = status,
                Title = title,
                Detail = detail,
                Instance = context.Request.Path,
                Type = $"https://httpstatuses.io/{status}"
            };
            problem.Extensions["traceId"] = context.TraceIdentifier;

            context.Response.Clear();
            context.Response.StatusCode = status;
            context.Response.ContentType = "application/problem+json";
            await context.Response.WriteAsync(JsonSerializer.Serialize(problem, JsonOptions));
        }
    }
}
