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
            new KeyValuePair<string, IEnumerable<string>>("X-VG-Orchestration-Executions-Remaining", new[] { "7" }),
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
