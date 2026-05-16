using Aegis.Application.DomainEvents;
using Aegis.Application.Interfaces;
using Aegis.Authorization.Core.Engine;
using Aegis.Authorization.Core.Interfaces;
using Aegis.Domain.Repositories;
using Aegis.Infrastructure.Authorization;
using Aegis.Infrastructure.DomainEvents;
using Aegis.Infrastructure.Identity;
using Aegis.Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Aegis.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddAegisInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddSingleton<IDomainEventOutboxStore, InMemoryDomainEventOutboxStore>();
            services.AddScoped<IDomainEventDispatcher, InProcessDomainEventDispatcher>();
            services.AddScoped(typeof(IDomainEventHandler<>), typeof(LoggingDomainEventHandler<>));
            services.AddScoped(typeof(IDomainEventHandler<>), typeof(PersistDomainEventToOutboxHandler<>));
            services.AddScoped<IOutboxProcessor, OutboxProcessor>();
            services.AddSingleton<IOutboxMessagePublisher, LoggingOutboxMessagePublisher>();
            services.AddHostedService<OutboxBackgroundService>();

            services.AddSingleton<InMemoryStoreRegistry>();
            services.AddSingleton<IStoreRegistry>(sp => sp.GetRequiredService<InMemoryStoreRegistry>());
            services.AddSingleton<IStoreRepository>(sp => sp.GetRequiredService<InMemoryStoreRegistry>());
            services.AddSingleton<IAuthorizationModelRegistry>(sp => sp.GetRequiredService<InMemoryStoreRegistry>());
            services.AddSingleton<IAuthorizationModelRepository>(sp => sp.GetRequiredService<InMemoryStoreRegistry>());
            services.AddSingleton<IAuthorizationModelProvider, AuthorizationModelProvider>();

            services.AddSingleton<IRelationshipStore, InMemoryRelationshipStore>();
            services.AddSingleton<IRelationshipRepository>(sp => sp.GetRequiredService<IRelationshipStore>() as IRelationshipRepository ?? throw new InvalidOperationException("Relationship repository is unavailable."));
            services.AddSingleton<IRbacProvider, InMemoryRbacStore>();
            services.AddSingleton<IRbacAdminStore>(sp => sp.GetRequiredService<InMemoryRbacStore>());
            services.AddSingleton<IAuditStore, InMemoryAuditStore>();

            services.AddScoped<IAuthorizationEngine, AuthorizationEngine>();
            services.AddSingleton<IAuthSessionService, JwtAuthSessionService>();

            return services;
        }
    }
}
