namespace webprogbackend.Models
{
    public class DeepSeekSettings
    {
        public string ApiKey { get; set; } = "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";
        public string BaseUrl { get; set; } = "https://openrouter.ai/api/v1/chat/completions";
        public string DefaultModel { get; set; } = "deepseek/deepseek-chat-v3-0324:free";
        public int Timeout { get; set; } = 60000; // milliseconds
        public int MaxRetries { get; set; } = 3;
        public string HttpReferer { get; set; } = "https://webprogbackend.com";
        public string XTitle { get; set; } = "Web Programming E-Commerce Backend";
    }
}