using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MenuBuilderBack.Repository;
using MenuBuilderBack.Repository.Interface;
using MenuBuilderBack.Service;
using MenuBuilderBack.Service.Interface;

namespace MenuBuilderBack.Extensions
{
    public static class ServiceExtesions
    {
        public static IServiceCollection AddRepositories(this IServiceCollection services)
        {
            services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));

            return services;
        }
        
        public static IServiceCollection AddServices(this IServiceCollection services)
        {
            services.AddScoped(typeof(IGenericService<>), typeof(GenericService<>));

            return services;
        }
    }
}