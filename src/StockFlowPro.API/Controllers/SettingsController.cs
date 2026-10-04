using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockFlowPro.Application.DTOs;
using StockFlowPro.Application.Interfaces;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace StockFlowPro.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class SettingsController : ControllerBase
{
    private readonly IAppDbContext _context;
    private readonly Microsoft.AspNetCore.Hosting.IWebHostEnvironment _env;
    private readonly IFileStorageService _fileStorageService;

    public SettingsController(IAppDbContext context, Microsoft.AspNetCore.Hosting.IWebHostEnvironment env, IFileStorageService fileStorageService)
    {
        _context = context;
        _env = env;
        _fileStorageService = fileStorageService;
    }

    [HttpGet]
    public async Task<IActionResult> GetSettings()
    {
        var tenant = await _context.Tenants.FirstOrDefaultAsync(t => t.Id == _context.CurrentTenantId);
        if (tenant == null)
            return NotFound(new { Message = "Enterprise non trouvée." });

        var dto = new TenantSettingsDto
        {
            Id = tenant.Id,
            Name = tenant.Name,
            Slug = tenant.Slug,
            LogoUrl = tenant.LogoUrl,
            PhoneNumber = tenant.PhoneNumber,
            Address = tenant.Address,
            Currency = tenant.Currency,
            DefaultLanguage = tenant.DefaultLanguage,
            TaxNumber = tenant.TaxNumber,
            Nif = tenant.Nif,
            Nis = tenant.Nis,
            Rc = tenant.Rc,
            Art = tenant.Art,
            DefaultTaxRate = tenant.DefaultTaxRate
        };

        return Ok(dto);
    }

    [HttpPut]
    public async Task<IActionResult> UpdateSettings([FromBody] UpdateTenantSettingsDto request)
    {
        var tenant = await _context.Tenants.FirstOrDefaultAsync(t => t.Id == _context.CurrentTenantId);
        if (tenant == null)
            return NotFound(new { Message = "Enterprise non trouvée." });

        tenant.Name = request.Name;
        tenant.PhoneNumber = request.PhoneNumber;
        tenant.Address = request.Address;
        tenant.Currency = request.Currency;
        tenant.DefaultLanguage = request.DefaultLanguage;
        tenant.TaxNumber = request.TaxNumber;
        tenant.Nif = request.Nif;
        tenant.Nis = request.Nis;
        tenant.Rc = request.Rc;
        tenant.Art = request.Art;
        tenant.DefaultTaxRate = request.DefaultTaxRate;

        await _context.SaveChangesAsync();

        return Ok(new { Message = "Paramètres mis à jour avec succès." });
    }

    [HttpPost("upload-logo")]
    public async Task<IActionResult> UploadLogo(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { Message = "Aucun fichier fourni." });

        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".svg", ".webp" };
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(extension))
            return BadRequest(new { Message = "Format de fichier non valide." });

        var tenant = await _context.Tenants.FirstOrDefaultAsync(t => t.Id == _context.CurrentTenantId);
        if (tenant == null)
            return NotFound(new { Message = "Enterprise non trouvée." });

        // Delete old logo if it exists on Supabase (optional, but handled by the interface)
        if (!string.IsNullOrEmpty(tenant.LogoUrl) && tenant.LogoUrl.Contains("supabase.co"))
        {
            try { await _fileStorageService.DeleteFileAsync(tenant.LogoUrl); } catch { /* Ignore */ }
        }

        using (var stream = file.OpenReadStream())
        {
            var publicUrl = await _fileStorageService.UploadFileAsync(stream, file.FileName, file.ContentType);
            tenant.LogoUrl = publicUrl;
            await _context.SaveChangesAsync();
        }

        return Ok(new { LogoUrl = tenant.LogoUrl, Message = "Logo mis à jour avec succès sur Supabase." });
    }

    [HttpGet("backup-db")]
    public IActionResult BackupDatabase()
    {
        var dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "stockflowpro.db");
        if (!System.IO.File.Exists(dbPath))
            return NotFound(new { Message = "Base de données introuvable." });

        var stream = new FileStream(dbPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        return File(stream, "application/octet-stream", $"stockflowpro_backup_{DateTime.Now:yyyyMMdd_HHmmss}.db");
    }

    /// <summary>Purge all transactional and test data (products, orders, clients, sub-users) for a clean client handover</summary>
    [HttpPost("purge-all-data")]
    public async Task<IActionResult> PurgeAllData()
    {
        var tenantId = _context.CurrentTenantId;
        var tenant = await _context.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId);
        if (tenant == null)
            return NotFound(new { Message = "Enterprise non trouvée." });

        var clientPayments = _context.ClientPayments.Where(cp => cp.TenantId == tenantId);
        _context.ClientPayments.RemoveRange(clientPayments);

        var supplierPayments = _context.SupplierPayments.Where(sp => sp.TenantId == tenantId);
        _context.SupplierPayments.RemoveRange(supplierPayments);

        var supplierPurchaseItems = _context.SupplierPurchaseItems.Where(spi => _context.SupplierPurchases.Any(sp => sp.Id == spi.PurchaseId && sp.TenantId == tenantId));
        _context.SupplierPurchaseItems.RemoveRange(supplierPurchaseItems);

        var supplierPurchases = _context.SupplierPurchases.Where(sp => sp.TenantId == tenantId);
        _context.SupplierPurchases.RemoveRange(supplierPurchases);

        var invoices = _context.Invoices.Where(i => i.TenantId == tenantId);
        _context.Invoices.RemoveRange(invoices);

        var orderItems = _context.OrderItems.Where(oi => _context.Orders.Any(o => o.Id == oi.OrderId && o.TenantId == tenantId));
        _context.OrderItems.RemoveRange(orderItems);

        var orders = _context.Orders.Where(o => o.TenantId == tenantId);
        _context.Orders.RemoveRange(orders);

        var stockMovements = _context.StockMovements.Where(sm => sm.TenantId == tenantId);
        _context.StockMovements.RemoveRange(stockMovements);

        var products = _context.Products.Where(p => p.TenantId == tenantId);
        _context.Products.RemoveRange(products);

        var categories = _context.Categories.Where(c => c.TenantId == tenantId);
        _context.Categories.RemoveRange(categories);

        var clients = _context.Clients.Where(c => c.TenantId == tenantId);
        _context.Clients.RemoveRange(clients);

        var suppliers = _context.Suppliers.Where(s => s.TenantId == tenantId);
        _context.Suppliers.RemoveRange(suppliers);

        var subUsers = _context.Users.IgnoreQueryFilters().Where(u => u.TenantId == tenantId && u.Role != Domain.Enums.UserRole.TenantAdmin && u.Role != Domain.Enums.UserRole.SuperAdmin);
        _context.Users.RemoveRange(subUsers);

        await _context.SaveChangesAsync();

        return Ok(new { Message = "Toutes les données de test ont été effacées avec succès." });
    }
}
