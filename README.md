# ValGuard .NET SDK

Thin client for the ValGuard OpenAI-compatible integrity layer.

Public GitHub: [github.com/valguard-ai/sdk-csharp](https://github.com/valguard-ai/sdk-csharp).

## Install

```bash
dotnet add package ValGuard.Sdk
```

From a GitHub clone (without NuGet):

```bash
git clone https://github.com/valguard-ai/sdk-csharp.git
dotnet add reference ./sdk-csharp/ValGuard.Sdk.csproj
```

## Environment

- `VG_API_KEY` — ValGuard API key
- `VG_PROXY` — layer base URL
- `VG_AGENT` — default agent slug (optional)

## Quick start

```csharp
using ValGuard.Sdk;

var vg = ValGuardClient.FromEnv().WithAgent("default");
var result = await vg.ChatCompletionsAsync(new ChatRequest(
    Model: "openai/gpt-4o-mini",
    Messages: [new ChatMessage("user", "Hello")]));

Console.WriteLine(result.Validation);
```

Pair with the official [OpenAI .NET SDK](https://github.com/openai/openai-dotnet) using headers from `BuildHeaders()`.
