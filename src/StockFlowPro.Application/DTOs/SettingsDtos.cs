using System.ComponentModel.DataAnnotations;
using StockFlowPro.Domain.Enums;

namespace StockFlowPro.Application.DTOs;

public class TenantSettingsDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? LogoUrl { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Address { get; set; }
    public Currency Currency { get; set; }
    public AppLanguage DefaultLanguage { get; set; }
    public string? TaxNumber { get; set; }
    public string? Nif { get; set; }
    public string? Nis { get; set; }
    public string? Rc { get; set; }
    public string? Art { get; set; }
    public decimal DefaultTaxRate { get; set; } = 19.0m; // % TVA
}

public class UpdateTenantSettingsDto
{
    [Required]
    public string Name { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? Address { get; set; }
    [Required]
    public Currency Currency { get; set; }
    [Required]
    public AppLanguage DefaultLanguage { get; set; }
    public string? TaxNumber { get; set; }
    public string? Nif { get; set; }
    public string? Nis { get; set; }
    public string? Rc { get; set; }
    public string? Art { get; set; }
    public decimal DefaultTaxRate { get; set; } = 19.0m;
}
