using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;
using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Options;
using webprogbackend.Models;

namespace webprogbackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DeepSeekController : ControllerBase
    {
        private readonly HttpClient _httpClient;
        private readonly DeepSeekSettings _deepSeekSettings;
        private readonly ILogger<DeepSeekController> _logger;

        public DeepSeekController(
            IHttpClientFactory httpClientFactory,
            IOptions<DeepSeekSettings> deepSeekSettings,
            ILogger<DeepSeekController> logger)
        {
            _httpClient = httpClientFactory.CreateClient("DeepSeek");
            _deepSeekSettings = deepSeekSettings.Value;
            _logger = logger;

            // Constructor'da ayarları logla
            _logger.LogInformation($"DeepSeekController initialized with model: {_deepSeekSettings.DefaultModel}");
            _logger.LogInformation($"API Key configured: {(!string.IsNullOrEmpty(_deepSeekSettings.ApiKey))}");

            // API anahtarının doğru yüklendiğini kontrol et
            if (string.IsNullOrWhiteSpace(_deepSeekSettings.ApiKey))
            {
                _logger.LogError("DeepSeek API key is not configured!");
            }
            else if (_deepSeekSettings.ApiKey.Length < 20)
            {
                _logger.LogError("DeepSeek API key appears to be invalid (too short)");
            }
            else
            {
                _logger.LogInformation($"DeepSeek API key loaded: {_deepSeekSettings.ApiKey.Substring(0, 10)}...");
            }
        }

        /// <summary>
        /// Genel sohbet endpoint'i - E-ticaret odaklı asistan
        /// </summary>
        [HttpPost("chat")]
        public async Task<ActionResult<DeepSeekResponse>> Chat([FromBody] DeepSeekRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                if (string.IsNullOrWhiteSpace(_deepSeekSettings.ApiKey))
                {
                    return StatusCode(500, new { error = "DeepSeek API key not configured" });
                }

                var deepSeekRequest = new
                {
                    model = request.Model ?? _deepSeekSettings.DefaultModel,
                    messages = new[]
                    {
                        new
                        {
                            role = "system",
                            content = request.SystemPrompt ?? GetDefaultSystemPrompt()
                        },
                        new
                        {
                            role = "user",
                            content = request.Message
                        }
                    },
                    max_tokens = Math.Min(request.MaxTokens ?? 1000, 4000),
                    temperature = Math.Max(0.0, Math.Min(request.Temperature ?? 0.7, 2.0)),
                    top_p = Math.Max(0.0, Math.Min(request.TopP ?? 0.9, 1.0)),
                    stream = false
                };

                var response = await SendDeepSeekRequest(deepSeekRequest);
                return Ok(response);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP request failed while calling DeepSeek API");
                return StatusCode(500, new { error = "Network error occurred", details = ex.Message });
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to parse DeepSeek API response");
                return StatusCode(500, new { error = "Invalid response format from DeepSeek API", details = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred while processing DeepSeek request");
                return StatusCode(500, new { error = "An unexpected error occurred", details = ex.Message });
            }
        }

        /// <summary>
        /// Basit sohbet - E-ticaret müşteri desteği için optimize edilmiş
        /// </summary>
        [HttpPost("simple-chat")]
        public async Task<ActionResult<SimpleChatResponse>> SimpleChat([FromBody] SimpleChatRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var deepSeekRequest = new
                {
                    model = _deepSeekSettings.DefaultModel,
                    messages = new[]
                    {
                        new
                        {
                            role = "system",
                            content = GetECommerceSystemPrompt()
                        },
                        new
                        {
                            role = "user",
                            content = request.Message
                        }
                    },
                    max_tokens = 500,
                    temperature = 0.7,
                    top_p = 0.9,
                    stream = false
                };

                var result = await SendDeepSeekRequest(deepSeekRequest);

                return Ok(new SimpleChatResponse
                {
                    Response = result.Message,
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in simple chat");
                return StatusCode(500, new { error = "Chat service temporarily unavailable", details = ex.Message });
            }
        }

        /// <summary>
        /// Ürün önerisi - Kullanıcı ihtiyaçlarına göre ürün kategorileri önerir
        /// </summary>
        [HttpPost("product-recommendation")]
        public async Task<ActionResult<ProductRecommendationResponse>> GetProductRecommendation([FromBody] ProductRecommendationRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var systemPrompt = @"You are an expert e-commerce product recommendation assistant. 
                Based on user needs, recommend suitable product categories and provide helpful shopping advice.
                Focus on the categories available in our store: Bilgisayar, Telefon, Ses, Tablet, Giyilebilir, Depolama.
                Provide specific, actionable recommendations in Turkish.
                Keep responses concise and helpful.";

                var deepSeekRequest = new
                {
                    model = _deepSeekSettings.DefaultModel,
                    messages = new[]
                    {
                        new
                        {
                            role = "system",
                            content = systemPrompt
                        },
                        new
                        {
                            role = "user",
                            content = $"Kullanıcı ihtiyacı: {request.UserNeed}. Bütçe: {request.Budget:C}. Öneri ver."
                        }
                    },
                    max_tokens = 800,
                    temperature = 0.3,
                    stream = false
                };

                var result = await SendDeepSeekRequest(deepSeekRequest);

                return Ok(new ProductRecommendationResponse
                {
                    Recommendation = result.Message,
                    Categories = ExtractCategories(result.Message),
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in product recommendation");
                return StatusCode(500, new { error = "Recommendation service temporarily unavailable" });
            }
        }

        /// <summary>
        /// Metin analizi - Müşteri yorumları, geri bildirimler için
        /// </summary>
        [HttpPost("analyze-text")]
        [Authorize] // Sadece yetkili kullanıcılar
        public async Task<ActionResult<TextAnalysisResponse>> AnalyzeText([FromBody] TextAnalysisRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var systemPrompt = GetAnalysisSystemPrompt(request.AnalysisType);

                var deepSeekRequest = new
                {
                    model = _deepSeekSettings.DefaultModel,
                    messages = new[]
                    {
                        new
                        {
                            role = "system",
                            content = systemPrompt
                        },
                        new
                        {
                            role = "user",
                            content = request.Text
                        }
                    },
                    max_tokens = 1500,
                    temperature = 0.3,
                    stream = false
                };

                var result = await SendDeepSeekRequest(deepSeekRequest);

                return Ok(new TextAnalysisResponse
                {
                    AnalysisResult = result.Message,
                    AnalysisType = request.AnalysisType,
                    Confidence = CalculateConfidence(result.Usage.TotalTokens),
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in text analysis");
                return StatusCode(500, new { error = "Analysis service temporarily unavailable" });
            }
        }

        /// <summary>
        /// Kod inceleme - Admin kullanıcılar için
        /// </summary>
        [HttpPost("code-review")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<CodeReviewResponse>> ReviewCode([FromBody] CodeReviewRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var systemPrompt = $@"You are an expert code reviewer specializing in {request.Language ?? "general programming"}. 
                Review the provided code for:
                1. Security vulnerabilities
                2. Performance optimizations
                3. Best practices compliance
                4. Code quality and maintainability
                5. Potential bugs

                Provide specific, actionable feedback with examples.
                Focus on critical issues first.";

                var deepSeekRequest = new
                {
                    model = _deepSeekSettings.DefaultModel,
                    messages = new[]
                    {
                        new
                        {
                            role = "system",
                            content = systemPrompt
                        },
                        new
                        {
                            role = "user",
                            content = $"Review this {request.Language ?? "code"}:\n\n```{request.Language ?? "text"}\n{request.Code}\n```"
                        }
                    },
                    max_tokens = 2000,
                    temperature = 0.2,
                    stream = false
                };

                var result = await SendDeepSeekRequest(deepSeekRequest);

                return Ok(new CodeReviewResponse
                {
                    ReviewResult = result.Message,
                    Language = request.Language,
                    CodeLength = request.Code.Length,
                    ReviewTimestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in code review");
                return StatusCode(500, new { error = "Code review service temporarily unavailable" });
            }
        }

        /// <summary>
        /// API durumu kontrolü - Yeni API anahtarı ile test
        /// </summary>
        [HttpGet("status")]
        public async Task<ActionResult<ApiStatusResponse>> GetApiStatus()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_deepSeekSettings.ApiKey))
                {
                    return Ok(new ApiStatusResponse
                    {
                        Status = "error",
                        Message = "API key not configured",
                        Suggestion = "Please add a valid DeepSeek API key to appsettings.json",
                        Timestamp = DateTime.UtcNow
                    });
                }

                if (_deepSeekSettings.ApiKey.Contains("your-") || _deepSeekSettings.ApiKey.Length < 20)
                {
                    return Ok(new ApiStatusResponse
                    {
                        Status = "error",
                        Message = "Invalid API key format",
                        Suggestion = "Please replace with your actual OpenRouter API key",
                        Timestamp = DateTime.UtcNow
                    });
                }

                // DeepSeek V3 için test isteği
                var testRequest = new
                {
                    model = "deepseek/deepseek-chat-v3-0324:free",
                    messages = new[]
                    {
                        new { role = "user", content = "Hello, respond with 'API Working'" }
                    },
                    max_tokens = 10,
                    temperature = 0.1
                };

                try
                {
                    var result = await SendDeepSeekRequest(testRequest);
                    return Ok(new ApiStatusResponse
                    {
                        Status = "healthy",
                        Message = "DeepSeek V3 API is working correctly",
                        Suggestion = $"Response: {result.Message}",
                        Timestamp = DateTime.UtcNow
                    });
                }
                catch (HttpRequestException ex) when (ex.Message.Contains("Unauthorized"))
                {
                    return Ok(new ApiStatusResponse
                    {
                        Status = "error",
                        Message = "Invalid API key",
                        Suggestion = "Please verify your OpenRouter API key at https://openrouter.ai/keys",
                        Error = ex.Message,
                        Timestamp = DateTime.UtcNow
                    });
                }
                catch (HttpRequestException ex) when (ex.Message.Contains("Payment"))
                {
                    return Ok(new ApiStatusResponse
                    {
                        Status = "error",
                        Message = "Insufficient credits",
                        Suggestion = "Please add credits to your OpenRouter account at https://openrouter.ai/credits",
                        Error = ex.Message,
                        Timestamp = DateTime.UtcNow
                    });
                }
                catch (HttpRequestException ex) when (ex.Message.Contains("Rate limit"))
                {
                    return Ok(new ApiStatusResponse
                    {
                        Status = "warning",
                        Message = "Rate limit exceeded",
                        Suggestion = "Please wait before making more requests. Free tier has limits.",
                        Error = ex.Message,
                        Timestamp = DateTime.UtcNow
                    });
                }
                catch (Exception ex)
                {
                    return Ok(new ApiStatusResponse
                    {
                        Status = "error",
                        Message = "API test failed",
                        Error = ex.Message,
                        Timestamp = DateTime.UtcNow
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking API status");
                return Ok(new ApiStatusResponse
                {
                    Status = "error",
                    Message = "Status check failed",
                    Error = ex.Message,
                    Timestamp = DateTime.UtcNow
                });
            }
        }

        /// <summary>
        /// Debug endpoint - Ayarları kontrol etmek için
        /// </summary>
        [HttpGet("debug")]
        public ActionResult<object> Debug()
        {
            return Ok(new
            {
                Settings = new
                {
                    HasApiKey = !string.IsNullOrWhiteSpace(_deepSeekSettings.ApiKey),
                    ApiKeyLength = _deepSeekSettings.ApiKey?.Length ?? 0,
                    ApiKeyPrefix = _deepSeekSettings.ApiKey?.Length >= 10 ? _deepSeekSettings.ApiKey.Substring(0, 10) : "Invalid",
                    BaseUrl = _deepSeekSettings.BaseUrl,
                    DefaultModel = _deepSeekSettings.DefaultModel,
                    Timeout = _deepSeekSettings.Timeout,
                    HttpReferer = _deepSeekSettings.HttpReferer,
                    XTitle = _deepSeekSettings.XTitle
                },
                HttpClientInfo = new
                {
                    Timeout = _httpClient.Timeout.TotalMilliseconds,
                    BaseAddress = _httpClient.BaseAddress?.ToString() ?? "None",
                    Headers = _httpClient.DefaultRequestHeaders.ToDictionary(h => h.Key, h => string.Join(", ", h.Value))
                },
                Environment = new
                {
                    MachineName = Environment.MachineName,
                    AspNetCoreEnvironment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"),
                    Timestamp = DateTime.UtcNow
                }
            });
        }

        /// <summary>
        /// OpenRouter API key doğrulama ve bilgi endpoint'i
        /// </summary>
        [HttpGet("validate-key")]
        public async Task<ActionResult<object>> ValidateApiKey()
        {
            try
            {
                var apiKey = _deepSeekSettings.ApiKey;

                if (string.IsNullOrWhiteSpace(apiKey))
                {
                    return Ok(new
                    {
                        Status = "error",
                        Message = "API key not configured",
                        Suggestions = new[]
                        {
                            "1. Set DEEPSEEK_API_KEY environment variable",
                            "2. Or configure in appsettings.json",
                            "3. Get a new key from https://openrouter.ai/settings/keys"
                        }
                    });
                }

                // OpenRouter API ile basit doğrulama
                using var client = new HttpClient();
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

                // Models endpoint'ini test et (daha hafif)
                var response = await client.GetAsync("https://openrouter.ai/api/v1/models");
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    return Ok(new
                    {
                        Status = "success",
                        Message = "API key is valid and active",
                        KeyInfo = new
                        {
                            Prefix = apiKey.Substring(0, 10) + "...",
                            Length = apiKey.Length,
                            Format = apiKey.StartsWith("sk-or-") ? "OpenRouter" : "Unknown"
                        },
                        TestResult = "Models endpoint accessible",
                        Timestamp = DateTime.UtcNow
                    });
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    return Ok(new
                    {
                        Status = "error",
                        Message = "API key is invalid or deactivated",
                        StatusCode = response.StatusCode,
                        Suggestions = new[]
                        {
                            "1. Check if your key is active at https://openrouter.ai/settings/keys",
                            "2. Generate a new API key",
                            "3. Make sure the key doesn't have strikethrough (crossed out)"
                        },
                        Response = responseContent
                    });
                }
                else
                {
                    return Ok(new
                    {
                        Status = "error",
                        Message = $"API request failed: {response.StatusCode}",
                        Response = responseContent,
                        Timestamp = DateTime.UtcNow
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating API key");
                return Ok(new
                {
                    Status = "error",
                    Message = "Validation failed",
                    Error = ex.Message,
                    Timestamp = DateTime.UtcNow
                });
            }
        }

        /// <summary>
        /// Kullanılabilir modeller listesi
        /// </summary>
        [HttpGet("models")]
        public ActionResult<IEnumerable<string>> GetAvailableModels()
        {
            var models = new[]
            {
                "deepseek/deepseek-chat-v3-0324:free",
                "deepseek/deepseek-coder",
                "deepseek/deepseek-chat",
                "anthropic/claude-3-haiku:beta",
                "openai/gpt-3.5-turbo"
            };

            return Ok(models);
        }

        #region Private Methods

        private async Task<DeepSeekResponse> SendDeepSeekRequest(object request)
        {
            var jsonContent = JsonSerializer.Serialize(request, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_deepSeekSettings.ApiKey}");
            _httpClient.DefaultRequestHeaders.Add("HTTP-Referer", _deepSeekSettings.HttpReferer);
            _httpClient.DefaultRequestHeaders.Add("X-Title", _deepSeekSettings.XTitle);

            var response = await _httpClient.PostAsync(_deepSeekSettings.BaseUrl, content);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError($"DeepSeek API error: {response.StatusCode} - {errorContent}");

                var errorMessage = response.StatusCode switch
                {
                    System.Net.HttpStatusCode.Unauthorized => "Invalid API key or unauthorized access",
                    System.Net.HttpStatusCode.PaymentRequired => "Insufficient credits in OpenRouter account",
                    System.Net.HttpStatusCode.TooManyRequests => "Rate limit exceeded",
                    System.Net.HttpStatusCode.BadRequest => "Invalid request format",
                    _ => $"API call failed: {response.StatusCode}"
                };

                throw new HttpRequestException(errorMessage);
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<DeepSeekApiResponse>(responseContent, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            if (apiResponse?.Choices == null || !apiResponse.Choices.Any())
            {
                throw new InvalidOperationException("No response received from DeepSeek API");
            }

            return new DeepSeekResponse
            {
                Message = apiResponse.Choices[0].Message.Content,
                Model = apiResponse.Model,
                Usage = new UsageInfo
                {
                    PromptTokens = apiResponse.Usage?.PromptTokens ?? 0,
                    CompletionTokens = apiResponse.Usage?.CompletionTokens ?? 0,
                    TotalTokens = apiResponse.Usage?.TotalTokens ?? 0
                },
                CreatedAt = DateTime.UtcNow
            };
        }

        private string GetDefaultSystemPrompt()
        {
            return @"You are a helpful AI assistant for an e-commerce platform. 
            You help customers with product recommendations, shopping questions, and general inquiries.
            Be friendly, professional, and focus on providing valuable shopping assistance.
            Respond in Turkish if the user writes in Turkish, otherwise respond in English.";
        }

        private string GetECommerceSystemPrompt()
        {
            return @"Sen bir e-ticaret platformu için müşteri hizmetleri asistanısın. 
            Müşterilere ürün önerileri, alışveriş konularında yardım ve genel sorularını yanıtlıyorsun.
            Mağazamızda şu kategoriler bulunuyor: Bilgisayar, Telefon, Ses, Tablet, Giyilebilir, Depolama.
            Dostça, profesyonel ol ve değerli alışveriş yardımı sağlamaya odaklan.
            Yanıtlarını kısa ve öz tut.";
        }

        private string GetAnalysisSystemPrompt(string analysisType)
        {
            return analysisType.ToLower() switch
            {
                "sentiment" => "You are a sentiment analysis expert. Analyze the sentiment and provide detailed insights.",
                "summary" => "You are a text summarization expert. Provide concise, accurate summaries.",
                "keywords" => "You are a keyword extraction expert. Extract and rank important keywords.",
                "grammar" => "You are a grammar expert. Check for errors and suggest improvements.",
                _ => "You are a text analysis expert. Provide comprehensive analysis of the given text."
            };
        }

        private List<string> ExtractCategories(string recommendation)
        {
            var categories = new List<string> { "Bilgisayar", "Telefon", "Ses", "Tablet", "Giyilebilir", "Depolama" };
            return categories.Where(cat => recommendation.Contains(cat, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        private double CalculateConfidence(int totalTokens)
        {
            // Basit güven skoru hesaplama
            return Math.Min(0.95, 0.5 + (totalTokens / 1000.0) * 0.3);
        }

        #endregion
    }

    #region DTO Models

    public class DeepSeekRequest
    {
        [Required]
        [StringLength(4000, MinimumLength = 1)]
        public string Message { get; set; }

        public string? SystemPrompt { get; set; }

        public string? Model { get; set; }

        [Range(1, 4000)]
        public int? MaxTokens { get; set; }

        [Range(0.0, 2.0)]
        public double? Temperature { get; set; }

        [Range(0.0, 1.0)]
        public double? TopP { get; set; }
    }

    public class SimpleChatRequest
    {
        [Required]
        [StringLength(1000, MinimumLength = 1)]
        public string Message { get; set; }
    }

    public class ProductRecommendationRequest
    {
        [Required]
        [StringLength(500, MinimumLength = 5)]
        public string UserNeed { get; set; }

        [Range(0, 1000000)]
        public decimal Budget { get; set; }
    }

    public class TextAnalysisRequest
    {
        [Required]
        [StringLength(5000, MinimumLength = 10)]
        public string Text { get; set; }

        [Required]
        public string AnalysisType { get; set; } // sentiment, summary, keywords, grammar
    }

    public class CodeReviewRequest
    {
        [Required]
        [StringLength(10000, MinimumLength = 10)]
        public string Code { get; set; }

        public string? Language { get; set; }
    }

    // Response Models
    public class DeepSeekResponse
    {
        public string Message { get; set; }
        public string Model { get; set; }
        public UsageInfo Usage { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class SimpleChatResponse
    {
        public string Response { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class ProductRecommendationResponse
    {
        public string Recommendation { get; set; }
        public List<string> Categories { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class TextAnalysisResponse
    {
        public string AnalysisResult { get; set; }
        public string AnalysisType { get; set; }
        public double Confidence { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class CodeReviewResponse
    {
        public string ReviewResult { get; set; }
        public string Language { get; set; }
        public int CodeLength { get; set; }
        public DateTime ReviewTimestamp { get; set; }
    }

    public class ApiStatusResponse
    {
        public string Status { get; set; }
        public string Message { get; set; }
        public string? Suggestion { get; set; }
        public string? Error { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class UsageInfo
    {
        public int PromptTokens { get; set; }
        public int CompletionTokens { get; set; }
        public int TotalTokens { get; set; }
    }

    // Internal API Response Models
    internal class DeepSeekApiResponse
    {
        public string Id { get; set; }
        public string Object { get; set; }
        public long Created { get; set; }
        public string Model { get; set; }
        public Choice[] Choices { get; set; }
        public Usage Usage { get; set; }
    }

    internal class Choice
    {
        public int Index { get; set; }
        public Message Message { get; set; }
        public string FinishReason { get; set; }
    }

    internal class Message
    {
        public string Role { get; set; }
        public string Content { get; set; }
    }

    internal class Usage
    {
        public int PromptTokens { get; set; }
        public int CompletionTokens { get; set; }
        public int TotalTokens { get; set; }
    }

    #endregion
}