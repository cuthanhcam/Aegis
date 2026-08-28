using System.Security.Cryptography;
using System.Text;
using Aegis.Contracts.Administration;

namespace Aegis.Api.Controllers.Helpers;

internal static class IdempotencyHeaders
{
    public static string? Validate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var key = value.Trim();
        if (key.Length is < 8 or > 128 || key.Any(character => !(char.IsAsciiLetterOrDigit(character) || ".:_-".Contains(character))))
        {
            throw new ArgumentException("Idempotency-Key must contain 8-128 ASCII letters, digits, '.', ':', '_' or '-'.");
        }

        return key;
    }

    public static string Fingerprint(CreateAuthorizationModelRequestDto request)
    {
        var schema = request.SchemaVersion?.Trim() ?? string.Empty;
        var model = request.Model?.Trim() ?? string.Empty;
        var canonical = $"{schema.Length}:{schema}{model.Length}:{model}";
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical))).ToLowerInvariant();
    }

    public static string Fingerprint(CreateStoreRequestDto request)
    {
        var name = request.Name?.Trim() ?? string.Empty;
        var canonical = $"{name.Length}:{name}";
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical))).ToLowerInvariant();
    }
}
