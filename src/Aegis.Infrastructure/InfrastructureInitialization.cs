using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Aegis.Infrastructure
{
    public static class InfrastructureInitialization
    {
        public static Task InitializeAegisInfrastructureAsync(
            this IServiceProvider services,
            IConfiguration configuration,
            bool isDevelopment,
            CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }
}
