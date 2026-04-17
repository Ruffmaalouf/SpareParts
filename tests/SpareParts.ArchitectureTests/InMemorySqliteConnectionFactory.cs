using System.Data;
using Microsoft.Data.Sqlite;
using SpareParts.Infrastructure.Interfaces;

namespace SpareParts.ArchitectureTests;

internal sealed class InMemorySqliteConnectionFactory : ISqlConnectionFactory, IDisposable
{
    private readonly SqliteConnection _connection;

    public InMemorySqliteConnectionFactory()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
    }

    public IDbConnection CreateConnection() => _connection;

    public void InitializeSchema()
    {
        using var command = _connection.CreateCommand();
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
        _connection.Dispose();
    }
}
