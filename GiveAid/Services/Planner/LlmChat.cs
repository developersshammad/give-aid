using System.Net.Http.Headers;
using System.Text;
using System.Text.Json.Nodes;

namespace GiveAid.Services.Planner
{
    /// <summary>A tool the AI asked to call.</summary>
    public class LlmToolCall
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        /// <summary>The arguments exactly as the AI sent them (a JSON object as text).</summary>
        public string ArgumentsJson { get; set; } = "{}";
    }

    /// <summary>One reply from the AI: some text and/or tool calls.</summary>
    public class LlmTurn
    {
        public string Text { get; set; } = "";
        public List<LlmToolCall> ToolCalls { get; } = new();
        public string? StopReason { get; set; }
    }

    /// <summary>A tool definition: name, description and a JSON Schema for its input.</summary>
    public record LlmTool(string Name, string Description, JsonObject Schema);

    public class LlmException : Exception
    {
        public LlmException(string message) : base(message) { }
    }

    /// <summary>
    /// A conversation with an AI model that can call tools. Two implementations:
    /// Anthropic Claude (Messages API) and any OpenAI-compatible Chat Completions API.
    /// The planner code above them is the same for both.
    /// </summary>
    public interface ILlmChat
    {
        string ProviderName { get; }
        void Start(string system, string userMessage, IReadOnlyList<LlmTool> tools);
        Task<LlmTurn> NextAsync(CancellationToken ct);
        void AddToolResult(LlmToolCall call, string content, bool isError);
    }

    // =====================================================================
    //  Anthropic Claude - POST {BaseUrl}/v1/messages
    // =====================================================================
    public class ClaudeChat : ILlmChat
    {
        private readonly HttpClient _http;
        private readonly ProviderOptions _o;
        private readonly JsonArray _messages = new();
        private readonly JsonArray _pendingResults = new();
        private string _system = "";
        private JsonArray _tools = new();

        public ClaudeChat(HttpClient http, ProviderOptions options)
        {
            _http = http;
            _o = options;
        }

        public string ProviderName => "Claude";

        public void Start(string system, string userMessage, IReadOnlyList<LlmTool> tools)
        {
            _system = system;
            _tools = new JsonArray(tools.Select(t => (JsonNode)new JsonObject
            {
                ["name"] = t.Name,
                ["description"] = t.Description,
                ["input_schema"] = t.Schema.DeepClone()
            }).ToArray());
            _messages.Add(new JsonObject { ["role"] = "user", ["content"] = userMessage });
        }

        public async Task<LlmTurn> NextAsync(CancellationToken ct)
        {
            // all tool results of the previous turn go back together in ONE user message
            if (_pendingResults.Count > 0)
            {
                _messages.Add(new JsonObject { ["role"] = "user", ["content"] = _pendingResults.DeepClone() });
                _pendingResults.Clear();
            }

            var body = new JsonObject
            {
                ["model"] = _o.Model,
                ["max_tokens"] = _o.MaxTokens,
                ["system"] = _system,
                ["tools"] = _tools.DeepClone(),
                ["messages"] = _messages.DeepClone()
            };
            using var req = new HttpRequestMessage(HttpMethod.Post, _o.BaseUrl.TrimEnd('/') + "/v1/messages")
            {
                Content = new StringContent(body.ToJsonString(), Encoding.UTF8, "application/json")
            };
            req.Headers.Add("x-api-key", _o.ApiKey);
            req.Headers.Add("anthropic-version", "2023-06-01");

            using var res = await _http.SendAsync(req, ct);
            var text = await res.Content.ReadAsStringAsync(ct);
            if (!res.IsSuccessStatusCode)
                throw new LlmException($"Claude API error {(int)res.StatusCode}: {ErrorMessage(text)}");

            var json = JsonNode.Parse(text) ?? throw new LlmException("Claude API returned an empty reply.");
            var content = json["content"]?.AsArray() ?? new JsonArray();
            _messages.Add(new JsonObject { ["role"] = "assistant", ["content"] = content.DeepClone() });

            var turn = new LlmTurn { StopReason = json["stop_reason"]?.GetValue<string>() };
            var sb = new StringBuilder();
            foreach (var block in content)
            {
                var type = block?["type"]?.GetValue<string>();
                if (type == "text") sb.AppendLine(block!["text"]?.GetValue<string>());
                else if (type == "tool_use")
                    turn.ToolCalls.Add(new LlmToolCall
                    {
                        Id = block!["id"]!.GetValue<string>(),
                        Name = block["name"]!.GetValue<string>(),
                        ArgumentsJson = block["input"]?.ToJsonString() ?? "{}"
                    });
            }
            turn.Text = sb.ToString().Trim();
            return turn;
        }

        public void AddToolResult(LlmToolCall call, string content, bool isError) =>
            _pendingResults.Add(new JsonObject
            {
                ["type"] = "tool_result",
                ["tool_use_id"] = call.Id,
                ["content"] = content,
                ["is_error"] = isError
            });

        internal static string ErrorMessage(string body)
        {
            try { return JsonNode.Parse(body)?["error"]?["message"]?.GetValue<string>() ?? Trim(body); }
            catch { return Trim(body); }
        }
        internal static string Trim(string s) => s.Length > 300 ? s[..300] + "..." : s;
    }

    // =====================================================================
    //  OpenAI-compatible - POST {BaseUrl}/chat/completions
    //  (OpenAI, Azure OpenAI, Groq, OpenRouter, a local Ollama server, ...)
    // =====================================================================
    public class OpenAiChat : ILlmChat
    {
        private readonly HttpClient _http;
        private readonly ProviderOptions _o;
        private readonly JsonArray _messages = new();
        private JsonArray _tools = new();

        public OpenAiChat(HttpClient http, ProviderOptions options)
        {
            _http = http;
            _o = options;
        }

        public string ProviderName => "OpenAI";

        public void Start(string system, string userMessage, IReadOnlyList<LlmTool> tools)
        {
            _tools = new JsonArray(tools.Select(t => (JsonNode)new JsonObject
            {
                ["type"] = "function",
                ["function"] = new JsonObject
                {
                    ["name"] = t.Name,
                    ["description"] = t.Description,
                    ["parameters"] = t.Schema.DeepClone()
                }
            }).ToArray());
            _messages.Add(new JsonObject { ["role"] = "system", ["content"] = system });
            _messages.Add(new JsonObject { ["role"] = "user", ["content"] = userMessage });
        }

        public async Task<LlmTurn> NextAsync(CancellationToken ct)
        {
            var body = new JsonObject
            {
                ["model"] = _o.Model,
                ["messages"] = _messages.DeepClone(),
                ["tools"] = _tools.DeepClone(),
                ["tool_choice"] = "auto"
            };
            using var req = new HttpRequestMessage(HttpMethod.Post, _o.BaseUrl.TrimEnd('/') + "/chat/completions")
            {
                Content = new StringContent(body.ToJsonString(), Encoding.UTF8, "application/json")
            };
            if (!string.IsNullOrEmpty(_o.ApiKey))          // a local Ollama server needs no key
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _o.ApiKey);

            using var res = await _http.SendAsync(req, ct);
            var text = await res.Content.ReadAsStringAsync(ct);
            if (!res.IsSuccessStatusCode)
                throw new LlmException($"AI API error {(int)res.StatusCode}: {ClaudeChat.ErrorMessage(text)}");

            var choice = JsonNode.Parse(text)?["choices"]?[0] ?? throw new LlmException("AI API returned no choices.");
            var message = choice["message"] ?? throw new LlmException("AI API returned no message.");
            var turn = new LlmTurn
            {
                StopReason = choice["finish_reason"]?.GetValue<string>(),
                Text = message["content"] is JsonValue v && v.TryGetValue<string>(out var s) ? s.Trim() : ""
            };

            // keep only the fields every compatible server accepts back
            var echo = new JsonObject { ["role"] = "assistant", ["content"] = turn.Text.Length > 0 ? turn.Text : null };
            if (message["tool_calls"] is JsonArray calls && calls.Count > 0)
            {
                echo["tool_calls"] = calls.DeepClone();
                foreach (var c in calls)
                    turn.ToolCalls.Add(new LlmToolCall
                    {
                        Id = c!["id"]?.GetValue<string>() ?? Guid.NewGuid().ToString("N"),
                        Name = c["function"]!["name"]!.GetValue<string>(),
                        ArgumentsJson = c["function"]!["arguments"]?.GetValue<string>() ?? "{}"
                    });
            }
            _messages.Add(echo);
            return turn;
        }

        public void AddToolResult(LlmToolCall call, string content, bool isError) =>
            _messages.Add(new JsonObject
            {
                ["role"] = "tool",
                ["tool_call_id"] = call.Id,
                ["content"] = isError ? "ERROR: " + content : content
            });
    }
}
