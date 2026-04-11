using Aegis.Domain.ValueObjects;

namespace Aegis.UnitTests.Domain
{
    public sealed class ValueObjectValidationTests
    {
        // Subject can be direct subject or userset reference.
        [Fact]
        public void SubjectId_Create_ShouldAllowSubjectReference()
        {
            var subjectId = SubjectId.Create("user:charlie");

            Assert.Equal("user:charlie", subjectId.Value);
        }

        [Fact]
        public void SubjectId_Create_ShouldAllowUsersetReference()
        {
            var subjectId = SubjectId.Create("group:eng#member");

            Assert.Equal("group:eng#member", subjectId.Value);
        }

        [Fact]
        public void SubjectId_Create_ShouldTrimInput()
        {
            var subjectId = SubjectId.Create("  user:charlie  ");

            Assert.Equal("user:charlie", subjectId.Value);
        }

        [Fact]
        public void SubjectId_Create_ShouldThrow_WhenFormatInvalid()
        {
            Assert.Throws<ArgumentException>(() => SubjectId.Create("invalid-subject"));
        }

        [Theory]
        [InlineData("user:")]
        [InlineData(":charlie")]
        [InlineData("1user:charlie")]
        [InlineData("user:charlie#")]
        [InlineData("user:charlie#1viewer")]
        public void SubjectId_TryCreate_ShouldReturnFalse_ForInvalidCases(string value)
        {
            var ok = SubjectId.TryCreate(value, out _);

            Assert.False(ok);
        }

        [Fact]
        public void ObjectId_Create_ShouldAllowObjectReference()
        {
            var objectId = ObjectId.Create("document:spec");

            Assert.Equal("document:spec", objectId.Value);
        }

        [Fact]
        public void ObjectId_Create_ShouldThrow_WhenUsersetMarkerExists()
        {
            Assert.Throws<ArgumentException>(() => ObjectId.Create("group:eng#member"));
        }

        [Theory]
        [InlineData("document")]
        [InlineData("document:")]
        [InlineData(":spec")]
        [InlineData("document#viewer")]
        [InlineData("document:spec#viewer")]
        [InlineData("1document:spec")]
        public void ObjectId_TryCreate_ShouldReturnFalse_ForInvalidCases(string value)
        {
            var ok = ObjectId.TryCreate(value, out _);

            Assert.False(ok);
        }

        [Fact]
        public void ObjectId_Create_ShouldTrimInput()
        {
            var objectId = ObjectId.Create("  document:spec  ");

            Assert.Equal("document:spec", objectId.Value);
        }

        [Fact]
        public void TryCreate_ShouldReturnFalse_WhenInputIsWhitespace()
        {
            var subjectOk = SubjectId.TryCreate("   ", out _);
            var objectOk = ObjectId.TryCreate("   ", out _);

            Assert.False(subjectOk);
            Assert.False(objectOk);
        }

        [Fact]
        public void RelationName_Create_ShouldAllowValidName()
        {
            var relation = RelationName.Create("viewer_role");

            Assert.Equal("viewer_role", relation.Value);
        }

        [Fact]
        public void RelationName_Create_ShouldThrow_WhenNameStartsWithDigit()
        {
            Assert.Throws<ArgumentException>(() => RelationName.Create("1viewer"));
        }

        [Theory]
        [InlineData("viewer.role")]
        [InlineData("viewer role")]
        [InlineData("viewer#role")]
        public void RelationName_TryCreate_ShouldReturnFalse_ForInvalidCases(string value)
        {
            var ok = RelationName.TryCreate(value, out _);

            Assert.False(ok);
        }

        [Fact]
        public void ResourceTypeName_Create_ShouldAllowValidName()
        {
            var resourceType = ResourceTypeName.Create("document-type");

            Assert.Equal("document-type", resourceType.Value);
        }

        [Fact]
        public void ResourceTypeName_Create_ShouldThrow_WhenInvalidCharacterExists()
        {
            Assert.Throws<ArgumentException>(() => ResourceTypeName.Create("document.type"));
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("1document")]
        [InlineData("document.type")]
        [InlineData("document type")]
        public void ResourceTypeName_TryCreate_ShouldReturnFalse_ForInvalidCases(string value)
        {
            var ok = ResourceTypeName.TryCreate(value, out _);

            Assert.False(ok);
        }

        [Fact]
        public void ValueObjects_WithSameValue_ShouldBeEqual()
        {
            var left = SubjectId.Create("user:charlie");
            var right = SubjectId.Create("user:charlie");

            Assert.Equal(left, right);
            Assert.Equal(left.GetHashCode(), right.GetHashCode());
        }
    }
}
