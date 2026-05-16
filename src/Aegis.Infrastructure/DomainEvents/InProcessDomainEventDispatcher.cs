using Aegis.Application.DomainEvents;
using Aegis.SharedKernel;
using Microsoft.Extensions.DependencyInjection;

namespace Aegis.Infrastructure.DomainEvents
{
    public sealed class InProcessDomainEventDispatcher : IDomainEventDispatcher
    {
        private readonly IServiceProvider _serviceProvider;

        public InProcessDomainEventDispatcher(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public async Task DispatchAsync(IEnumerable<DomainEvent> domainEvents, CancellationToken cancellationToken = default)
        {
            foreach (var domainEvent in domainEvents)
            {
                var handlerType = typeof(IDomainEventHandler<>).MakeGenericType(domainEvent.GetType());
                var handlers = _serviceProvider.GetServices(handlerType);

                foreach (var handler in handlers)
                {
                    var method = handlerType.GetMethod(nameof(IDomainEventHandler<DomainEvent>.HandleAsync));
                    if (method is null)
                    {
                        continue;
                    }

                    var task = method.Invoke(handler, [domainEvent, cancellationToken]) as Task;
                    if (task is not null)
                    {
                        await task;
                    }
                }
            }
        }
    }
}
