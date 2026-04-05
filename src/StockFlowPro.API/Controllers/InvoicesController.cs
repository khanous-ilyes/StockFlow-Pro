using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StockFlowPro.Application.DTOs;
using StockFlowPro.Application.Services;

namespace StockFlowPro.API.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/[controller]")]
public class InvoicesController : ControllerBase
{
    private readonly IInvoiceService _invoiceService;

    public InvoicesController(IInvoiceService invoiceService)
    {
        _invoiceService = invoiceService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var invoices = await _invoiceService.GetAllAsync();
        return Ok(invoices);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var invoice = await _invoiceService.GetByIdAsync(id);
        if (invoice == null) return NotFound();
        return Ok(invoice);
    }

    /// <summary>
    /// Generate a Provisoire or Officiel invoice from a confirmed order.
    /// If an invoice of the same type already exists, returns the existing one.
    /// </summary>
    [HttpPost("generate")]
    public async Task<IActionResult> Generate([FromBody] CreateInvoiceDto dto)
    {
        try
        {
            var invoice = await _invoiceService.CreateFromOrderAsync(dto);
            return Ok(invoice);
        }
        catch (KeyNotFoundException ex) { return NotFound(ex.Message); }
        catch (InvalidOperationException ex) { return BadRequest(ex.Message); }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            await _invoiceService.DeleteAsync(id);
            return NoContent();
        }
        catch (KeyNotFoundException) { return NotFound(); }
    }
}
