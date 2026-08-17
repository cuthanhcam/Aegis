using System.Reflection;
using System.Text.RegularExpressions;
using Aegis.Contracts.Common;

namespace Aegis.UnitTests.Contracts;

public sealed class NativeErrorCodesTests
{
    [Fact]
    public void Registry_ContainsUniqueUpperSnakeCaseConstants()
    {
        var codes = typeof(NativeErrorCodes)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(field => field.IsLiteral && !field.IsInitOnly)
            .Select(field => Assert.IsType<string>(field.GetRawConstantValue()))
            .ToArray();

        Assert.NotEmpty(codes);
        Assert.Equal(codes.Length, codes.Distinct(StringComparer.Ordinal).Count());
        Assert.All(codes, code => Assert.Matches(new Regex("^[A-Z][A-Z0-9]*(?:_[A-Z0-9]+)*$"), code));
    }
}
