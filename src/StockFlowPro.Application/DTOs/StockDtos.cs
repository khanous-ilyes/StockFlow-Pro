namespace StockFlowPro.Application.DTOs;

public class CategoryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? ParentId { get; set; }
    public string? ColorHex { get; set; }
    public string? IconName { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; }
}

public class CreateCategoryDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? ParentId { get; set; }
    public string? ColorHex { get; set; }
    public string? IconName { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

public class ProductDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? SKU { get; set; }
    public string? Description { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal CostPrice { get; set; }
    public decimal StockQuantity { get; set; }
    public decimal MinStockAlert { get; set; }
    public Guid CategoryId { get; set; }
    public string? ImageUrl { get; set; }
    public string Unit { get; set; } = "pcs";
    public bool IsActive { get; set; }
}

public class CreateProductDto
{
    public string Name { get; set; } = string.Empty;
    public string? SKU { get; set; }
    public string? Description { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal CostPrice { get; set; }
    public decimal StockQuantity { get; set; }
    public decimal MinStockAlert { get; set; }
    public Guid CategoryId { get; set; }
    public string? ImageUrl { get; set; }
    public string Unit { get; set; } = "pcs";
    public bool IsActive { get; set; } = true;
}
