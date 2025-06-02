using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using webprogbackend.Services.Payment;

namespace webprogbackend.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class PaymentController : ControllerBase
    {
        private readonly IStripePaymentService _paymentService;
        private readonly ILogger<PaymentController> _logger;

        public PaymentController(IStripePaymentService paymentService, ILogger<PaymentController> logger)
        {
            _paymentService = paymentService;
            _logger = logger;
        }

        [HttpPost("process")]
        public async Task<ActionResult<PaymentResult>> ProcessPayment([FromBody] PaymentRequest request)
        {
            try
            {
                var result = await _paymentService.ProcessPaymentAsync(request);
                
                if (result.Success)
                {
                    return Ok(result);
                }
                
                return BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ödeme işlemi sırasında hata oluştu");
                return StatusCode(500, new { message = "Ödeme işlemi sırasında bir hata oluştu" });
            }
        }

        [HttpPost("refund")]
        public async Task<ActionResult<RefundResult>> ProcessRefund([FromBody] RefundRequest request)
        {
            try
            {
                var result = await _paymentService.ProcessRefundAsync(request.PaymentIntentId);
                
                if (result.Success)
                {
                    return Ok(result);
                }
                
                return BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "İade işlemi sırasında hata oluştu");
                return StatusCode(500, new { message = "İade işlemi sırasında bir hata oluştu" });
            }
        }
    }

    public class RefundRequest
    {
        public string PaymentIntentId { get; set; }
    }
} 