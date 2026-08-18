using System.Globalization;
using System.Text;

namespace Aegis.Application.Contracts;

/// <summary>
/// Encodes native continuation state without exposing its representation as API contract.
/// Legacy numeric offsets remain readable for rolling upgrades but are never emitted.
/// </summary>
public static class ContinuationTokenCodec
{
    private const string VersionPrefix = "aegis:v1:";

    public static string EncodeOffset(int offset)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(offset);
        var payload = VersionPrefix + offset.ToString(CultureInfo.InvariantCulture);
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(payload))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    public static bool TryDecodeOffset(string? token, out int offset)
    {
        offset = 0;
        if (string.IsNullOrWhiteSpace(token))
        {
            return true;
        }

        // Temporary rolling-upgrade compatibility for tokens emitted before B1 governance.
        if (int.TryParse(token, NumberStyles.None, CultureInfo.InvariantCulture, out offset))
        {
            return offset >= 0;
        }

        try
        {
            var base64 = token.Replace('-', '+').Replace('_', '/');
            base64 = base64.PadRight(base64.Length + ((4 - base64.Length % 4) % 4), '=');
            var payload = Encoding.UTF8.GetString(Convert.FromBase64String(base64));
            if (!payload.StartsWith(VersionPrefix, StringComparison.Ordinal))
            {
                return false;
            }

            return int.TryParse(
                    payload.AsSpan(VersionPrefix.Length),
                    NumberStyles.None,
                    CultureInfo.InvariantCulture,
                    out offset)
                && offset >= 0;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
