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
    private const int FileActivationFailure = 5120;

    public static async Task<bool> InitializeAsync(IServiceProvider services, string dataDirectory, CancellationToken cancellationToken)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var connectionString = db.Database.GetConnectionString()
            ?? throw new InvalidOperationException("Connection string for ApplicationDbContext is not configured.");

        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(DatabaseInitializer));
        await EnsureLocalDbFilesAsync(connectionString, dataDirectory, logger, cancellationToken).ConfigureAwait(false);
        var appliedMigrations = await db.Database.GetAppliedMigrationsAsync(cancellationToken).ConfigureAwait(false);
        await db.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);
        return !appliedMigrations.Any();
    }

    private static async Task EnsureLocalDbFilesAsync(
        string connectionString,
        string dataDirectory,
        ILogger logger,
        CancellationToken cancellationToken)
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
            if (await FindMissingDataFileAsync(connection, databaseName, cancellationToken).ConfigureAwait(false) is not { } missingFile)
            {
                return;
            }

            logger.LogWarning(
                "Database {DatabaseName} is registered in LocalDB but its data file {DataFile} no longer exists, the stale registration is removed",
                databaseName,
                missingFile);
            await DropStaleDatabaseAsync(connection, databaseName, cancellationToken).ConfigureAwait(false);
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

    private static async Task<string?> FindMissingDataFileAsync(SqlConnection connection, string databaseName, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT physical_name FROM sys.master_files WHERE database_id = DB_ID(@name) AND type = 0";
        command.Parameters.Add(new SqlParameter("@name", SqlDbType.NVarChar, 128) { Value = databaseName });
        var result = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
        return result is string path && !File.Exists(path) ? path : null;
    }

    private static async Task DropStaleDatabaseAsync(SqlConnection connection, string databaseName, CancellationToken cancellationToken)
    {
        try
        {
            await ExecuteAsync(connection, $"DROP DATABASE {QuoteName(databaseName)};", cancellationToken).ConfigureAwait(false);
        }
        catch (SqlException ex) when (ex.Number == FileActivationFailure)
        {
            if (await DatabaseExistsAsync(connection, databaseName, cancellationToken).ConfigureAwait(false))
            {
                throw;
            }
        }
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
