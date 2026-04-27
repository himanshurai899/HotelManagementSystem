using System.Net;
using System.Text.Json;
using HotelManagementSystem.API.Middleware;
using HotelManagementSystem.Shared.Exceptions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace HotelManagementSystem.Tests.Middleware
{
    public class GlobalExceptionMiddlewareTests
    {
        private static DefaultHttpContext NewContext()
        {
            var ctx = new DefaultHttpContext();
            ctx.Response.Body = new MemoryStream();
            return ctx;
        }

        private static GlobalExceptionMiddleware Build(
            RequestDelegate next,
            string envName = "Production")
        {
            var env = new Mock<IHostEnvironment>();
            env.SetupGet(e => e.EnvironmentName).Returns(envName);
            return new GlobalExceptionMiddleware(
                next,
                NullLogger<GlobalExceptionMiddleware>.Instance,
                env.Object);
        }

        private static async Task<ProblemDetails> ReadBodyAsync(HttpContext ctx)
        {
            ctx.Response.Body.Seek(0, SeekOrigin.Begin);
            var json = await new StreamReader(ctx.Response.Body).ReadToEndAsync();
            return JsonSerializer.Deserialize<ProblemDetails>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
        }

        [Fact]
        public async Task NoException_PassesThrough()
        {
            var ctx = NewContext();
            var middleware = Build(_ => Task.CompletedTask);
            await middleware.InvokeAsync(ctx);
            Assert.Equal(200, ctx.Response.StatusCode);
        }

        [Fact]
        public async Task NotFoundException_Returns404_WithProblemDetails()
        {
            var ctx = NewContext();
            var middleware = Build(_ => throw new NotFoundException("Booking 5 not found."));
            await middleware.InvokeAsync(ctx);

            Assert.Equal((int)HttpStatusCode.NotFound, ctx.Response.StatusCode);
            Assert.Equal("application/problem+json", ctx.Response.ContentType);
            var body = await ReadBodyAsync(ctx);
            Assert.Equal(404, body.Status);
            Assert.Equal("Resource not found", body.Title);
            Assert.Equal("Booking 5 not found.", body.Detail);
        }

        [Fact]
        public async Task ValidationException_Returns400_WithProblemDetails()
        {
            var ctx = NewContext();
            var middleware = Build(_ => throw new ValidationException("Check-in date must be in the future."));
            await middleware.InvokeAsync(ctx);

            Assert.Equal((int)HttpStatusCode.BadRequest, ctx.Response.StatusCode);
            var body = await ReadBodyAsync(ctx);
            Assert.Equal(400, body.Status);
            Assert.Equal("Validation failed", body.Title);
            Assert.Equal("Check-in date must be in the future.", body.Detail);
        }

        [Fact]
        public async Task ConflictException_Returns409_WithProblemDetails()
        {
            var ctx = NewContext();
            var middleware = Build(_ => throw new ConflictException("Room number already exists."));
            await middleware.InvokeAsync(ctx);

            Assert.Equal((int)HttpStatusCode.Conflict, ctx.Response.StatusCode);
            var body = await ReadBodyAsync(ctx);
            Assert.Equal(409, body.Status);
            Assert.Equal("Conflict", body.Title);
            Assert.Equal("Room number already exists.", body.Detail);
        }

        [Fact]
        public async Task UnauthorizedAccessException_Returns401()
        {
            var ctx = NewContext();
            var middleware = Build(_ => throw new UnauthorizedAccessException("nope"));
            await middleware.InvokeAsync(ctx);

            Assert.Equal((int)HttpStatusCode.Unauthorized, ctx.Response.StatusCode);
            var body = await ReadBodyAsync(ctx);
            Assert.Equal(401, body.Status);
        }

        [Fact]
        public async Task UnknownException_Returns500_WithGenericMessage_InProduction()
        {
            var ctx = NewContext();
            var middleware = Build(
                _ => throw new InvalidOperationException("internal SQL failure trace xyz"),
                envName: Environments.Production);
            await middleware.InvokeAsync(ctx);

            Assert.Equal((int)HttpStatusCode.InternalServerError, ctx.Response.StatusCode);
            var body = await ReadBodyAsync(ctx);
            Assert.Equal(500, body.Status);
            Assert.Equal("An unexpected error occurred", body.Title);
            // Production must NOT leak the original message
            Assert.DoesNotContain("SQL failure trace xyz", body.Detail ?? string.Empty);
            Assert.False(string.IsNullOrWhiteSpace(body.Detail));
        }

        [Fact]
        public async Task UnknownException_IncludesDetailMessage_InDevelopment()
        {
            var ctx = NewContext();
            var middleware = Build(
                _ => throw new InvalidOperationException("dev-only details"),
                envName: Environments.Development);
            await middleware.InvokeAsync(ctx);

            var body = await ReadBodyAsync(ctx);
            Assert.Equal(500, body.Status);
            Assert.Contains("dev-only details", body.Detail ?? string.Empty);
        }

        [Fact]
        public async Task ProblemDetails_AlwaysIncludesTraceId()
        {
            var ctx = NewContext();
            ctx.TraceIdentifier = "trace-abc";
            var middleware = Build(_ => throw new NotFoundException("missing"));
            await middleware.InvokeAsync(ctx);

            ctx.Response.Body.Seek(0, SeekOrigin.Begin);
            var json = await new StreamReader(ctx.Response.Body).ReadToEndAsync();
            Assert.Contains("trace-abc", json);
        }
    }
}
