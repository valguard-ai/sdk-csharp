namespace ValGuard.Sdk;

public enum VgValidationStatus
{
    Pass,
    Warn,
    Block,
    Shadow,
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
    bool IdempotencyReplayed)
{
    public static ValidationHeaders Parse(IEnumerable<KeyValuePair<string, IEnumerable<string>>> headers)
    {
        var map = headers.ToDictionary(
            h => h.Key,
            h => h.Value.FirstOrDefault() ?? "",
            StringComparer.OrdinalIgnoreCase);

        VgValidationStatus? status = null;
        if (map.TryGetValue("x-vg-validation-status", out var statusRaw))
        {
            status = statusRaw.ToLowerInvariant() switch
            {
                "pass" => VgValidationStatus.Pass,
                "warn" => VgValidationStatus.Warn,
                "block" => VgValidationStatus.Block,
                "shadow" => VgValidationStatus.Shadow,
                _ => null,
            };
        }

        int? latencyMs = null;
        if (map.TryGetValue("x-vg-latency-ms", out var latencyRaw) &&
            int.TryParse(latencyRaw, out var parsedLatency))
        {
            latencyMs = parsedLatency;
        }

        string? phase = map.GetValueOrDefault("x-vg-validation-phase") == "input" ? "input" : null;

        int? orchestrationValidationsUsed = null;
        if (map.TryGetValue("x-vg-orchestration-validations-used", out var validationsUsedRaw) &&
            int.TryParse(validationsUsedRaw, out var parsedValidationsUsed))
        {
            orchestrationValidationsUsed = parsedValidationsUsed;
        }

        return new ValidationHeaders(
            ValidationStatus: status,
            ShadowMode: map.GetValueOrDefault("x-vg-shadow-mode") == "true",
            LatencyMs: latencyMs,
            RequestId: map.GetValueOrDefault("x-vg-request-id"),
            InputValidationShadow: map.GetValueOrDefault("x-vg-input-validation") == "shadow",
            ValidationPhase: phase,
            Overage: map.GetValueOrDefault("x-vg-overage") == "true",
            TargetType: map.GetValueOrDefault("x-vg-target-type"),
            OrchestrationPath: map.GetValueOrDefault("x-vg-orchestration-path"),
            OrchestrationValidationsUsed: orchestrationValidationsUsed,
            OrchestrationExecutionsRemaining: map.GetValueOrDefault("x-vg-orchestration-executions-remaining"),
            IdempotencyReplayed: map.GetValueOrDefault("x-vg-idempotency-replayed") == "true");
    }
}
