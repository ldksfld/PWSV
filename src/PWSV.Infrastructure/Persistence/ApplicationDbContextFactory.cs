using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using PWSV.Application.Common.Interfaces;
using PWSV.Infrastructure.Persistence.Interceptors;
using PWSV.Infrastructure.Services;

namespace PWSV.Infrastructure.Persistence;

public sealed class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    private const string DesignTimeConnectionString =
        "Server=(localdb)\\MSSQLLocalDB;Database=PWSV;Trusted_Connection=True;Encrypt=False;MultipleActiveResultSets=True";

    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(DesignTimeConnectionString, sql => sql.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName))
            .Options;

        var interceptor = new AuditInterceptor(NullCurrentUserService.Instance, new DateTimeProvider());
        return new ApplicationDbContext(options, interceptor);
    }

    private sealed class NullCurrentUserService : ICurrentUserService
    {
        public static NullCurrentUserService Instance { get; } = new();

        public int? UserId => null;
        public string? Username => null;
        public bool IsAuthenticated => false;
    }
}
