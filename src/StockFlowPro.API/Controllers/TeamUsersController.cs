using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockFlowPro.Application.Interfaces;
using StockFlowPro.Domain.Entities;
using StockFlowPro.Domain.Enums;
using System.Security.Claims;

namespace StockFlowPro.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class TeamUsersController : ControllerBase
{
    private readonly IAppDbContext _context;

    public TeamUsersController(IAppDbContext context)
    {
        _context = context;
    }

    private Guid GetCurrentUserId()
    {
        var claim = User.FindFirst("sub") ?? User.FindFirst(ClaimTypes.NameIdentifier);
        return Guid.TryParse(claim?.Value, out var id) ? id : Guid.Empty;
    }

    private Guid GetCurrentTenantId()
    {
        var claim = User.FindFirst("tenant_id");
        return Guid.TryParse(claim?.Value, out var id) ? id : Guid.Empty;
    }

    private string? GetCurrentRole() => User.FindFirstValue(ClaimTypes.Role);

    /// <summary>List all users in the current tenant (excluding the calling TenantAdmin)</summary>
    [HttpGet]
    public async Task<IActionResult> GetTeamUsers()
    {
        var role = GetCurrentRole();
        if (role != UserRole.TenantAdmin.ToString())
            return Forbid();

        var tenantId = GetCurrentTenantId();
        var currentUserId = GetCurrentUserId();

        var users = await _context.Users
            .IgnoreQueryFilters()
            .Where(u => u.TenantId == tenantId && u.Id != currentUserId && u.Role != UserRole.SuperAdmin)
            .Select(u => new
            {
                u.Id,
                u.FullName,
                u.Email,
                Role = u.Role.ToString(),
                u.IsActive
            })
            .OrderBy(u => u.FullName)
            .ToListAsync();

        return Ok(users);
    }

    public record CreateTeamUserRequest(string FullName, string Email, string Password, string Role);

    /// <summary>Create a new sub-user (Cashier or Manager) in the current tenant</summary>
    [HttpPost]
    public async Task<IActionResult> CreateTeamUser([FromBody] CreateTeamUserRequest req)
    {
        var role = GetCurrentRole();
        if (role != UserRole.TenantAdmin.ToString())
            return Forbid();

        var tenantId = GetCurrentTenantId();

        // Validate role
        if (!Enum.TryParse<UserRole>(req.Role, out var parsedRole) ||
            parsedRole == UserRole.SuperAdmin || parsedRole == UserRole.TenantAdmin)
        {
            return BadRequest(new { message = "دور غير صالح. استخدم 'أمين الصندوق' أو 'مدير'." });
        }

        // Check email uniqueness
        bool emailExists = await _context.Users
            .IgnoreQueryFilters()
            .AnyAsync(u => u.Email == req.Email);
        if (emailExists)
            return BadRequest(new { message = "هذا البريد الإلكتروني مستخدم بالفعل." });

        if (string.IsNullOrWhiteSpace(req.Password) || req.Password.Length < 4)
            return BadRequest(new { message = "يجب أن تحتوي كلمة المرور على 4 أحرف على الأقل." });

        var newUser = new User
        {
            TenantId = tenantId,
            FullName = req.FullName,
            Email = req.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.Password, 12),
            Role = parsedRole,
            IsActive = true
        };

