using Aegis.Application.Features.Assertions;
using Aegis.Application.Features.Permissions;
using Aegis.Application.Features.Query;
using Aegis.Application.Features.Stores;
using Aegis.Application.Features.Users;
using Aegis.Application.Features.AuthorizationModels;
using Aegis.Application.Interfaces;
using Aegis.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Aegis.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddAegisApplication(this IServiceCollection services)
        {
            // Use Cases (registered without interfaces as they are directly consumed by application services)
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
            services.AddScoped<CreateStoreUseCase>();
            services.AddScoped<CreateUserUseCase>();
            services.AddScoped<UpdateUserUseCase>();
            services.AddScoped<DeleteUserUseCase>();
            services.AddSingleton<AssertionValidator>();
            services.AddScoped<WriteAssertionsUseCase>();
            services.AddScoped<RunAssertionsUseCase>();
            services.AddScoped<GenerateAssertionsFromAuditUseCase>();
            services.AddSingleton<AuthorizationModelValidator>();
            services.AddScoped<CreateAuthorizationModelUseCase>();
            services.AddScoped<UpdateAuthorizationModelUseCase>();
            services.AddScoped<DeleteAuthorizationModelUseCase>();
            services.AddScoped<PublishAuthorizationModelUseCase>();
            services.AddScoped<RollbackAuthorizationModelUseCase>();

            // Application Services - Standard Dependency Inversion Pattern
            services.AddScoped<IAuthAppService, AuthAppService>();
            services.AddScoped<IStoreAppService, StoreAppService>();
            services.AddScoped<IAuthorizationModelAppService, AuthorizationModelAppService>();
            services.AddScoped<IAuthorizationQueryAppService, AuthorizationQueryAppService>();
            services.AddScoped<IPermissionAppService, PermissionAppService>();
            services.AddScoped<IRelationshipService, RelationshipAppService>();
            services.AddScoped<IAssertionAppService, AssertionAppService>();
            services.AddScoped<IRbacAdminService, RbacAdminService>();
            services.AddSingleton<IPresetAppService, PresetAppService>();

            return services;
        }
    }
}
