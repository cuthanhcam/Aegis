using Aegis.Application.Interfaces;
using Aegis.Application.Services;
using Aegis.Contracts.Administration;
using Moq;

namespace Aegis.UnitTests.Application.Services
{
    [Trait("Category", "ApplicationTests")]
    [Trait("Feature", "Services")]
    public class AuthorizationModelAppServiceTests
    {
        [Fact]
        public async Task ValidateAsync_WithValidModel_ReturnsSummary()
        {
            var service = CreateService();
            var request = new ValidateAuthorizationModelRequestDto(
                "1.1",
                """
                type user

                type document
                  relations
                    define viewer: [user]
                    define editor: viewer or owner
                    define owner: [user]
                """);

            var result = await service.ValidateAsync(request);

            Assert.True(result.Valid);
            Assert.Empty(result.Errors);
            Assert.Equal(2, result.Summary.TypeCount);
            Assert.Equal(3, result.Summary.RelationCount);
            Assert.Equal(2, result.Summary.DirectRelationCount);
            Assert.True(result.Summary.HasUnion);
        }

        [Fact]
        public async Task ValidateAsync_WithDuplicateRelation_ReturnsError()
        {
            var service = CreateService();
            var request = new ValidateAuthorizationModelRequestDto(
                "1.1",
                """
                type user

                type document
                  relations
                    define viewer: [user]
                    define viewer: owner
                """);

            var result = await service.ValidateAsync(request);

            Assert.False(result.Valid);
            Assert.Contains(result.Errors, error => error.Code == "DUPLICATE_RELATION");
        }

        [Fact]
        public async Task ValidateAsync_WithRelationOutsideType_ReturnsError()
        {
            var service = CreateService();
            var request = new ValidateAuthorizationModelRequestDto(
                "1.1",
                """
                define viewer: [user]
                type user
                """);

            var result = await service.ValidateAsync(request);

            Assert.False(result.Valid);
            Assert.Contains(result.Errors, error => error.Code == "RELATION_OUTSIDE_TYPE");
        }

        [Fact]
        public async Task ValidateAsync_WithMinimalModel_ReturnsWarningsOnly()
        {
            var service = CreateService();
            var request = new ValidateAuthorizationModelRequestDto("1.1", "type document");

            var result = await service.ValidateAsync(request);

            Assert.True(result.Valid);
            Assert.Empty(result.Errors);
            Assert.Contains(result.Warnings, warning => warning.Code == "RELATION_RECOMMENDED");
            Assert.Contains(result.Warnings, warning => warning.Code == "DIRECT_RELATION_RECOMMENDED");
        }

        private static AuthorizationModelAppService CreateService()
        {
            return new AuthorizationModelAppService(
                new Mock<IStoreRegistry>().Object,
                new Mock<IAuthorizationModelRegistry>().Object);
        }
    }
}
