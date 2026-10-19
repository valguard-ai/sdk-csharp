using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace ValGuard.Sdk;

/// <summary>
/// Offline HMAC check for a ValGuard evidence envelope.
/// Matches <c>valguard-cli verify-proof</c>: org key is HMAC-SHA256(secret, key_id),
/// signature is <c>sha256=</c> plus HMAC-SHA256(org key, exact payload bytes).
/// </summary>
public static class EvidenceProof
{
    public const string Typ = "valguard-evidence/1";

    public static bool Verify(string? keyId, string? payload, string? signature, string? secret)
    {
        if (string.IsNullOrEmpty(keyId) || string.IsNullOrEmpty(payload) ||
            string.IsNullOrEmpty(signature) || string.IsNullOrEmpty(secret))
        {
            return false;
        }

        var orgKey = HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), Encoding.UTF8.GetBytes(keyId));
        var mac = HMACSHA256.HashData(orgKey, Encoding.UTF8.GetBytes(payload));
        var expected = "sha256=" + Convert.ToHexString(mac).ToLowerInvariant();
        var got = signature.Trim();
        var left = Encoding.UTF8.GetBytes(expected);
        var right = Encoding.UTF8.GetBytes(got);
        return left.Length == right.Length && CryptographicOperations.FixedTimeEquals(left, right);
    }

    /// <summary>
    /// Verify a dashboard JSON download containing key_id, payload, and signature.
    /// </summary>
    public static bool VerifyEnvelope(string json, string secret)
    {
        if (string.IsNullOrWhiteSpace(json)) return false;
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            var keyId = root.TryGetProperty("key_id", out var key) ? key.GetString() : null;
            var payload = root.TryGetProperty("payload", out var body) ? body.GetString() : null;
            var signature = root.TryGetProperty("signature", out var sig) ? sig.GetString() : null;
            return Verify(keyId, payload, signature, secret);
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
