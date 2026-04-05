using System;
using StockFlowPro.Domain.Enums;

namespace StockFlowPro.Domain.Entities;

public enum InvoiceType { Provisoire, Officiel }

public class Invoice : TenantEntity
{
    public Guid OrderId { get; set; }
    
    public string InvoiceNumber { get; set; } = string.Empty;
    public InvoiceStatus Status { get; set; } = InvoiceStatus.Draft;
    public InvoiceType Type { get; set; } = InvoiceType.Provisoire;
    
    public DateTime? DueDate { get; set; }
    public DateTime? PaidAt { get; set; }
    public PaymentMethod? PaymentMethod { get; set; }
    public AppLanguage Language { get; set; } = AppLanguage.FR;
    
    public string? Notes { get; set; }
    public string? PdfUrl { get; set; }
    public DateTime? SentAt { get; set; }
    
    public ICollection<ClientPayment> Payments { get; set; } = new List<ClientPayment>();
}

public class StockMovement : TenantEntity
{
    public Guid ProductId { get; set; }
    public Product? Product { get; set; }
    
    public StockMovementType Type { get; set; }
    public decimal Quantity { get; set; }
    public decimal QuantityBefore { get; set; }
    public decimal QuantityAfter { get; set; }
    
    public string? Reason { get; set; }
    public string? Reference { get; set; }
    
    public Guid CreatedByUserId { get; set; }
    public User? CreatedByUser { get; set; }
}

public class ClientPayment : TenantEntity
{
    public Guid ClientId { get; set; }
    public Client? Client { get; set; }
    
    public Guid? InvoiceId { get; set; }
    public Invoice? Invoice { get; set; }
    
    public decimal Amount { get; set; }
    public PaymentMethod Method { get; set; }
    
    public string? Reference { get; set; }
    public string? Notes { get; set; }
    
    public Guid CreatedByUserId { get; set; }
    public User? CreatedByUser { get; set; }
}

public class Notification : TenantEntity
{
    public Guid UserId { get; set; }
    public User? User { get; set; }
    
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public bool IsRead { get; set; }
}
