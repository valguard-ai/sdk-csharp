using System.Text;
using System.Text.Json;

namespace ValGuard.Sdk;

public sealed record ChatMessage(string Role, string Content);

public sealed record ChatRequest(
    string Model,
    IReadOnlyList<ChatMessage> Messages,
    bool Stream = false,
    double? Temperature = null,
    int? MaxTokens = null);

public sealed record ChatResult(
    int Status,
    JsonElement? Data,
    ValidationHeaders Validation,
    ValGuardErrorInfo? Error);

public sealed record TestValidatorsRequest(
    string Output,
    string AgentSlug,
    string InputText = "",
    IReadOnlyList<object>? RagSources = null,
    int? ReaskAttempt = null);

public sealed record TestValidatorsResult(
    int Status,
    JsonElement? Data,
    ValidationHeaders Validation,
    ValGuardErrorInfo? Error);

public sealed class ValGuardClient
{
    private readonly string _apiKey;
    private readonly string _baseUrl;
    private readonly string _agentSlug;
    private readonly string? _flowSlug;
    private readonly string? _providerKey;
    private readonly string? _requestId;
    private readonly string? _internalSecret;
    private readonly string? _projectId;
    private readonly HttpClient _httpClient;

    private ValGuardClient(
        string apiKey,
        string baseUrl,
        string agentSlug,
        string? flowSlug,
        string? providerKey,
        string? requestId,
        string? internalSecret,
        string? projectId,
        HttpClient httpClient)
    {
        _apiKey = apiKey;
        _baseUrl = baseUrl.TrimEnd('/');
        _agentSlug = agentSlug;
        _flowSlug = flowSlug;
        _providerKey = providerKey;
        _requestId = requestId;
        _internalSecret = internalSecret;
        _projectId = projectId;
        _httpClient = httpClient;
    }

    public static ValGuardClient FromEnv(HttpClient? httpClient = null)
    {
        var apiKey = FirstEnv("VG_API_KEY", "VALGUARD_API_KEY");
        var baseUrl = FirstEnv("VG_PROXY", "VALGUARD_BASE_URL");
        var agentSlug = FirstEnv("VG_AGENT", "VALGUARD_AGENT");
        if (string.IsNullOrWhiteSpace(agentSlug))
        {
            agentSlug = "default";
        }
        var providerKey = FirstEnv("VG_PROVIDER_KEY", "OPENAI_API_KEY");
        var internalSecret = FirstEnv("INTERNAL_PROXY_SECRET", "VG_INTERNAL_SECRET");
        var projectId = FirstEnv("VG_PROJECT_ID", "VG_TEST_PROJECT_ID");

        if (string.IsNullOrWhiteSpace(apiKey) &&
            (string.IsNullOrWhiteSpace(internalSecret) || string.IsNullOrWhiteSpace(projectId)))
        {
            throw new InvalidOperationException(
                "Missing VG_API_KEY (or INTERNAL_PROXY_SECRET + VG_PROJECT_ID for internal test endpoints)");
        }
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            throw new InvalidOperationException("Missing VG_PROXY (layer base URL)");
        }

