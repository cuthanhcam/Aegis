using Aegis.Application.DomainEvents;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Aegis.Infrastructure.DomainEvents
{
    public sealed class OutboxBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<OutboxBackgroundService> _logger;
        private readonly OutboxWorkerOptions _options;

        public OutboxBackgroundService(
            IServiceProvider serviceProvider,
            ILogger<OutboxBackgroundService> logger,
            OutboxWorkerOptions options)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _options = options;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var processor = scope.ServiceProvider.GetRequiredService<IOutboxProcessor>();
                    await processor.ProcessPendingAsync(_options.BatchSize, stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Outbox processing failed.");
                }

                await Task.Delay(_options.PollInterval, stoppingToken);
            }
        }
    }
}
