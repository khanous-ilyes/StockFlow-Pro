using System;
using System.Collections.Generic;
using StockFlowPro.Domain.Enums;

namespace StockFlowPro.Domain.Entities;

public class Client : TenantEntity
{
    public string FullName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? Notes { get; set; }
    public string? Nif { get; set; }   // Numéro d'Identification Fiscale
    
    public decimal CreditLimit { get; set; }
    public decimal CurrentBalance { get; set; }
    public decimal TotalPurchases { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    public ICollection<Order> Orders { get; set; } = new List<Order>();
    public ICollection<ClientPayment> Payments { get; set; } = new List<ClientPayment>();
}

public class Order : TenantEntity
{
    public Guid? ClientId { get; set; }
    public Client? Client { get; set; }
    
    public string OrderNumber { get; set; } = string.Empty;
    public OrderStatus Status { get; set; } = OrderStatus.Draft;
    
    public decimal SubTotal { get; set; }
    public decimal TaxRate { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public Currency Currency { get; set; }
    
    public decimal OrderExpenses { get; set; }
    
    public string? Notes { get; set; }
    
    // Payment tracking
    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Cash;
    public decimal AmountPaid { get; set; }     // Cash paid at order time
    public decimal RemainingCredit { get; set; } // TotalAmount - AmountPaid (credit left)
    
    public Guid CreatedByUserId { get; set; }
    public User? CreatedByUser { get; set; }
    public DateTime? UpdatedAt { get; set; }
    
    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
    public ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
}

public class OrderItem : BaseEntity
{
    public Guid OrderId { get; set; }
    public Order? Order { get; set; }
    
    public Guid ProductId { get; set; }
    public Product? Product { get; set; }
    
    public string ProductName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Discount { get; set; }
    public decimal TotalPrice { get; set; }
}