        return new ValGuardClient(
            apiKey,
            baseUrl,
            agentSlug,
            null,
            string.IsNullOrWhiteSpace(providerKey) ? null : providerKey,
            null,
            string.IsNullOrWhiteSpace(internalSecret) ? null : internalSecret,
            string.IsNullOrWhiteSpace(projectId) ? null : projectId,
            httpClient ?? new HttpClient());
    }

    public ValGuardClient WithAgent(string agentSlug) =>
        new(_apiKey, _baseUrl, agentSlug, null, _providerKey, _requestId, _internalSecret, _projectId, _httpClient);

    public ValGuardClient WithPlaybook(string flowSlug) =>
        new(_apiKey, _baseUrl, "", flowSlug, _providerKey, _requestId, _internalSecret, _projectId, _httpClient);

    public Dictionary<string, string> BuildHeaders(IDictionary<string, string>? extra = null)
    {
        if (!string.IsNullOrWhiteSpace(_flowSlug) && !string.IsNullOrWhiteSpace(_agentSlug))
        {
            throw new InvalidOperationException("mutually_exclusive_headers: use either WithAgent or WithPlaybook, not both");
        }
        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Authorization"] = $"Bearer {_apiKey}",
        };
        if (!string.IsNullOrWhiteSpace(_flowSlug))
        {
            headers["X-VG-Flow"] = _flowSlug;
        }
        else
        {
            headers["X-VG-Agent"] = string.IsNullOrWhiteSpace(_agentSlug) ? "default" : _agentSlug;
        }
        if (!string.IsNullOrWhiteSpace(_providerKey))
        {
            headers["X-Provider-Key"] = _providerKey;
        }
        if (!string.IsNullOrWhiteSpace(_requestId))
        {
            headers["X-Request-Id"] = _requestId;
        }
        if (extra != null)
        {
            foreach (var (key, value) in extra)
            {
                headers[key] = value;
            }
        }
        return headers;
    }

    public string ChatCompletionsUrl => $"{_baseUrl}/v1/chat/completions";

    public string TestValidatorsUrl => $"{_baseUrl}/internal/test-validators";

    public Dictionary<string, string> BuildInternalTestHeaders(IDictionary<string, string>? extra = null)
    {
        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (!string.IsNullOrWhiteSpace(_apiKey))
        {
            headers["Authorization"] = $"Bearer {_apiKey}";
        }
        if (!string.IsNullOrWhiteSpace(_internalSecret))
        {
            headers["X-VG-Internal-Secret"] = _internalSecret;
        }
        if (!string.IsNullOrWhiteSpace(_projectId))
        {
            headers["X-VG-Project-Id"] = _projectId;
        }
        if (!headers.ContainsKey("Authorization") &&
            (string.IsNullOrWhiteSpace(_internalSecret) || string.IsNullOrWhiteSpace(_projectId)))
        {
            throw new InvalidOperationException(
                "test_validators requires api key or both internal secret and project id");
        }
        if (extra != null)
        {
            foreach (var (key, value) in extra)
            {
                headers[key] = value;
            }
        }
        return headers;
    }

    public async Task<TestValidatorsResult> TestValidatorsAsync(
        TestValidatorsRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Output) || string.IsNullOrWhiteSpace(request.AgentSlug))
        {
            throw new ArgumentException("test_validators requires output and agent_slug");
        }

        var body = new Dictionary<string, object?>
        {
            ["output"] = request.Output,
            ["agent_slug"] = request.AgentSlug,
        };
        if (!string.IsNullOrWhiteSpace(request.InputText))
        {
            body["input_text"] = request.InputText;
        }
        if (request.RagSources is { Count: > 0 })
        {
            body["rag_sources"] = request.RagSources;
        }
        if (request.ReaskAttempt is not null)
        {
            body["reask_attempt"] = request.ReaskAttempt;
        }

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, TestValidatorsUrl)
        {
            Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json"),
        };

        foreach (var (key, value) in BuildInternalTestHeaders())
        {
            if (key.Equals("Content-Type", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }
            httpRequest.Headers.TryAddWithoutValidation(key, value);
        }

        using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
        var raw = await response.Content.ReadAsStringAsync(cancellationToken);

        JsonElement? data = null;
        if (!string.IsNullOrWhiteSpace(raw))
        {
            try
            {
                data = JsonSerializer.Deserialize<JsonElement>(raw);
            }
            catch (JsonException)
            {
                data = JsonSerializer.SerializeToElement(raw);
            }
        }

        var validation = ValidationHeaders.Parse(response.Headers);
        var error = ValGuardErrorParser.Parse((int)response.StatusCode, data);

        if (error is not null && !response.IsSuccessStatusCode)
        {
            throw new ValGuardException((int)response.StatusCode, error);
        }

        return new TestValidatorsResult((int)response.StatusCode, data, validation, error);
    }

    public async Task<ChatResult> ChatCompletionsAsync(
        ChatRequest request,
        CancellationToken cancellationToken = default)
    {
        var body = new Dictionary<string, object?>
        {
            ["model"] = request.Model,
            ["messages"] = request.Messages.Select(m => new { role = m.Role, content = m.Content }).ToArray(),
            ["stream"] = request.Stream,
        };
        if (request.Temperature is not null)
        {
            body["temperature"] = request.Temperature;
        }
        if (request.MaxTokens is not null)
        {
            body["max_tokens"] = request.MaxTokens;
        }

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, ChatCompletionsUrl)
        {
            Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json"),
        };

        foreach (var (key, value) in BuildHeaders())
        {
            if (key.Equals("Content-Type", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }
            httpRequest.Headers.TryAddWithoutValidation(key, value);
        }

        using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
        var raw = await response.Content.ReadAsStringAsync(cancellationToken);

        JsonElement? data = null;
        if (!string.IsNullOrWhiteSpace(raw))
        {
            try
            {
                data = JsonSerializer.Deserialize<JsonElement>(raw);
            }
            catch (JsonException)
            {
                data = JsonSerializer.SerializeToElement(raw);
            }
        }

        var validation = ValidationHeaders.Parse(response.Headers);
        var error = ValGuardErrorParser.Parse((int)response.StatusCode, data);

        if (error is { SoftFail: false } && !response.IsSuccessStatusCode)
        {
            throw new ValGuardException((int)response.StatusCode, error);
        }

        return new ChatResult((int)response.StatusCode, data, validation, error);
    }

    private static string FirstEnv(params string[] keys)
    {
        foreach (var key in keys)
        {
            var value = Environment.GetEnvironmentVariable(key);
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }
        return "";
    }
}
