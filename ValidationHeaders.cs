namespace ValGuard.Sdk;

public enum VgValidationStatus
{
    Pass,
    Warn,
    Block,
    Shadow,
    StreamPassthrough,
    Fail,
}

public sealed record ValidationHeaders(
    VgValidationStatus? ValidationStatus,
    bool ShadowMode,
    int? LatencyMs,
    string? RequestId,
    bool InputValidationShadow,
    string? ValidationPhase,
    bool Overage,
    string? TargetType,
    string? OrchestrationPath,
    int? OrchestrationValidationsUsed,
    string? OrchestrationExecutionsRemaining,
    bool IdempotencyReplayed,
    bool Healed,
    IReadOnlyList<string> HealedRules,
    int? HealedEventCount,
    IReadOnlyList<string> HealedTrace,
    string? CacheStatus,
    string? TraceId,
    string? SpanId,
    string? ParentSpanId,
    string? AgentId,
    string? IdempotentRunId,
    string? RunId,
    string? RunStatus,
    string? ApprovalId,
    string? LicenseStatus,
    string? LicenseMode)
{
    public static ValidationHeaders Parse(IEnumerable<KeyValuePair<string, IEnumerable<string>>> headers)
    {
        var map = headers.ToDictionary(
            h => h.Key,
            h => h.Value.FirstOrDefault() ?? "",
            StringComparer.OrdinalIgnoreCase);

        return new ValidationHeaders(
            ValidationStatus: ParseStatus(Optional(map, "x-vg-validation-status")),
            ShadowMode: Optional(map, "x-vg-shadow-mode") == "true",
            LatencyMs: ParseInt(Optional(map, "x-vg-latency-ms")),
            RequestId: Optional(map, "x-vg-request-id"),
            InputValidationShadow: Optional(map, "x-vg-input-validation") == "shadow",
            ValidationPhase: Optional(map, "x-vg-validation-phase") == "input" ? "input" : null,
            Overage: Optional(map, "x-vg-overage") == "true",
            TargetType: Optional(map, "x-vg-target-type"),
            OrchestrationPath: Optional(map, "x-vg-orchestration-path"),
            OrchestrationValidationsUsed: ParseInt(Optional(map, "x-vg-orchestration-validations-used")),
            OrchestrationExecutionsRemaining: Optional(map, "x-vg-requests-remaining"),
            IdempotencyReplayed: Optional(map, "x-vg-idempotency-replayed") == "true",
            Healed: Optional(map, "x-vg-healed") == "true",
            HealedRules: SplitCsv(Optional(map, "x-vg-healed-rules")),
            HealedEventCount: ParseInt(Optional(map, "x-vg-healed-event-count")),
            HealedTrace: SplitCsv(Optional(map, "x-vg-healed-trace")),
            CacheStatus: Optional(map, "x-vg-cache"),
            TraceId: Optional(map, "x-trace-id"),
            SpanId: Optional(map, "x-vg-span-id"),
            ParentSpanId: Optional(map, "x-vg-parent-span-id"),
            AgentId: Optional(map, "x-vg-agent-id"),
            IdempotentRunId: Optional(map, "x-vg-idempotent-run-id"),
            RunId: Optional(map, "x-vg-run-id"),
            RunStatus: Optional(map, "x-vg-run-status"),
            ApprovalId: Optional(map, "x-vg-approval-id"),
            LicenseStatus: Optional(map, "x-vg-license-status"),
            LicenseMode: Optional(map, "x-vg-license-mode"));
    }

    private static string? Optional(Dictionary<string, string> map, string key)
    {
        if (!map.TryGetValue(key, out var value) || string.IsNullOrEmpty(value))
        {
            return null;
        }
        return value;
    }

    private static VgValidationStatus? ParseStatus(string? statusRaw)
    {
        return statusRaw?.ToLowerInvariant() switch
        {
            "pass" => VgValidationStatus.Pass,
            "warn" => VgValidationStatus.Warn,
            "block" => VgValidationStatus.Block,
            "shadow" => VgValidationStatus.Shadow,
            "stream_passthrough" => VgValidationStatus.StreamPassthrough,
            "fail" => VgValidationStatus.Fail,
            _ => null,
        };
    }

    private static int? ParseInt(string? raw)
    {
        if (raw != null && int.TryParse(raw, out var parsed))
        {
            return parsed;
        }
        return null;
    }

    private static IReadOnlyList<string> SplitCsv(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return Array.Empty<string>();
        }
        return raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }
}
