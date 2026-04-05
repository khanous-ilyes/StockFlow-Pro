using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StockFlowPro.Application.DTOs;
using StockFlowPro.Application.Services;
using System.Security.Claims;

namespace StockFlowPro.API.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/[controller]")]
public class CreditsController : ControllerBase
{
    private readonly ICreditsService _creditsService;

    public CreditsController(ICreditsService creditsService)
    {
        _creditsService = creditsService;
    }

    /// <summary>Get all open credit lines (orders with remaining credit)</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var credits = await _creditsService.GetCreditLinesAsync();
        return Ok(credits);
    }

    /// <summary>Get credit lines for a specific client</summary>
    [HttpGet("client/{clientId}")]
    public async Task<IActionResult> GetByClient(Guid clientId)
    {
        var credits = await _creditsService.GetCreditLinesByClientAsync(clientId);
        return Ok(credits);
    }

    /// <summary>Record an installment payment against an order credit</summary>
    [HttpPost("pay")]
    public async Task<IActionResult> RecordPayment([FromBody] RecordCreditPaymentDto dto)
    {
        try
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? Guid.Empty.ToString());
            var payment = await _creditsService.RecordPaymentAsync(dto, userId);
            return Ok(payment);
        }
        catch (KeyNotFoundException ex) { return NotFound(ex.Message); }
        catch (InvalidOperationException ex) { return BadRequest(ex.Message); }
    }
}
