using System;
using System.Collections.Generic;

namespace StockFlowPro.Domain.Entities;

public class Category : TenantEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? ParentId { get; set; }
    public Category? Parent { get; set; }
    public ICollection<Category> SubCategories { get; set; } = new List<Category>();
    
    public string? ColorHex { get; set; }
    public string? IconName { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
    
    public ICollection<Product> Products { get; set; } = new List<Product>();
}

public class Product : TenantEntity
{
    public Guid CategoryId { get; set; }
    public Category? Category { get; set; }
    
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? SKU { get; set; }
    public string? Barcode { get; set; }
    
    public decimal UnitPrice { get; set; }
    public decimal CostPrice { get; set; }
    public decimal StockQuantity { get; set; }
    public decimal MinStockAlert { get; set; }
    public string Unit { get; set; } = "pcs"; // kg, pcs, L
    public string? ImageUrl { get; set; }
    
    public bool IsActive { get; set; } = true;
    public DateTime? UpdatedAt { get; set; }
    
    public ICollection<StockMovement> StockMovements { get; set; } = new List<StockMovement>();
}
