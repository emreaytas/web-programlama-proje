using Stripe;
using webprogbackend.Models;

namespace webprogbackend.Services.Payment
{
    public interface IStripePaymentService
    {
        Task<PaymentResult> ProcessPaymentAsync(PaymentRequest request);
        Task<RefundResult> ProcessRefundAsync(string paymentIntentId);
    }

    public class StripePaymentService : IStripePaymentService
    {
        private readonly string _stripeSecretKey;
        private readonly ILogger<StripePaymentService> _logger;

        public StripePaymentService(IConfiguration configuration, ILogger<StripePaymentService> logger)
        {
            _stripeSecretKey = configuration["Stripe:SecretKey"];
            _logger = logger;
            StripeConfiguration.ApiKey = _stripeSecretKey;
        }

        public async Task<PaymentResult> ProcessPaymentAsync(PaymentRequest request)
        {
            try
            {
                // Simüle edilmiş ödeme işlemi
                var paymentIntent = new PaymentIntent
                {
                    Id = $"pi_{Guid.NewGuid().ToString("N")}",
                    Amount = (long)(request.Amount * 100), // Stripe kuruş cinsinden çalışır
                    Currency = "try",
                    Status = "succeeded",
                    Created = DateTime.UtcNow
                };

                // Başarılı ödeme simülasyonu
                return new PaymentResult
                {
                    Success = true,
                    PaymentIntentId = paymentIntent.Id,
                    Amount = request.Amount,
                    Currency = "try",
                    Status = "succeeded",
                    Message = "Ödeme başarıyla tamamlandı"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ödeme işlemi sırasında hata oluştu");
                return new PaymentResult
                {
                    Success = false,
                    Message = "Ödeme işlemi sırasında bir hata oluştu"
                };
            }
        }

        public async Task<RefundResult> ProcessRefundAsync(string paymentIntentId)
        {
            try
            {
                // Simüle edilmiş iade işlemi
                var refund = new Refund
                {
                    Id = $"re_{Guid.NewGuid().ToString("N")}",
                    PaymentIntentId = paymentIntentId,
                    Status = "succeeded",
                    Created = DateTime.UtcNow
                };

                return new RefundResult
                {
                    Success = true,
                    RefundId = refund.Id,
                    PaymentIntentId = paymentIntentId,
                    Status = "succeeded",
                    Message = "İade işlemi başarıyla tamamlandı"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "İade işlemi sırasında hata oluştu");
                return new RefundResult
                {
                    Success = false,
                    Message = "İade işlemi sırasında bir hata oluştu"
                };
            }
        }
    }

    public class PaymentRequest
    {
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "try";
        public string PaymentMethodId { get; set; }
        public string Description { get; set; }
    }

    public class PaymentResult
    {
        public bool Success { get; set; }
        public string PaymentIntentId { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; }
        public string Status { get; set; }
        public string Message { get; set; }
    }

    public class RefundResult
    {
        public bool Success { get; set; }
        public string RefundId { get; set; }
        public string PaymentIntentId { get; set; }
        public string Status { get; set; }
        public string Message { get; set; }
    }
} 