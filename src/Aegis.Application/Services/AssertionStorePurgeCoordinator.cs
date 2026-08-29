using Aegis.Application.Interfaces;

namespace Aegis.Application.Services
{
    public sealed class AssertionStorePurgeCoordinator
    {
        private readonly IAssertionRepository _assertionRepository;
        private readonly IAssertionRunStore _assertionRunStore;

        public AssertionStorePurgeCoordinator(
            IAssertionRepository assertionRepository,
            IAssertionRunStore assertionRunStore)
        {
            _assertionRepository = assertionRepository;
            _assertionRunStore = assertionRunStore;
        }

        public async Task PurgeStoreAsync(string storeId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(storeId))
            {
                throw new ArgumentException("storeId is required.");
            }

            await _assertionRepository.PurgeStoreAsync(storeId, cancellationToken);
            await _assertionRunStore.PurgeStoreAsync(storeId, cancellationToken);
        }
    }
}
