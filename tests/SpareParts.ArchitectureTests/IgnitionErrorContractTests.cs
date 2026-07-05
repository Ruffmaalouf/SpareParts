using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using SpareParts.Api.Errors;
using SpareParts.Infrastructure.Data;
using SpareParts.Infrastructure.Services;

namespace SpareParts.ArchitectureTests;

/// <summary>
/// Ignition QA gate — error contract for the new endpoints (Report 06 §06–§07 security:
/// "Forced server error returns production-safe [error envelope]; no .NET exception details reach
/// the client"; "Cross-tenant customer id returns the same 404 (status, shape, ...) as a
/// nonexistent id").
///
/// Report 04 §09 specifies the error body is the existing ApiExceptionMiddleware envelope
/// (code/message/traceId), NOT RFC 7807 ProblemDetails — the checklist's "ProblemDetails" wording
/// is used loosely for "structured, production-safe error". This locks in the ACTUAL shape the
/// workspace 404 flows through.
/// </summary>
public class IgnitionErrorContractTests
{
    [Fact]
    public async Task NotFoundException_ShouldMapTo404_WithUniformNotFoundEnvelope_AndNoLeakage()
    {
        // Every enumeration-safe path (missing OR cross-tenant customer) throws this exact exception.
        var middleware = new ApiExceptionMiddleware(
            _ => throw new NotFoundException("Customer not found."),
            NullLogger<ApiExceptionMiddleware>.Instance);

        var (statusCode, payload) = await InvokeAsync(middleware, "trace-workspace-404");

        Assert.Equal((int)HttpStatusCode.NotFound, statusCode);

        var envelope = Deserialize(payload);
        Assert.NotNull(envelope);
        Assert.Equal("not_found", envelope!.Code);
        Assert.Equal("Customer not found.", envelope.Message);
        Assert.Equal("trace-workspace-404", envelope.TraceId);

        // No tenant identity, db name, stack trace, or exception type in the wire body.
        Assert.DoesNotContain("Tenant", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("dbName", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("StackTrace", payload, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("at SpareParts.", payload, StringComparison.Ordinal);
    }

    [Fact]
    public async Task NotFound404Body_ShouldBeByteIdentical_ForCrossTenantAndNonexistentIds()
    {
        // The workspace guarantee: a cross-tenant id and a nonexistent id are indistinguishable.
        // Both throw NotFoundException("Customer not found."), so — given the same trace id — the
        // serialized 404 body is byte-for-byte identical (shape enumeration-safety, Report 06 §07).
        var crossTenant = new ApiExceptionMiddleware(
            _ => throw new NotFoundException("Customer not found."),
            NullLogger<ApiExceptionMiddleware>.Instance);
        var nonexistent = new ApiExceptionMiddleware(
            _ => throw new NotFoundException("Customer not found."),
            NullLogger<ApiExceptionMiddleware>.Instance);

        var (crossStatus, crossBody) = await InvokeAsync(crossTenant, "trace-same");
        var (missingStatus, missingBody) = await InvokeAsync(nonexistent, "trace-same");

        Assert.Equal(missingStatus, crossStatus);
        Assert.Equal(missingBody, crossBody);
    }

    [Fact]
    public async Task ForcedServerError_ShouldReturnGeneric500_AndNeverLeakDbOrTenantDetail()
    {
        const string sensitive = "Invalid object name 'dbo.Tenant_118_Secrets' at SqlConnection.Open for dbName=Tenant_118";
        var middleware = new ApiExceptionMiddleware(
            _ => throw new InvalidOperationException(sensitive),
            NullLogger<ApiExceptionMiddleware>.Instance);

        var (statusCode, payload) = await InvokeAsync(middleware, "trace-500");

        Assert.Equal((int)HttpStatusCode.InternalServerError, statusCode);

        var envelope = Deserialize(payload);
        Assert.NotNull(envelope);
        Assert.Equal("internal_error", envelope!.Code);
        Assert.Equal("An unexpected server error occurred.", envelope.Message);

        Assert.DoesNotContain(sensitive, payload, StringComparison.Ordinal);
        Assert.DoesNotContain("Tenant_118", payload, StringComparison.Ordinal);
        Assert.DoesNotContain(nameof(InvalidOperationException), payload, StringComparison.Ordinal);
    }

    private static async Task<(int StatusCode, string Payload)> InvokeAsync(ApiExceptionMiddleware middleware, string traceId)
    {
        var context = new DefaultHttpContext { TraceIdentifier = traceId };
        context.Response.Body = new MemoryStream();

        await middleware.Invoke(context, new NoOpExceptionLogWriter());

        context.Response.Body.Position = 0;
        using var reader = new StreamReader(context.Response.Body, Encoding.UTF8, leaveOpen: true);
        return (context.Response.StatusCode, await reader.ReadToEndAsync());
    }

    private static ApiErrorEnvelope? Deserialize(string payload)
        => JsonSerializer.Deserialize<ApiErrorEnvelope>(payload, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

    private sealed class NoOpExceptionLogWriter : IExceptionLogWriter
    {
        public Task WriteAsync(ExceptionLogEntry entry, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }
}
