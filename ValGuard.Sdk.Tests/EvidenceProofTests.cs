using ValGuard.Sdk;

namespace ValGuard.Sdk.Tests;

public class EvidenceProofTests
{
    private const string Secret = "contract-test-evidence-secret";
    private const string KeyId = "org:bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb:v1";
    private const string Payload =
        "{\"typ\":\"valguard-evidence/1\",\"request_id\":\"aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa\",\"org_id\":\"bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb\",\"checked_at\":\"2026-03-15T08:30:00Z\",\"engine_version\":\"valguard-engine/1\",\"policy_snapshot_hash\":\"policy-1\",\"request_digest\":\"sha256:758d61f26a44448384e5c4468a0dcb7a2abe456067b0f7b505bc28b9411fe931\",\"response_digest\":\"sha256:9795c5ff8937f23526ccb207a5684c1fc94a7854e19c021b39d944e51f5baef2\",\"verdicts\":[]}";
    private const string Signature = "sha256=35b87328b2f2c94b34658646f15e993fe900e1a79fe75558b4b6140d55b3003a";

    [Fact]
    public void Verify_contract_vector()
    {
        Assert.Equal("valguard-evidence/1", EvidenceProof.Typ);
        Assert.True(EvidenceProof.Verify(KeyId, Payload, Signature, Secret));
        Assert.False(EvidenceProof.Verify(KeyId, Payload, Signature + "x", Secret));
        Assert.False(EvidenceProof.Verify(KeyId, Payload, Signature, ""));
    }

    [Fact]
    public void VerifyEnvelope_json_download()
    {
        var json =
            "{\"key_id\":\"" + KeyId + "\",\"payload\":" + System.Text.Json.JsonSerializer.Serialize(Payload) +
            ",\"signature\":\"" + Signature + "\"}";
        Assert.True(EvidenceProof.VerifyEnvelope(json, Secret));
        Assert.False(EvidenceProof.VerifyEnvelope("{", Secret));
    }
}
