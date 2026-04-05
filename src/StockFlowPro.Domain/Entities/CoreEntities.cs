using System;
using StockFlowPro.Domain.Enums;

namespace StockFlowPro.Domain.Entities;

public abstract class BaseEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public abstract class TenantEntity : BaseEntity
{
    public Guid TenantId { get; set; }
    public Tenant? Tenant { get; set; }
}

public class Tenant : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? LogoUrl { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Address { get; set; }
    public Currency Currency { get; set; } = Currency.DZD;
    public AppLanguage DefaultLanguage { get; set; } = AppLanguage.FR;
    public string? TaxNumber { get; set; } // Identifiant fiscal général
    public string? Nif { get; set; }      // Numéro d'Identification Fiscale
    public string? Nis { get; set; }      // Numéro d'Identification Statistique
    public string? Rc { get; set; }       // Registre de Commerce
    public string? Art { get; set; }      // Article d'imposition
    public decimal DefaultTaxRate { get; set; } = 19.0m; // TVA en %
    
    // Security & Validation
    public bool IsActive { get; set; } = false; // Requires validation by SuperAdmin
    public string? AllowedMacAddress { get; set; } // Hardware binding
    public SubscriptionMode Mode { get; set; } = SubscriptionMode.Local; // Sync mode
    
    public ICollection<User> Users { get; set; } = new List<User>();
}

public class User : TenantEntity
{
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.Manager;
    public bool IsActive { get; set; } = true;
}
