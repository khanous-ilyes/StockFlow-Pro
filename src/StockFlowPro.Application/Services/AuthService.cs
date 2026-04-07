using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using StockFlowPro.Domain.Entities;
using StockFlowPro.Domain.Enums;
using StockFlowPro.Application.Interfaces;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BCrypt.Net;

namespace StockFlowPro.Application.Services;

public class AuthService
{
    private readonly IAppDbContext _context;
    private readonly string _jwtSecret;
    private readonly string _jwtIssuer;
    private readonly string _jwtAudience;
    private readonly int _jwtExpiryMinutes;
    private readonly bool _isOnlineMode;

    public AuthService(IAppDbContext context, string jwtSecret, string jwtIssuer, string jwtAudience, int jwtExpiryMinutes, bool isOnlineMode = false)
    {
        _context = context;
        _jwtSecret = jwtSecret;
        _jwtIssuer = jwtIssuer;
        _jwtAudience = jwtAudience;
        _jwtExpiryMinutes = jwtExpiryMinutes;
        _isOnlineMode = isOnlineMode;
    }

    public async Task<(string Token, User User, Tenant Tenant)> RegisterAsync(string tenantName, string email, string password, string fullName, string? macAddress = null)
    {
        // 1. Create Tenant (Inactive by default)
        var tenant = new Tenant
        {
            Name = tenantName,
            Slug = tenantName.ToLower().Replace(" ", "-") + "-" + Guid.NewGuid().ToString("N")[..6],
            IsActive = false, // Requires SuperAdmin validation
            AllowedMacAddress = macAddress
        };
        _context.Tenants.Add(tenant);
        await _context.SaveChangesAsync(); // To get TenantId

        // 2. Create User
        var user = new User
        {
            TenantId = tenant.Id,
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password, 12),
            FullName = fullName,
            Role = UserRole.TenantAdmin,
            IsActive = true // The user is active, but the tenant is what restricts access
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // 3. Generate Token
        var token = GenerateJwtToken(user);
        return (token, user, tenant);
    }

    public async Task<(string Token, User User)?> LoginAsync(string email, string password, string? deviceMacAddress = null)
    {
        // IgnoreQueryFilters: needed because the tenant context is not yet set at login time.
        // Also Include Tenant to check activation status and MAC binding.
        var user = await _context.Users
            .Include(u => u.Tenant)
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Email == email);
            
        if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
        {
            return null;
        }

        if (!user.IsActive) 
            throw new UnauthorizedAccessException("User account is deactivated.");
            
        // SuperAdmin bypasses Tenant checks
        if (user.Role != UserRole.SuperAdmin)
        {
            if (user.Tenant == null || !user.Tenant.IsActive)
                throw new UnauthorizedAccessException("TenantAccountPending"); // Specific error code for frontend
                
            // Hardware Binding check
            if (!_isOnlineMode)
            {
                if (!string.IsNullOrEmpty(user.Tenant.AllowedMacAddress))
                {
                    if (string.IsNullOrEmpty(deviceMacAddress) || user.Tenant.AllowedMacAddress != deviceMacAddress)
                        throw new UnauthorizedAccessException("DeviceNotAuthorized");
                }
                // If AllowedMacAddress is null but Tenant IS active, we should capture the first MAC as the allowed one.
                else if (!string.IsNullOrEmpty(deviceMacAddress) && user.Tenant.AllowedMacAddress == null)
                {
                    user.Tenant.AllowedMacAddress = deviceMacAddress;
                    await _context.SaveChangesAsync();
                }
            }
        }

        var token = GenerateJwtToken(user);
        return (token, user);
    }

    private string GenerateJwtToken(User user)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_jwtSecret);
        
        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim("tenant_id", user.TenantId.ToString()),
            new Claim(ClaimTypes.Role, user.Role.ToString())
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(_jwtExpiryMinutes),
            Issuer = _jwtIssuer,
            Audience = _jwtAudience,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
}
