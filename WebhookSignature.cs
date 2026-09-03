using System.Security.Cryptography;
using System.Text;

namespace ValGuard.Sdk;

public static class WebhookSignature
{
    public const string SignatureHeader = "X-VG-Signature";

    public static string SignPayload(ReadOnlySpan<byte> rawBody, string secret)
    {
        var key = Encoding.UTF8.GetBytes(secret);
        var hash = HMACSHA256.HashData(key, rawBody);
        return "sha256=" + Convert.ToHexString(hash).ToLowerInvariant();
    }

    public static string SignPayload(string rawBody, string secret) =>
        SignPayload(Encoding.UTF8.GetBytes(rawBody), secret);

    public static bool Verify(ReadOnlySpan<byte> rawBody, string? signatureHeader, string secret)
    {
        if (string.IsNullOrEmpty(signatureHeader) || string.IsNullOrEmpty(secret))
        {
            return false;
        }

        var expected = SignPayload(rawBody, secret);
        var expectedBytes = Encoding.UTF8.GetBytes(expected);
        var actualBytes = Encoding.UTF8.GetBytes(signatureHeader);
        if (expectedBytes.Length != actualBytes.Length)
        {
            return false;
        }

        return CryptographicOperations.FixedTimeEquals(expectedBytes, actualBytes);
    }

    public static bool Verify(string rawBody, string? signatureHeader, string secret) =>
        Verify(Encoding.UTF8.GetBytes(rawBody), signatureHeader, secret);
}
