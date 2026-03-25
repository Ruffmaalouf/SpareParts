using System.Net;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using SpareParts.Infrastructure.Data;
using SpareParts.Infrastructure.Services;

namespace SpareParts.Api.Errors
{
    public sealed class ApiExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ApiExceptionMiddleware> _logger;

        public ApiExceptionMiddleware(RequestDelegate next, ILogger<ApiExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task Invoke(HttpContext context, IExceptionLogWriter exceptionLogWriter)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                var (statusCode, code) = MapException(ex);
                var (sourceClass, sourceMethod, sourceLine) = ExtractSource(ex);

                try
                {
                    await exceptionLogWriter.WriteAsync(new ExceptionLogEntry
                    {
                        OccurredAtUtc = DateTime.UtcNow,
                        Application = "SpareParts.Api",
                        TraceId = context.TraceIdentifier,
                        HttpMethod = context.Request.Method,
                        RequestPath = context.Request.Path.Value,
                        StatusCode = (int)statusCode,
                        ErrorCode = code,
                        ExceptionType = ex.GetType().FullName ?? ex.GetType().Name,
                        ExceptionMessage = ex.Message,
                        SourceClass = sourceClass,
                        SourceMethod = sourceMethod,
                        SourceLine = sourceLine,
                        StackTrace = ex.ToString(),
                        IsCaught = false
                    }, context.RequestAborted);
                }
                catch (Exception loggingException)
                {
                    _logger.LogError(loggingException, "Failed to persist exception log for trace {TraceId}", context.TraceIdentifier);
                }

                await WriteError(context, ex, statusCode, code);
            }
        }

        private static (HttpStatusCode StatusCode, string Code) MapException(Exception ex)
        {
            return ex switch
            {
                ValidationException => (HttpStatusCode.BadRequest, "validation_error"),
                NotFoundException => (HttpStatusCode.NotFound, "not_found"),
                ConflictException => (HttpStatusCode.Conflict, "conflict"),
                UnauthorizedAccessException => (HttpStatusCode.Unauthorized, "unauthorized"),
                _ => (HttpStatusCode.InternalServerError, "internal_error")
            };
        }

        private static (string? SourceClass, string? SourceMethod, int? SourceLine) ExtractSource(Exception ex)
        {
            var frames = new StackTrace(ex, true).GetFrames();
            var frame = frames?.FirstOrDefault(f => f.GetFileLineNumber() > 0)
                ?? frames?.FirstOrDefault();
            var method = frame?.GetMethod();
            var line = frame?.GetFileLineNumber();

            return (
                method?.DeclaringType?.FullName,
                method?.Name,
                line is > 0 ? line : null
            );
        }

        private static async Task WriteError(HttpContext context, Exception ex, HttpStatusCode statusCode, string code)
        {

            context.Response.StatusCode = (int)statusCode;
            context.Response.ContentType = "application/json";

            var envelope = new ApiErrorEnvelope
            {
                Code = code,
                Message = ex.Message,
                TraceId = context.TraceIdentifier
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(envelope));
        }
    }
}
