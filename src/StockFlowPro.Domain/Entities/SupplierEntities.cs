using System;
using System.Collections.Generic;

namespace StockFlowPro.Domain.Entities;

public class Supplier : TenantEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? Category { get; set; }
    
    public decimal TotalDebt { get; set; }
    public decimal AmountPaid { get; set; }
    
    public ICollection<Product> Products { get; set; } = new List<Product>();
    public ICollection<SupplierPayment> Payments { get; set; } = new List<SupplierPayment>();
}

public class SupplierPayment : TenantEntity
{
    public Guid SupplierId { get; set; }
    public Supplier? Supplier { get; set; }
    
    public decimal Amount { get; set; }
    public DateTime Date { get; set; }
    public string? Notes { get; set; }
    public string? Reference { get; set; }
    
    public Guid CreatedByUserId { get; set; }
    public User? CreatedByUser { get; set; }
}
