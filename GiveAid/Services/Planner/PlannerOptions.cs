namespace GiveAid.Services.Planner
{
    /// <summary>
    /// Settings for the AI Programme Planner ("Planner" section of appsettings.json).
    ///
    ///   Provider = "Demo"   : built-in rules, no internet or API key needed (default).
    ///   Provider = "Claude" : Anthropic Claude Messages API (needs Planner:Claude:ApiKey).
    ///   Provider = "OpenAI" : any OpenAI-compatible chat API (OpenAI, Azure OpenAI, Groq,
    ///                         OpenRouter, a local Ollama server ...) - set BaseUrl, Model and ApiKey.
    /// If the chosen provider has no API key, the planner falls back to Demo mode and says so.
    /// </summary>
    public class PlannerOptions
    {
        public string Provider { get; set; } = "Demo";

        /// <summary>Most tool calls the AI may make in one plan (stops runaway loops).</summary>
        public int MaxToolCalls { get; set; } = 14;

        /// <summary>Time limit for one whole plan.</summary>
        public int TimeoutSeconds { get; set; } = 120;

        public ProviderOptions Claude { get; set; } = new()
        {
            BaseUrl = "https://api.anthropic.com",
            Model = "claude-sonnet-5-5"
        };

        public ProviderOptions OpenAI { get; set; } = new()
        {
            BaseUrl = "https://api.openai.com/v1",
            Model = ""
        };
    }

    public class ProviderOptions
    {
        public string BaseUrl { get; set; } = "";
        public string Model { get; set; } = "";
        public string ApiKey { get; set; } = "";
        public int MaxTokens { get; set; } = 2048;
    }
}
