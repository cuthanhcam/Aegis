using Aegis.Application.Features.Permissions;
using Aegis.Application.Features.Query;
using Aegis.Application.Services;
using Microsoft.Extensions.DependencyInjection;
using Aegis.Application.Interfaces;

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
            services.AddScoped<IAuthAppService, AuthAppService>();
            services.AddScoped<IStoreAppService, StoreAppService>();
            services.AddScoped<IAuthorizationModelAppService, AuthorizationModelAppService>();
            services.AddScoped<IAuthorizationQueryAppService, AuthorizationQueryAppService>();
            services.AddScoped<IPermissionAppService, PermissionAppService>();
            services.AddScoped<IRelationshipService, RelationshipAppService>();
            services.AddScoped<AssertionAppService>();
            services.AddScoped<IRbacAdminService, RbacAdminService>();
            services.AddSingleton<IPresetAppService, PresetAppService>();

            return services;
        }
    }
}
