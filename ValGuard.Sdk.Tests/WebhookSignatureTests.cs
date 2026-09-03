using ValGuard.Sdk;

namespace ValGuard.Sdk.Tests;

public class WebhookSignatureTests
{
    private const string ContractSecret = "contract-test-webhook-secret";
    private const string ContractBody = "{\"event\":\"validation.block\",\"request_id\":\"abc\"}";
    private const string ContractSig =
        "sha256=6ef400c3c929c837d90cd6e99f082815966aee86cf10ac5f884e0f06cbcfd879";

    [Fact]
    public void Sign_and_verify_contract_vector()
    {
        Assert.Equal(WebhookSignature.SignatureHeader, "X-VG-Signature");
        Assert.Equal(ContractSig, WebhookSignature.SignPayload(ContractBody, ContractSecret));
        Assert.True(WebhookSignature.Verify(ContractBody, ContractSig, ContractSecret));
        Assert.False(WebhookSignature.Verify(ContractBody, "sha256=bad", ContractSecret));
    }
}

public class InternalTestHeadersTests
{
    [Fact]
    public void BuildInternalTestHeaders_supports_secret_auth()
    {
        Environment.SetEnvironmentVariable("VG_API_KEY", null);
        Environment.SetEnvironmentVariable("INTERNAL_PROXY_SECRET", "secret");
        Environment.SetEnvironmentVariable("VG_PROJECT_ID", "proj-1");
        Environment.SetEnvironmentVariable("VG_PROXY", "https://api.example.com");

        var client = ValGuardClient.FromEnv();
        var headers = client.BuildInternalTestHeaders();

        Assert.Equal("secret", headers["X-VG-Internal-Secret"]);
        Assert.Equal("proj-1", headers["X-VG-Project-Id"]);
        Assert.False(headers.ContainsKey("Authorization"));
    }
}
