using Aegis.Application.Features.Query;

namespace Aegis.UnitTests.Application
{
    /// <summary>
    /// Tests for query use cases - critical for authorization engine performance
    /// </summary>
    public sealed class AuthorizationQueryValidationTests
    {
        [Theory]
        [InlineData("")]
        [InlineData(null)]
        [InlineData("   ")]
        public void ValidateCheckInput_WithEmptySubject_ShouldThrow(string? invalidSubject)
        {
            // This tests a critical validation path - authorization checks must have valid subject
            Assert.Throws<ArgumentException>(() =>
                AuthorizationQueryHelper.ValidateCheckInput(invalidSubject!, "relation", "object"));
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public void ValidateCheckInput_WithEmptyRelation_ShouldThrow(string? invalidRelation)
        {
            Assert.Throws<ArgumentException>(() =>
                AuthorizationQueryHelper.ValidateCheckInput("subject", invalidRelation!, "object"));
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public void ValidateCheckInput_WithEmptyObject_ShouldThrow(string? invalidObject)
        {
            Assert.Throws<ArgumentException>(() =>
                AuthorizationQueryHelper.ValidateCheckInput("subject", "relation", invalidObject!));
        }

        [Fact]
        public void ValidateCheckInput_WithValidInputs_ShouldNotThrow()
        {
            // Should not throw for valid inputs
            AuthorizationQueryHelper.ValidateCheckInput("user:alice", "editor", "doc:1");
        }
    }
}
