using Aegis.Application.DomainEvents;
using Aegis.Application.Interfaces;
using Aegis.Authorization.Core.Engine;
using Aegis.Authorization.Caching;
using Aegis.Authorization.Core.Interfaces;
using Aegis.Domain.Repositories;
using Aegis.Infrastructure.Authorization;
using Aegis.Infrastructure.DomainEvents;
using Aegis.Infrastructure.Identity;
using Aegis.Infrastructure.Persistence;
using Npgsql;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Aegis.Authorization.Core.Metrics;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.StackExchangeRedis;

namespace Aegis.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddAegisInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            var storageProvider = configuration.GetSection("Storage").GetValue<string>("Provider") ?? "InMemory";
            var cacheProvider = configuration.GetSection("Cache").GetValue<string>("Provider") ?? "Memory";
            var cacheTtlSeconds = configuration.GetSection("Cache").GetValue<int?>("DecisionTtlSeconds") ?? 15;

            if (cacheProvider.Equals("Redis", StringComparison.OrdinalIgnoreCase))
            {
                var redisConfiguration = configuration.GetSection("Cache:Redis").GetValue<string>("Configuration");
                if (string.IsNullOrWhiteSpace(redisConfiguration))
                {
                    throw new InvalidOperationException("Cache:Redis:Configuration is missing.");
                }

                services.AddStackExchangeRedisCache(options =>
                {
                    options.Configuration = redisConfiguration;
                    options.InstanceName = "aegis:";
                });
            }

            services.AddSingleton<IDomainEventOutboxStore, InMemoryDomainEventOutboxStore>();
            services.AddScoped<IDomainEventDispatcher, InProcessDomainEventDispatcher>();
            services.AddScoped(typeof(IDomainEventHandler<>), typeof(LoggingDomainEventHandler<>));
            services.AddScoped(typeof(IDomainEventHandler<>), typeof(PersistDomainEventToOutboxHandler<>));
            services.AddScoped<IOutboxProcessor, OutboxProcessor>();
            services.AddSingleton<IOutboxMessagePublisher, LoggingOutboxMessagePublisher>();
            services.AddHostedService<OutboxBackgroundService>();

            if (storageProvider.Equals("Postgres", StringComparison.OrdinalIgnoreCase))
            {
                var connectionString = configuration.GetConnectionString("Aegis");
                if (string.IsNullOrWhiteSpace(connectionString))
                {
                    throw new InvalidOperationException("ConnectionStrings:Aegis configuration is missing.");
                }

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
                services.AddSingleton<IRbacProvider>(sp => sp.GetRequiredService<InMemoryRbacStore>());
                services.AddSingleton<IRbacAdminStore>(sp => sp.GetRequiredService<InMemoryRbacStore>());
                services.AddSingleton<IAuditStore, InMemoryAuditStore>();
            }

            services.AddSingleton<IAuthorizationModelProvider, AuthorizationModelProvider>();

            // Register authorization metrics
            services.AddSingleton<IAuthorizationMetrics, InMemoryAuthorizationMetrics>();
            services.AddSingleton(sp => new AuthorizationCache(TimeSpan.FromSeconds(cacheTtlSeconds), sp.GetService<IDistributedCache>()));

            // Configure AuthorizationEngine with options from configuration (section: AuthorizationEngine)
            var authorizationEngineOptions = configuration.GetSection("AuthorizationEngine").Get<AuthorizationEngineOptions>() ?? new AuthorizationEngineOptions();
            services.AddScoped<IAuthorizationEngine>(sp =>
                new AuthorizationEngine(
                    sp.GetRequiredService<IRelationshipStore>(),
                    sp.GetRequiredService<IRbacProvider>(),
                    sp.GetRequiredService<IAuthorizationMetrics>(),
                    sp.GetService<IAuthorizationModelProvider>(),
                    sp.GetService<AuthorizationCache>(),
                    authorizationEngineOptions));
            services.AddSingleton<IAuthSessionService, JwtAuthSessionService>();

            return services;
        }
    }
}
