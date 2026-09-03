using System.Text.Json;

namespace ValGuard.Sdk;

public sealed record ValGuardErrorInfo(
    string Type,
    string? Code,
    string? Message,
    string? Reason,
    object? Details,
    bool SoftFail,
    JsonElement? Body);

public sealed class ValGuardException : Exception
{
    public int Status { get; }
    public ValGuardErrorInfo Info { get; }

    public ValGuardException(int status, ValGuardErrorInfo info)
        : base(info.Message ?? info.Reason ?? $"ValGuard request failed ({status})")
    {
        Status = status;
        Info = info;
    }
}

public static class ValGuardErrorParser
{
    public static ValGuardErrorInfo? Parse(int status, JsonElement? body)
    {
        if (body is not { ValueKind: JsonValueKind.Object } root)
        {
            return null;
        }

        if (root.TryGetProperty("valid", out var validProp) &&
            validProp.ValueKind == JsonValueKind.False &&
            root.TryGetProperty("validation", out var validationProp))
        {
            return new ValGuardErrorInfo(
                Type: "validation_soft_fail",
                Code: null,
                Message: "response failed validation (http 200 envelope)",
                Reason: null,
                Details: validationProp,
                SoftFail: true,
                Body: root);
        }

        if (!root.TryGetProperty("error", out var errorProp) ||
            errorProp.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        var type = errorProp.TryGetProperty("type", out var typeProp) && typeProp.ValueKind == JsonValueKind.String
            ? typeProp.GetString() ?? "unknown"
            : "unknown";

        return new ValGuardErrorInfo(
            Type: type,
            Code: errorProp.TryGetProperty("code", out var codeProp) && codeProp.ValueKind == JsonValueKind.String
                ? codeProp.GetString()
                : null,
            Message: errorProp.TryGetProperty("message", out var msgProp) && msgProp.ValueKind == JsonValueKind.String
                ? msgProp.GetString()
                : null,
            Reason: errorProp.TryGetProperty("reason", out var reasonProp) && reasonProp.ValueKind == JsonValueKind.String
                ? reasonProp.GetString()
                : null,
            Details: errorProp.TryGetProperty("details", out var detailsProp) ? detailsProp : null,
            SoftFail: false,
            Body: root);
    }
}
