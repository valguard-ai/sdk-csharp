using ValGuard.Sdk;

namespace ValGuard.Sdk.Tests;

public class ValidationHeadersTests
{
    [Fact]
    public void Parse_maps_core_headers()
    {
        var headers = new[]
        {
            new KeyValuePair<string, IEnumerable<string>>("X-VG-Validation-Status", new[] { "pass" }),
            new KeyValuePair<string, IEnumerable<string>>("X-VG-Latency-ms", new[] { "12" }),
            new KeyValuePair<string, IEnumerable<string>>("X-VG-Request-Id", new[] { "req-1" }),
        };

        var parsed = ValidationHeaders.Parse(headers);

        Assert.Equal(VgValidationStatus.Pass, parsed.ValidationStatus);
        Assert.Equal(12, parsed.LatencyMs);
        Assert.Equal("req-1", parsed.RequestId);
    }

    [Fact]
    public void Parse_maps_orchestration_headers()
    {
        var headers = new[]
        {
            new KeyValuePair<string, IEnumerable<string>>("X-VG-Target-Type", new[] { "playbook" }),
            new KeyValuePair<string, IEnumerable<string>>("X-VG-Orchestration-Path", new[] { "a,b,c" }),
            new KeyValuePair<string, IEnumerable<string>>("X-VG-Orchestration-Validations-Used", new[] { "5" }),
            new KeyValuePair<string, IEnumerable<string>>("X-VG-Requests-Remaining", new[] { "7" }),
            new KeyValuePair<string, IEnumerable<string>>("X-VG-Idempotency-Replayed", new[] { "true" }),
        };

        var parsed = ValidationHeaders.Parse(headers);

        Assert.Equal("playbook", parsed.TargetType);
        Assert.Equal("a,b,c", parsed.OrchestrationPath);
        Assert.Equal(5, parsed.OrchestrationValidationsUsed);
        Assert.Equal("7", parsed.OrchestrationExecutionsRemaining);
        Assert.True(parsed.IdempotencyReplayed);
    }

    [Fact]
    public void Parse_maps_repair_trace_and_run_headers()
    {
        var headers = new[]
        {
            new KeyValuePair<string, IEnumerable<string>>("X-VG-Validation-Status", new[] { "fail" }),
            new KeyValuePair<string, IEnumerable<string>>("X-VG-Healed", new[] { "true" }),
            new KeyValuePair<string, IEnumerable<string>>("X-VG-Healed-Rules", new[] { "tier1:trim_whitespace,tier2:enum_normalize" }),
            new KeyValuePair<string, IEnumerable<string>>("X-VG-Healed-Event-Count", new[] { "2" }),
            new KeyValuePair<string, IEnumerable<string>>("X-VG-Healed-Trace", new[] { "tier1:trim_whitespace@_,tier2:enum_normalize@status" }),
            new KeyValuePair<string, IEnumerable<string>>("X-VG-Cache", new[] { "cache_hit" }),
            new KeyValuePair<string, IEnumerable<string>>("X-Trace-Id", new[] { "trace-1" }),
            new KeyValuePair<string, IEnumerable<string>>("X-VG-Run-Status", new[] { "waiting_approval" }),
            new KeyValuePair<string, IEnumerable<string>>("X-VG-Approval-Id", new[] { "appr-1" }),
            new KeyValuePair<string, IEnumerable<string>>("X-VG-License-Mode", new[] { "enforce" }),
        };

        var parsed = ValidationHeaders.Parse(headers);

        Assert.Equal(VgValidationStatus.Fail, parsed.ValidationStatus);
        Assert.True(parsed.Healed);
        Assert.Equal(2, parsed.HealedEventCount);
        Assert.Equal(new[] { "tier1:trim_whitespace", "tier2:enum_normalize" }, parsed.HealedRules);
        Assert.Equal("cache_hit", parsed.CacheStatus);
        Assert.Equal("trace-1", parsed.TraceId);
        Assert.Equal("waiting_approval", parsed.RunStatus);
        Assert.Equal("appr-1", parsed.ApprovalId);
        Assert.Equal("enforce", parsed.LicenseMode);
    }

    [Fact]
    public void Parse_drops_reasked_ok_header()
    {
        var headers = new[]
        {
            new KeyValuePair<string, IEnumerable<string>>("X-VG-Validation-Status", new[] { "reasked_ok" }),
        };
        Assert.Null(ValidationHeaders.Parse(headers).ValidationStatus);
    }

    [Fact]
    public void BuildHeaders_sets_playbook_approval_and_trace()
    {
        Environment.SetEnvironmentVariable("VG_API_KEY", "vg_live_test");
        Environment.SetEnvironmentVariable("VG_PROXY", "https://api.example.com");
        Environment.SetEnvironmentVariable("VG_AGENT", "billing");

        var client = ValGuardClient.FromEnv()
            .WithPlaybook("invoice-processing")
            .WithIdempotencyKey("idem-1")
            .WithApprovalMode("async")
            .WithTrace("00-abc-def-01", "parent", "billing-agent");

        var headers = client.BuildHeaders();

        Assert.Equal("invoice-processing", headers["X-VG-Flow"]);
        Assert.False(headers.ContainsKey("X-VG-Agent"));
        Assert.Equal("idem-1", headers["X-VG-Idempotency-Key"]);
        Assert.Equal("async", headers["X-VG-Approval-Mode"]);
        Assert.Equal("00-abc-def-01", headers["traceparent"]);
        Assert.Equal("billing-agent", headers["X-VG-Agent-Id"]);
    }

    [Fact]
    public void BuildHeaders_sets_agent_and_auth()
    {
        Environment.SetEnvironmentVariable("VG_API_KEY", "vg_live_test");
        Environment.SetEnvironmentVariable("VG_PROXY", "https://api.example.com");
        Environment.SetEnvironmentVariable("VG_AGENT", "billing");

        var client = ValGuardClient.FromEnv();
        var headers = client.BuildHeaders();

        Assert.Equal("Bearer vg_live_test", headers["Authorization"]);
        Assert.Equal("billing", headers["X-VG-Agent"]);
    }
}
