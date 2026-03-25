namespace SpareParts.Infrastructure.Data
{
    public interface IExceptionLogWriter
    {
        Task WriteAsync(ExceptionLogEntry entry, CancellationToken cancellationToken = default);
    }
}