        _context.Users.Add(newUser);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            newUser.Id,
            newUser.FullName,
            newUser.Email,
            Role = newUser.Role.ToString(),
            newUser.IsActive
        });
    }

    public record UpdateTeamUserRequest(string FullName, string Email, string Role, bool IsActive, string? NewPassword);

    /// <summary>Update a sub-user's details</summary>
    [HttpPut("{userId}")]
    public async Task<IActionResult> UpdateTeamUser(Guid userId, [FromBody] UpdateTeamUserRequest req)
    {
        var role = GetCurrentRole();
        if (role != UserRole.TenantAdmin.ToString())
            return Forbid();

        var tenantId = GetCurrentTenantId();
        var currentUserId = GetCurrentUserId();

        var user = await _context.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Id == userId && u.TenantId == tenantId && u.Id != currentUserId);

        if (user == null) return NotFound(new { message = "المستخدم غير موجود." });
        if (user.Role == UserRole.TenantAdmin || user.Role == UserRole.SuperAdmin)
            return Forbid();

        // Validate role
        if (!Enum.TryParse<UserRole>(req.Role, out var parsedRole) ||
            parsedRole == UserRole.SuperAdmin || parsedRole == UserRole.TenantAdmin)
        {
            return BadRequest(new { message = "دور غير صالح." });
        }

        // Check email uniqueness
        bool emailTaken = await _context.Users
            .IgnoreQueryFilters()
            .AnyAsync(u => u.Email == req.Email && u.Id != userId);
        if (emailTaken)
            return BadRequest(new { message = "هذا البريد الإلكتروني مستخدم من طرف حساب آخر." });

        user.FullName = req.FullName;
        user.Email = req.Email;
        user.Role = parsedRole;
        user.IsActive = req.IsActive;

        if (!string.IsNullOrWhiteSpace(req.NewPassword))
        {
            if (req.NewPassword.Length < 4)
                return BadRequest(new { message = "يجب أن تحتوي كلمة المرور على 4 أحرف على الأقل." });
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.NewPassword, 12);
        }

        await _context.SaveChangesAsync();

        return Ok(new { message = "تم تحديث المستخدم بنجاح." });
    }

    /// <summary>Delete a sub-user from the current tenant</summary>
    [HttpDelete("{userId}")]
    public async Task<IActionResult> DeleteTeamUser(Guid userId)
    {
        var role = GetCurrentRole();
        if (role != UserRole.TenantAdmin.ToString())
            return Forbid();

        var tenantId = GetCurrentTenantId();
        var currentUserId = GetCurrentUserId();

        var user = await _context.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Id == userId && u.TenantId == tenantId && u.Id != currentUserId);

        if (user == null) return NotFound(new { message = "المستخدم غير موجود." });
        if (user.Role == UserRole.TenantAdmin || user.Role == UserRole.SuperAdmin)
            return Forbid();

        _context.Users.Remove(user);
        await _context.SaveChangesAsync();

        return Ok(new { message = "تم حذف المستخدم بنجاح." });
    }

    public class TraceabilityLogDto
    {
        public Guid Id { get; set; }
        public string Type { get; set; } = string.Empty; // "ClientPayment" or "SupplierPayment"
        public string PartnerName { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Method { get; set; } = string.Empty;
        public string? Reference { get; set; }
        public string? Notes { get; set; }
        public DateTime Date { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string UserRole { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;
    }

    /// <summary>Get full payment traceability audit log for TenantAdmin</summary>
    [HttpGet("traceability")]
    public async Task<IActionResult> GetTraceabilityLogs()
    {
        var role = GetCurrentRole();
        if (role != UserRole.TenantAdmin.ToString())
            return Forbid();

        var tenantId = GetCurrentTenantId();

        // 1. Load users for this tenant into a lookup dictionary
        var tenantUsers = await _context.Users
            .IgnoreQueryFilters()
            .Where(u => u.TenantId == tenantId)
            .ToDictionaryAsync(u => u.Id, u => new { u.FullName, u.Email, Role = u.Role.ToString() });

        // 2. Load Client payments for this tenant
        var clientPayments = await _context.ClientPayments
            .Include(cp => cp.Client)
            .Where(cp => cp.TenantId == tenantId)
            .OrderByDescending(cp => cp.CreatedAt)
            .Take(500)
            .ToListAsync();

        // 3. Load Supplier payments for this tenant
        var supplierPayments = await _context.SupplierPayments
            .Include(sp => sp.Supplier)
            .Where(sp => sp.TenantId == tenantId)
            .OrderByDescending(sp => sp.Date)
            .Take(500)
            .ToListAsync();

        var logs = new List<TraceabilityLogDto>();

        foreach (var cp in clientPayments)
        {
            tenantUsers.TryGetValue(cp.CreatedByUserId, out var userInfo);
            logs.Add(new TraceabilityLogDto
            {
                Id = cp.Id,
                Type = "ClientPayment",
                PartnerName = cp.Client?.FullName ?? "Client Inconnu",
                Amount = cp.Amount,
                Method = cp.Method.ToString(),
                Reference = cp.Reference,
                Notes = cp.Notes,
                Date = cp.CreatedAt,
                UserName = userInfo?.FullName ?? "Utilisateur inconnu",
                UserRole = userInfo?.Role ?? "—",
                UserEmail = userInfo?.Email ?? "—"
            });
        }

        foreach (var sp in supplierPayments)
        {
            tenantUsers.TryGetValue(sp.CreatedByUserId, out var userInfo);
            logs.Add(new TraceabilityLogDto
            {
                Id = sp.Id,
                Type = "SupplierPayment",
                PartnerName = sp.Supplier?.Name ?? "Fournisseur Inconnu",
                Amount = sp.Amount,
                Method = "Espèces/Virement",
                Reference = sp.Reference,
                Notes = sp.Notes,
                Date = sp.Date,
                UserName = userInfo?.FullName ?? "Utilisateur inconnu",
                UserRole = userInfo?.Role ?? "—",
                UserEmail = userInfo?.Email ?? "—"
            });
        }

        var sortedLogs = logs.OrderByDescending(l => l.Date).ToList();

        return Ok(sortedLogs);
    }
}
