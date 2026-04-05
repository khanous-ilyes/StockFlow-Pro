using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockFlowPro.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace StockFlowPro.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize(Roles = "SuperAdmin")]
public class AdminController : ControllerBase
{
    private readonly IAppDbContext _context;
    private readonly IConfiguration _configuration;

    public AdminController(IAppDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    [HttpGet("tenants")]
    public async Task<IActionResult> GetAllTenants()
    {
        // Exclude the SuperAdmin tenant from the management list
        var tenants = await _context.Tenants
            .Where(t => t.Slug != "superadmin-system")
            .Select(t => new { 
                t.Id, 
                t.Name, 
                t.Slug, 
                t.CreatedAt, 
                t.IsActive, 
                t.AllowedMacAddress,
                AdminEmail = t.Users.FirstOrDefault(u => u.Role == StockFlowPro.Domain.Enums.UserRole.TenantAdmin)!.Email ?? t.Users.FirstOrDefault()!.Email 
            })
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();
            
        return Ok(tenants);
    }

    [HttpPut("tenants/{id}/toggle-status")]
    public async Task<IActionResult> ToggleTenantStatus(Guid id)
    {
        var tenant = await _context.Tenants.FindAsync(id);
        if (tenant == null)
            return NotFound(new { Message = "Entreprise non trouvée." });

        if (!tenant.IsActive && string.IsNullOrWhiteSpace(tenant.AllowedMacAddress))
            return BadRequest(new { Message = "Impossible d'activer ce compte : Aucune adresse MAC n'est associée. Le client doit se connecter une première fois pour l'enregistrer." });

        tenant.IsActive = !tenant.IsActive; // Toggle

        await _context.SaveChangesAsync();
        
        var status = tenant.IsActive ? "activée" : "désactivée";
        return Ok(new { Message = $"L'entreprise a été {status} avec succès." });
    }

    [HttpGet("offline-licenses")]
    public async Task<IActionResult> GetOfflineLicenses()
    {
        var licenses = await _context.OfflineLicenses
            .OrderByDescending(l => l.CreatedAt)
            .ToListAsync();
        return Ok(licenses);
    }

    [HttpPost("offline-licenses/generate")]
    public async Task<IActionResult> GenerateOfflineLicense([FromBody] GenerateLicenseRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.CustomerName) || string.IsNullOrWhiteSpace(request.MachineId))
            return BadRequest(new { Message = "Nom du client et Machine ID sont requis." });

        string privateKeyBase64 = _configuration["LicenseSettings:PrivateKey"] ?? string.Empty;
        if (string.IsNullOrEmpty(privateKeyBase64))
            return StatusCode(500, new { Message = "Clé privée non configurée sur le serveur." });

        try {
            using var rsa = System.Security.Cryptography.RSA.Create();
            rsa.ImportRSAPrivateKey(Convert.FromBase64String(privateKeyBase64), out _);

            var payload = new StockFlowPro.API.Services.License.LicensePayload
            {
                MachineId = request.MachineId,
                ExpirationDate = request.ExpirationDate
            };

            string jsonPayload = System.Text.Json.JsonSerializer.Serialize(payload);
            byte[] payloadBytes = System.Text.Encoding.UTF8.GetBytes(jsonPayload);
            
            byte[] signatureBytes = rsa.SignData(
                payloadBytes,
                System.Security.Cryptography.HashAlgorithmName.SHA256,
                System.Security.Cryptography.RSASignaturePadding.Pkcs1);

            string payloadBase64 = Convert.ToBase64String(payloadBytes);
            string signatureBase64 = Convert.ToBase64String(signatureBytes);

            string finalLicenseKey = $"{payloadBase64}.{signatureBase64}";

            var offlineLicense = new StockFlowPro.Domain.Entities.OfflineLicense
            {
                CustomerName = request.CustomerName,
                MachineId = request.MachineId,
                LicenseKey = finalLicenseKey,
                ExpiryDate = request.ExpirationDate,
                IsActive = true
            };

            _context.OfflineLicenses.Add(offlineLicense);
            await _context.SaveChangesAsync();

            return Ok(offlineLicense);
        }
        catch (Exception ex) {
            return BadRequest(new { Message = $"Erreur lors de la génération: {ex.Message}" });
        }
    }

    [HttpPut("offline-licenses/{id}/toggle-status")]
    public async Task<IActionResult> ToggleOfflineLicenseStatus(Guid id)
    {
        var license = await _context.OfflineLicenses.FindAsync(id);
        if (license == null) return NotFound(new { Message = "Licence non trouvée." });

        license.IsActive = !license.IsActive;
        await _context.SaveChangesAsync();
        var status = license.IsActive ? "activée" : "désactivée";
        return Ok(new { Message = $"Licence {status} avec succès." });
    }

    [HttpDelete("offline-licenses/{id}")]
    public async Task<IActionResult> DeleteOfflineLicense(Guid id)
    {
        var license = await _context.OfflineLicenses.FindAsync(id);
        if (license == null) return NotFound(new { Message = "Licence non trouvée." });

        _context.OfflineLicenses.Remove(license);
        await _context.SaveChangesAsync();
        return Ok(new { Message = "Licence supprimée." });
    }
}

public class GenerateLicenseRequest
{
    public string CustomerName { get; set; } = string.Empty;
    public string MachineId { get; set; } = string.Empty;
    public DateTime? ExpirationDate { get; set; }
}
