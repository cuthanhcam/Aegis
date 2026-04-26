using Aegis.Application.Features.Permissions;
using Aegis.Application.Features.Query;
using Microsoft.Extensions.DependencyInjection;

namespace Aegis.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddAegisApplication(this IServiceCollection services)
        {
            services.AddScoped<CheckPermissionUseCase>();
            services.AddScoped<BatchCheckInStoreUseCase>();
            services.AddScoped<ResolveAuthorizationModelUseCase>();
            services.AddScoped<BatchCheckAegisCompatUseCase>();
            services.AddScoped<QueryAuditUseCase>();
            services.AddScoped<ResolveQueryModelContextUseCase>();
            services.AddScoped<QueryAllowTuplesUseCase>();
            services.AddScoped<ListUsersQueryUseCase>();
            services.AddScoped<ListObjectsQueryUseCase>();
            services.AddScoped<ExpandQueryUseCase>();
            services.AddScoped<ResolveUsersetEntriesFromRelationFiltersUseCase>();
            return services;
        }
    }
}
