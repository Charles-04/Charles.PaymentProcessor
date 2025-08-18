using Charles.PaymentProcessor.Domain.DTOs;
using Charles.PaymentProcessor.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Charles.PaymentProcessor.Controllers;


[ApiController]
[Route("api/webhooks")]
public class WebhookController : ControllerBase
{
    private readonly IPaymentService _service;

    public WebhookController(IPaymentService service)
    {
        _service = service;
    }

    [HttpPost("payment-updates")]
    public async Task<IActionResult> PaymentUpdates([FromBody] WebhookRequest req,
        [FromHeader(Name = "X-Signature")] string signature, [FromHeader(Name = "X-Merchant-Id")] Guid merchantId,
        CancellationToken ct)
    {
        await _service.HandleGatewayWebhookAsync(req.Payload, signature, merchantId, ct);
        return Ok();
    }
}