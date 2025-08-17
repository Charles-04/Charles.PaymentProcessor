using Charles.PaymentProcessor.Domain.DTOs;
using Charles.PaymentProcessor.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PaymentSystem.Infrastructure;

namespace Charles.PaymentProcessor.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentService _svc;

    public PaymentsController(IPaymentService svc) { _svc = svc; }

    [HttpPost("initialize")]
    public async Task<ActionResult<InitPaymentResponse>> Initialize([FromBody] InitPaymentRequest req, CancellationToken ct)
    {
        if (req.Amount <= 0) return ValidationProblem("Amount must be > 0");
        if (string.IsNullOrWhiteSpace(req.Currency)) return ValidationProblem("Currency is required");

        if (!Guid.TryParse(Request.Headers["X-Merchant-Id"], out var merchantId))
            return Unauthorized("Missing X-Merchant-Id");

        var reference = await _svc.InitializeAsync(
            merchantId,
            req.Amount,
            req.Currency.ToUpperInvariant(),
            req.CustomerEmail,
            System.Text.Json.JsonSerializer.Serialize(req.Metadata ?? new Dictionary<string, object>()),
            ct
        );

        return Ok(new InitPaymentResponse(reference, "Pending"));
    }

    [HttpGet("{reference}")]
    public async Task<IActionResult> Get(string reference, [FromServices] PaymentDbContext db, CancellationToken ct)
    {
        if (!Guid.TryParse(Request.Headers["X-Merchant-Id"], out var merchantId))
            return Unauthorized("Missing X-Merchant-Id");

        var payment = await db.Payments.AsNoTracking().SingleOrDefaultAsync(p => p.Reference == reference && p.MerchantId == merchantId, ct);
        if (payment is null) return NotFound();
        return Ok(payment);
    }
}