using AutoMapper;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace AeroMech.Models
{
    public static class AutoMapperDependencyInjection
    {
        public static void AddAutomapperProfiles(this IServiceCollection services)
        {
            services.AddSingleton(provider =>
            {
                var loggerFactory = provider.GetRequiredService<ILoggerFactory>();

                Assembly currentAssembly = typeof(AutoMapperDependencyInjection).Assembly;

                var config = new MapperConfiguration(cfg =>
                {
                    cfg.AddMaps(currentAssembly);
                }, loggerFactory);

                return config.CreateMapper();
            });

        }
    }
}
