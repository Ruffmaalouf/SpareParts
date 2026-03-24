using System.Net;
using System.Text.Json;
using SpareParts.Infrastructure.Services;

namespace SpareParts.Api.Errors
{
    public sealed class ApiExceptionMiddleware
    {
        private readonly RequestDelegate _next;

        public ApiExceptionMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                await WriteError(context, ex);
            }
        }

        private static async Task WriteError(HttpContext context, Exception ex)
        {
            var (statusCode, code) = ex switch
            {
                ValidationException => (HttpStatusCode.BadRequest, "validation_error"),
                NotFoundException => (HttpStatusCode.NotFound, "not_found"),
                ConflictException => (HttpStatusCode.Conflict, "conflict"),
                UnauthorizedAccessException => (HttpStatusCode.Unauthorized, "unauthorized"),
                _ => (HttpStatusCode.InternalServerError, "internal_error")
            };

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
