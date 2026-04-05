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

    public SettingsController(IAppDbContext context, Microsoft.AspNetCore.Hosting.IWebHostEnvironment env)
    {
        _context = context;
        _env = env;
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

        string uploadsFolder = Path.Combine(_env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), "uploads", "logos");
        if (!Directory.Exists(uploadsFolder))
            Directory.CreateDirectory(uploadsFolder);

        string uniqueFileName = $"{tenant.Id}_{Guid.NewGuid()}{extension}";
        string filePath = Path.Combine(uploadsFolder, uniqueFileName);

        // Delete old logo if it exists
        if (!string.IsNullOrEmpty(tenant.LogoUrl))
        {
            var oldFileName = Path.GetFileName(tenant.LogoUrl.TrimEnd('/'));
            if (!string.IsNullOrEmpty(oldFileName))
            {
                var oldFilePath = Path.Combine(uploadsFolder, oldFileName);
                if (System.IO.File.Exists(oldFilePath))
                {
                    try { System.IO.File.Delete(oldFilePath); } catch { /* Ignore */ }
                }
            }
        }

        using (var fileStream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(fileStream);
        }

        tenant.LogoUrl = $"/uploads/logos/{uniqueFileName}";
        await _context.SaveChangesAsync();

        return Ok(new { LogoUrl = tenant.LogoUrl, Message = "Logo mis à jour avec succès." });
    }
}
