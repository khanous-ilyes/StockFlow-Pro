using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockFlowPro.Application.Interfaces;
using System.Threading.Tasks;
using System.Linq;

namespace StockFlowPro.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class DataTransferController : ControllerBase
{
    private readonly IAppDbContext _context;

    public DataTransferController(IAppDbContext context)
    {
        _context = context;
    }

    [HttpGet("export-all")]
    public async Task<IActionResult> ExportAllData()
    {
        var tenantId = _context.CurrentTenantId;
        
        var tenant = await _context.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId);
        if (tenant == null) return NotFound(new { Message = "Tenant not found." });

        var users = await _context.Users.Where(u => u.TenantId == tenantId).ToListAsync();
        var clients = await _context.Clients.ToListAsync();
        var suppliers = await _context.Suppliers.ToListAsync();
        var categories = await _context.Categories.ToListAsync();
        var products = await _context.Products.ToListAsync();
        var invoices = await _context.Invoices.ToListAsync();
        var orders = await _context.Orders.Include(o => o.Items).ToListAsync();
        var clientPayments = await _context.ClientPayments.ToListAsync();
        var supplierPayments = await _context.SupplierPayments.ToListAsync();
        var stockMovements = await _context.StockMovements.ToListAsync();
        var notifications = await _context.Notifications.ToListAsync();
        
        var exportData = new
        {
            Tenant = tenant,
            Users = users,
            Clients = clients,
            Suppliers = suppliers,
            Categories = categories,
            Products = products,
            Invoices = invoices,
            Orders = orders,
            ClientPayments = clientPayments,
            SupplierPayments = supplierPayments,
            StockMovements = stockMovements,
            Notifications = notifications
        };

        return Ok(exportData);
    }
}
