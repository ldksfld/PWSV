using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PWSV.Infrastructure.Persistence;

namespace PWSV.Api.IntegrationTests;

public sealed class PwsvWebApplicationFactory : WebApplicationFactory<Program>
{
    public string DatabaseName { get; } = $"PWSV_Tests_{Guid.NewGuid():N}";

    public string ConnectionString =>
        $"Server=(localdb)\\MSSQLLocalDB;Database={DatabaseName};Trusted_Connection=True;Encrypt=False;MultipleActiveResultSets=True";

    protected override IHost CreateHost(IHostBuilder builder)
    {
        builder.ConfigureHostConfiguration(config =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = ConnectionString,
                ["Jwt:SecretKey"] = "integration-test-secret-must-be-at-least-32-bytes-long-0123456789"
            });
        });

        return base.CreateHost(builder);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
    }

    public async Task DisposeDatabaseAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await db.Database.EnsureDeletedAsync();
    }
}
