using System.Data.Common;
using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;
using Testcontainers.MsSql;
using Xunit;

namespace TestcontainersDemo;

/// <summary>
/// The lie this repo exists to demonstrate: SQLite "because it's close enough"
/// happily accepts data the real dialect rejects. Same DDL, same INSERT —
/// green on SQLite, SqlException on SQL Server.
/// </summary>
public sealed class SqlDialectTests : IAsyncLifetime
{
    private readonly MsSqlContainer _sql =
        new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-CU14-ubuntu-22.04").Build();

    public async Task InitializeAsync()
    {
        // A real SQL Server, owned by this test run. Nothing shared, nothing left behind.
        await _sql.StartAsync();
    }

    public async Task DisposeAsync() => await _sql.DisposeAsync().AsTask();

    [Fact]
    public async Task Sqlite_accepts_the_string_sql_server_rejects()
    {
        const string elevenChars = "abcdefghijk"; // one char too many for NVARCHAR(10)

        // SQLite: the "close enough" test double. NVARCHAR(10) is just advice here.
        await using var sqlite = new SqliteConnection("Data Source=:memory:");
        await sqlite.OpenAsync();
        await Exec(sqlite, "CREATE TABLE Customers (Name NVARCHAR(10) NOT NULL)");
        await Exec(sqlite, $"INSERT INTO Customers (Name) VALUES ('{elevenChars}')");
        // ...and it passed. Test suite green, bug still on its way to production.

        // SQL Server: the dialect production actually runs.
        await using var mssql = new SqlConnection(_sql.GetConnectionString());
        await mssql.OpenAsync();
        await Exec(mssql, "CREATE TABLE Customers (Name NVARCHAR(10) NOT NULL)");
        var ex = await Assert.ThrowsAsync<SqlException>(
            () => Exec(mssql, $"INSERT INTO Customers (Name) VALUES ('{elevenChars}')"));
        Assert.Contains("truncated", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static async Task Exec(DbConnection connection, string sql)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync();
    }
}
