using Aegis.Application.DomainEvents;
using Aegis.Application.Interfaces;
using Aegis.Authorization.Core.Engine;
using Aegis.Authorization.Core.Interfaces;
using Aegis.Domain.Repositories;
using Aegis.Infrastructure.Authorization;
using Aegis.Infrastructure.DomainEvents;
using Aegis.Infrastructure.Identity;
using Aegis.Infrastructure.Persistence;
using Npgsql;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Aegis.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddAegisInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            var storageProvider = configuration.GetSection("Storage").GetValue<string>("Provider") ?? "InMemory";

            services.AddSingleton<IDomainEventOutboxStore, InMemoryDomainEventOutboxStore>();
            services.AddScoped<IDomainEventDispatcher, InProcessDomainEventDispatcher>();
            services.AddScoped(typeof(IDomainEventHandler<>), typeof(LoggingDomainEventHandler<>));
            services.AddScoped(typeof(IDomainEventHandler<>), typeof(PersistDomainEventToOutboxHandler<>));
            services.AddScoped<IOutboxProcessor, OutboxProcessor>();
            services.AddSingleton<IOutboxMessagePublisher, LoggingOutboxMessagePublisher>();
            services.AddHostedService<OutboxBackgroundService>();

            if (storageProvider.Equals("Postgres", StringComparison.OrdinalIgnoreCase))
            {
                var connectionString = configuration.GetConnectionString("Aegis")
                    ?? throw new InvalidOperationException("ConnectionStrings:Aegis configuration is missing.");

                services.AddSingleton(NpgsqlDataSource.Create(connectionString));
                services.AddSingleton<PostgresStoreRegistry>();
                services.AddSingleton<IStoreRegistry>(sp => sp.GetRequiredService<PostgresStoreRegistry>());
                services.AddSingleton<IStoreRepository>(sp => sp.GetRequiredService<PostgresStoreRegistry>());
                services.AddSingleton<IAuthorizationModelRegistry>(sp => sp.GetRequiredService<PostgresStoreRegistry>());
                services.AddSingleton<IAuthorizationModelRepository>(sp => sp.GetRequiredService<PostgresStoreRegistry>());
                services.AddSingleton<PostgresRelationshipStore>();
                services.AddSingleton<IRelationshipStore>(sp => sp.GetRequiredService<PostgresRelationshipStore>());
                services.AddSingleton<IRelationshipRepository>(sp => sp.GetRequiredService<PostgresRelationshipStore>());
                services.AddSingleton<PostgresRbacStore>();
                services.AddSingleton<IRbacProvider>(sp => sp.GetRequiredService<PostgresRbacStore>());
                services.AddSingleton<IRbacAdminStore>(sp => sp.GetRequiredService<PostgresRbacStore>());
                services.AddSingleton<PostgresAuditStore>();
                services.AddSingleton<IAuditStore>(sp => sp.GetRequiredService<PostgresAuditStore>());
            }
            else
            {
                services.AddSingleton<InMemoryStoreRegistry>();
                services.AddSingleton<IStoreRegistry>(sp => sp.GetRequiredService<InMemoryStoreRegistry>());
                services.AddSingleton<IStoreRepository>(sp => sp.GetRequiredService<InMemoryStoreRegistry>());
                services.AddSingleton<IAuthorizationModelRegistry>(sp => sp.GetRequiredService<InMemoryStoreRegistry>());
                services.AddSingleton<IAuthorizationModelRepository>(sp => sp.GetRequiredService<InMemoryStoreRegistry>());

                services.AddSingleton<IRelationshipStore, InMemoryRelationshipStore>();
                services.AddSingleton<IRelationshipRepository>(sp => sp.GetRequiredService<IRelationshipStore>() as IRelationshipRepository ?? throw new InvalidOperationException("Relationship repository is unavailable."));
                services.AddSingleton<InMemoryRbacStore>();
                services.AddSingleton<IRbacProvider, InMemoryRbacStore>();
                services.AddSingleton<IRbacAdminStore>(sp => sp.GetRequiredService<InMemoryRbacStore>());
                services.AddSingleton<IAuditStore, InMemoryAuditStore>();
            }

            services.AddSingleton<IAuthorizationModelProvider, AuthorizationModelProvider>();

            services.AddScoped<IAuthorizationEngine, AuthorizationEngine>();
            services.AddSingleton<IAuthSessionService, JwtAuthSessionService>();

            return services;
        }
    }
}
