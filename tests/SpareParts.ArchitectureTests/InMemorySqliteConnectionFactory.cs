using System.Data;
using Microsoft.Data.Sqlite;
using SpareParts.Infrastructure.Interfaces;
using SpareParts.Infrastructure.Services;

namespace SpareParts.ArchitectureTests;

internal sealed class InMemorySqliteConnectionFactory : ISqlConnectionFactory, IDisposable
{
    private readonly string _connectionString;
    private readonly SqliteConnection _keeperConnection;

    public static ITenantContext NoTenant { get; } = TenantContext.Legacy;

    public static ITenantContext ForTenant(int tenantId, string code = "test") =>
        new TenantContext { TenantId = tenantId, TenantCode = code, IsResolved = true };

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
            CREATE TABLE IF NOT EXISTS Tenants (
                Id INTEGER PRIMARY KEY,
                Name TEXT NOT NULL,
                Code TEXT NOT NULL,
                Domain TEXT NULL,
                IsActive INTEGER NOT NULL DEFAULT 1,
                CreatedAt TEXT,
                UpdatedAt TEXT
            );

            INSERT OR IGNORE INTO Tenants (Id, Name, Code, IsActive)
            VALUES (1, 'Default', 'default', 1);

            CREATE TABLE IF NOT EXISTS Users (
                Id INTEGER PRIMARY KEY,
                Username TEXT,
                FullName TEXT,
                Email TEXT,
                PasswordHash TEXT,
                RoleId INTEGER NOT NULL DEFAULT 3,
                TenantId INTEGER NULL,
                IsActive INTEGER,
                LastLoginAt TEXT,
                CreatedAt TEXT,
                ModifiedAt TEXT
            );

            CREATE TABLE IF NOT EXISTS Roles (
                Id INTEGER PRIMARY KEY,
                Name TEXT NOT NULL,
                IsActive INTEGER NOT NULL DEFAULT 1
            );

            INSERT OR IGNORE INTO Roles (Id, Name, IsActive)
            VALUES (1, 'Admin', 1), (2, 'Manager', 1), (3, 'Cashier', 1), (4, 'Web App User', 1),
                   (5, 'Super Admin', 1);

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
