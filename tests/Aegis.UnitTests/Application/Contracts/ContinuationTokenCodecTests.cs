using Aegis.Application.Contracts;

namespace Aegis.UnitTests.Application.Contracts;

public sealed class ContinuationTokenCodecTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(50)]
    [InlineData(int.MaxValue)]
    public void EncodeOffset_RoundTripsWithoutExposingNumericRepresentation(int offset)
    {
        var token = ContinuationTokenCodec.EncodeOffset(offset);

        Assert.False(int.TryParse(token, out _));
        Assert.True(ContinuationTokenCodec.TryDecodeOffset(token, out var decoded));
        Assert.Equal(offset, decoded);
    }

    [Fact]
    public void TryDecodeOffset_AcceptsLegacyNonNegativeNumericToken()
    {
        Assert.True(ContinuationTokenCodec.TryDecodeOffset("50", out var offset));
        Assert.Equal(50, offset);
    }

    [Theory]
    [InlineData("-1")]
    [InlineData("not-a-token")]
    [InlineData("YXV0aDp2Mjo1MA")]
    public void TryDecodeOffset_RejectsInvalidOrUnsupportedToken(string token)
    {
        Assert.False(ContinuationTokenCodec.TryDecodeOffset(token, out _));
    }
}
