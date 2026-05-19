using Microsoft.Data.SqlClient;

namespace PWSV.Api.Extensions;

public static class DatabaseInitializer
{
    private const string DatabaseName = "PWSV";

    public static async Task EnsureDatabaseFilesAsync(string targetDirectory, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(targetDirectory);

        var mdfPath = Path.Combine(targetDirectory, $"{DatabaseName}.mdf");
        var ldfPath = Path.Combine(targetDirectory, $"{DatabaseName}_log.ldf");

        var masterConnectionString = "Server=(localdb)\\MSSQLLocalDB;Database=master;Trusted_Connection=True;Encrypt=False";

        await using var connection = new SqlConnection(masterConnectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        await using var checkCmd = connection.CreateCommand();
        checkCmd.CommandText = "SELECT DB_ID(@name)";
        checkCmd.Parameters.AddWithValue("@name", DatabaseName);
        var exists = await checkCmd.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);

        if (exists is null or DBNull)
        {
            await using var createCmd = connection.CreateCommand();
            createCmd.CommandText = $"""
                CREATE DATABASE [{DatabaseName}]
                ON PRIMARY (NAME = N'{DatabaseName}', FILENAME = N'{mdfPath}')
                LOG ON (NAME = N'{DatabaseName}_log', FILENAME = N'{ldfPath}')
                COLLATE Cyrillic_General_CI_AS;
                """;
            await createCmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);

            await using var compatCmd = connection.CreateCommand();
            compatCmd.CommandText = $"ALTER DATABASE [{DatabaseName}] SET COMPATIBILITY_LEVEL = 160;";
            await compatCmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}
