using System;
using StockFlowPro.Domain.Enums;

namespace StockFlowPro.Application.DTOs;

public class ClientDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? Notes { get; set; }
    public string? Nif { get; set; }
    
    public decimal CreditLimit { get; set; }
    public decimal CurrentBalance { get; set; }
    public decimal TotalPurchases { get; set; }
    
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public int OrderCount { get; set; }
}

public class CreateClientDto
{
    public string FullName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? Notes { get; set; }
    public string? Nif { get; set; }
    public decimal CreditLimit { get; set; }
    public bool IsActive { get; set; } = true;
}

public class OrderItemDto
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Discount { get; set; }
    public decimal TotalPrice { get; set; }
}

public class OrderDto
{
    public Guid Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public OrderStatus Status { get; set; }
    public Guid? ClientId { get; set; }
    public string? ClientName { get; set; }
    
    public decimal SubTotal { get; set; }
    public decimal TaxRate { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal OrderExpenses { get; set; }
    public Currency Currency { get; set; }
    
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    
    public PaymentMethod PaymentMethod { get; set; }
    public decimal AmountPaid { get; set; }
    public decimal RemainingCredit { get; set; }
    
    public List<OrderItemDto> Items { get; set; } = new();
}

public class CreateOrderItemDto
{
    public Guid ProductId { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Discount { get; set; }
}

public class CreateOrderDto
{
    public Guid? ClientId { get; set; }
    public decimal TaxRate { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal OrderExpenses { get; set; }
    public Currency Currency { get; set; } = Currency.DZD;
    public string? Notes { get; set; }
    
    // Payment
    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Cash;
    public decimal AmountPaid { get; set; }  // 0 = full credit; TotalAmount = full cash
    
    public List<CreateOrderItemDto> Items { get; set; } = new();
}

public class UpdateOrderDto
{
    public string? Notes { get; set; }
    public decimal OrderExpenses { get; set; }
}
