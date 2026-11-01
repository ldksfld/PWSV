using System.Data;
using System.Globalization;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using PWSV.Infrastructure.Persistence;

namespace PWSV.Api.Extensions;

public static class DatabaseInitializer
{
    private const int TargetCompatibilityLevel = 160;
    private const string LocalDbPrefix = "(localdb)";

    public static async Task InitializeAsync(IServiceProvider services, string dataDirectory, CancellationToken cancellationToken)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var connectionString = db.Database.GetConnectionString()
            ?? throw new InvalidOperationException("Connection string for ApplicationDbContext is not configured.");

        await EnsureLocalDbFilesAsync(connectionString, dataDirectory, cancellationToken).ConfigureAwait(false);
        await db.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async Task EnsureLocalDbFilesAsync(string connectionString, string dataDirectory, CancellationToken cancellationToken)
    {
        var target = new SqlConnectionStringBuilder(connectionString);
        if (!target.DataSource.StartsWith(LocalDbPrefix, StringComparison.OrdinalIgnoreCase)
            || string.IsNullOrWhiteSpace(target.InitialCatalog))
        {
            return;
        }

        var databaseName = target.InitialCatalog;
        var master = new SqlConnectionStringBuilder(connectionString)
        {
            InitialCatalog = "master",
            MultipleActiveResultSets = false
        };

        await using var connection = new SqlConnection(master.ConnectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        if (await DatabaseExistsAsync(connection, databaseName, cancellationToken).ConfigureAwait(false))
        {
            return;
        }

        Directory.CreateDirectory(dataDirectory);
        var mdfPath = Path.Combine(dataDirectory, $"{databaseName}.mdf");
        var ldfPath = Path.Combine(dataDirectory, $"{databaseName}_log.ldf");

        var createSql = BuildCreateDatabaseSql(databaseName, mdfPath, ldfPath);
        await ExecuteAsync(connection, createSql, cancellationToken).ConfigureAwait(false);

        var compatibilityLevel = Math.Min(TargetCompatibilityLevel, GetServerMajorVersion(connection) * 10);
        var compatibilitySql = $"ALTER DATABASE {QuoteName(databaseName)} SET COMPATIBILITY_LEVEL = {compatibilityLevel.ToString(CultureInfo.InvariantCulture)};";
        await ExecuteAsync(connection, compatibilitySql, cancellationToken).ConfigureAwait(false);
    }

    private static string BuildCreateDatabaseSql(string databaseName, string mdfPath, string ldfPath)
    {
        var name = QuoteName(databaseName);
        var dataFile = $"(NAME = {QuoteLiteral(databaseName)}, FILENAME = {QuoteLiteral(mdfPath)})";
        var logFile = $"(NAME = {QuoteLiteral($"{databaseName}_log")}, FILENAME = {QuoteLiteral(ldfPath)})";

        if (!File.Exists(mdfPath))
        {
            return $"CREATE DATABASE {name} ON PRIMARY {dataFile} LOG ON {logFile} COLLATE Cyrillic_General_CI_AS;";
        }

        return File.Exists(ldfPath)
            ? $"CREATE DATABASE {name} ON {dataFile}, {logFile} FOR ATTACH;"
            : $"CREATE DATABASE {name} ON {dataFile} FOR ATTACH_REBUILD_LOG;";
    }

    private static async Task<bool> DatabaseExistsAsync(SqlConnection connection, string databaseName, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT DB_ID(@name)";
        command.Parameters.Add(new SqlParameter("@name", SqlDbType.NVarChar, 128) { Value = databaseName });
        var result = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
        return result is not null and not DBNull;
    }

    private static async Task ExecuteAsync(SqlConnection connection, string sql, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    private static int GetServerMajorVersion(SqlConnection connection)
    {
        var major = connection.ServerVersion.Split('.')[0];
        return int.Parse(major, CultureInfo.InvariantCulture);
    }

    private static string QuoteName(string identifier) => $"[{identifier.Replace("]", "]]", StringComparison.Ordinal)}]";

    private static string QuoteLiteral(string value) => $"N'{value.Replace("'", "''", StringComparison.Ordinal)}'";
}
