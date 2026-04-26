using Aegis.Application.Features.Permissions;
using Aegis.Application.Features.Query;
using Aegis.Application.Services;
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
            services.AddScoped<AuthAppService>();
            services.AddScoped<StoreAppService>();
            services.AddScoped<AuthorizationModelAppService>();
            services.AddScoped<AuthorizationQueryAppService>();
            services.AddScoped<PermissionAppService>();
            services.AddScoped<RelationshipAppService>();
            services.AddScoped<AssertionAppService>();
            services.AddScoped<RbacAdminService>();
            services.AddSingleton<PresetAppService>();

            return services;
        }
    }
}
