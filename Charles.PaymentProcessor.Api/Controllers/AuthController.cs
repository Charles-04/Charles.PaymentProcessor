using Charles.PayementProcessor.Application.Utilities;
using Charles.PaymentProcessor.Domain.DTOs;
using Charles.PaymentProcessor.Domain.Entities;
using Charles.PaymentProcessor.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Charles.PaymentProcessor.Api.Controllers;

[ApiController]
[Route("auth")]
public class AuthController(IRepository<Merchant> db, IJwtTokenGenerator jwt, IConfiguration config)
    : ControllerBase
{
    [HttpPost("apikey")]
    public async Task<IActionResult> AuthenticateWithApiKey([FromBody] ApiKeyRequest request)
    {
        var salt = config["ApiKeys:Salt"];
        var apiKeyHash = ApiKeyHelper.HashApiKey(request.ApiKey, salt);
        var merchant = await db
            .GetSingleByAsync(m => m.ApiKeyHash == apiKeyHash);

        if (merchant == null)
            return Unauthorized(new { error = "Invalid API Key" });

        var token = jwt.GenerateToken(merchant);

        return Ok(new
        {
            accessToken = token,
            expiresIn = 3600
        });
    }
}