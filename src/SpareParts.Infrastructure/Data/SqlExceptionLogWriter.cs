using Dapper;

namespace SpareParts.Infrastructure.Data
{
    public sealed class SqlExceptionLogWriter : IExceptionLogWriter
    {
        private readonly ISqlConnectionFactory _connectionFactory;

        public SqlExceptionLogWriter(ISqlConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task WriteAsync(ExceptionLogEntry entry, CancellationToken cancellationToken = default)
        {
            const string sql = @"
IF OBJECT_ID('dbo.ApplicationExceptionLogs', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.ApplicationExceptionLogs
    (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        OccurredAtUtc DATETIME2 NOT NULL,
        Application NVARCHAR(128) NOT NULL,
        TraceId NVARCHAR(128) NULL,
        HttpMethod NVARCHAR(16) NULL,
        RequestPath NVARCHAR(512) NULL,
        StatusCode INT NOT NULL,
        ErrorCode NVARCHAR(64) NOT NULL,
        ExceptionType NVARCHAR(512) NOT NULL,
        ExceptionMessage NVARCHAR(MAX) NOT NULL,
        SourceClass NVARCHAR(512) NULL,
        SourceMethod NVARCHAR(256) NULL,
        SourceLine INT NULL,
        StackTrace NVARCHAR(MAX) NULL,
        IsCaught BIT NOT NULL
    );
END;

INSERT INTO dbo.ApplicationExceptionLogs
(
    OccurredAtUtc,
    Application,
    TraceId,
    HttpMethod,
    RequestPath,
    StatusCode,
    ErrorCode,
    ExceptionType,
    ExceptionMessage,
    SourceClass,
    SourceMethod,
    SourceLine,
    StackTrace,
    IsCaught
)
VALUES
(
    @OccurredAtUtc,
    @Application,
    @TraceId,
    @HttpMethod,
    @RequestPath,
    @StatusCode,
    @ErrorCode,
    @ExceptionType,
    @ExceptionMessage,
    @SourceClass,
    @SourceMethod,
    @SourceLine,
    @StackTrace,
    @IsCaught
);";

            using var connection = _connectionFactory.Create();
            await connection.ExecuteAsync(new CommandDefinition(sql, entry, cancellationToken: cancellationToken));
        }
    }
}
