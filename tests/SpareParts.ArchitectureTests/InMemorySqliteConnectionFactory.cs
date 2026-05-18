using System.Data;
using Microsoft.Data.Sqlite;
using SpareParts.Infrastructure.Interfaces;

namespace SpareParts.ArchitectureTests;

internal sealed class InMemorySqliteConnectionFactory : ISqlConnectionFactory, IDisposable
{
    private readonly string _connectionString;
    private readonly SqliteConnection _keeperConnection;

    public InMemorySqliteConnectionFactory()
    {
        _connectionString = $"Data Source=SparePartsArchitectureTests-{Guid.NewGuid():N};Mode=Memory;Cache=Shared";
        _keeperConnection = new SqliteConnection(_connectionString);
        _keeperConnection.Open();
    }

    public IDbConnection CreateConnection()
    {
        var connection = new SqliteConnection(_connectionString);
        connection.Open();
        return connection;
    }

    public void InitializeSchema()
    {
        using var command = _keeperConnection.CreateCommand();
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS Users (
                Id INTEGER PRIMARY KEY,
                Username TEXT,
                FullName TEXT,
                Email TEXT,
                PasswordHash TEXT,
                Role TEXT,
                IsActive INTEGER,
                LastLoginAt TEXT,
                CreatedAt TEXT,
                ModifiedAt TEXT
            );

            CREATE TABLE IF NOT EXISTS AccountingPostingSettings (
                SettingKey TEXT PRIMARY KEY,
                AccountId INTEGER NOT NULL
            );
            """;
        command.ExecuteNonQuery();
    }

    public void Dispose()
    {
        _keeperConnection.Dispose();
    }
}
