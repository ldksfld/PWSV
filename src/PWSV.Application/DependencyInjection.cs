using System.Reflection;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using PWSV.Application.Common.Behaviors;
using PWSV.Application.Common.Mappings;
using PWSV.Application.Common.Validation;

namespace PWSV.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(assembly);
            cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
            cfg.AddOpenBehavior(typeof(UnitOfWorkBehavior<,>));
        });

        services.AddValidatorsFromAssembly(assembly);

        MapsterConfig.Register();
        ValidationConfig.Register();

        return services;
    }
}
