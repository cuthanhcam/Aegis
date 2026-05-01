using Aegis.Application.Features.Permissions;
using Aegis.Application.Interfaces;
using Aegis.Authorization.Core.Interfaces;
using Moq;

namespace Aegis.UnitTests.Application
{
    /// <summary>
    /// Risk Test: Null dependency injection in services
    /// If dependencies are null, service fails at unpredictable times
    /// </summary>
    public sealed class ServiceDependencyInjectionTests
    {
        [Fact]
        public void ResolveAuthorizationModelUseCase_ShouldRequireStoreRegistry()
        {
            // Risk: Constructor doesn't validate null dependencies
            Assert.Throws<ArgumentNullException>(() =>
                new ResolveAuthorizationModelUseCase(null!, new Mock<IAuthorizationModelRegistry>().Object));
        }

        [Fact]
        public void ResolveAuthorizationModelUseCase_ShouldRequireModelRegistry()
        {
            // Risk: Constructor doesn't validate null dependencies
            Assert.Throws<ArgumentNullException>(() =>
                new ResolveAuthorizationModelUseCase(new Mock<IStoreRegistry>().Object, null!));
        }

        [Fact]
        public void CheckPermissionUseCase_ShouldRequireAuthEngine()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new CheckPermissionUseCase(null!, new Mock<IAuditStore>().Object));
        }

        [Fact]
        public void CheckPermissionUseCase_ShouldRequireAuditStore()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new CheckPermissionUseCase(new Mock<IAuthorizationEngine>().Object, null!));
        }
    }
}
