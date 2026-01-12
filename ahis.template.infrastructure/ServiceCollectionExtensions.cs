using ahis.template.application.Shared.Mediator;
using ahis.template.application.Interfaces.Repositories;
using ahis.template.infrastructure.Repositories;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using FluentValidation;
using ahis.template.domain.SharedKernel;
using ahis.template.infrastructure.SharedKernel;

namespace ahis.template.infrastructure
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services)
        {
            // Register UnitOfWork (fully-qualified to avoid namespace/type name collision)
            services.AddScoped<IUnitOfWork, UnitOfWork>();

            // Register repositories
            services.AddScoped<ICountryRepository, CountryRepository>();

            // Register custom Mediator
            services.AddScoped<IMediator, SimpleMediator>();

            // Register FluentValidation validators from the application assembly
            var applicationAssembly = Assembly.Load("ahis.template.application");
            services.AddValidatorsFromAssembly(applicationAssembly);

            // Automatically register all IRequestHandler<,> handlers (exclude the ValidationBehavior itself)
            var handlerType = typeof(IRequestHandler<,>);
            var validationBehaviorGeneric = typeof(ahis.template.application.Shared.Mediator.ValidationBehavior<,>);

            foreach (var type in applicationAssembly.GetTypes())
            {
                // Skip the ValidationBehavior generic definition to avoid registering it as a handler implementation
                if (type.IsGenericType && type.IsGenericTypeDefinition && type.GetGenericTypeDefinition() == validationBehaviorGeneric)
                    continue;

                var interfaces = type.GetInterfaces()
                    .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == handlerType)
                    .ToList();

                foreach (var @interface in interfaces)
                {
                    services.AddScoped(@interface, type);
                }
            }

            return services;
        }
    }
}
