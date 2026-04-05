using StockFlowPro.Domain.Enums;
using StockFlowPro.Domain.Entities;

namespace StockFlowPro.Application.DTOs;

public class InvoiceDto
{
    public Guid Id { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public InvoiceType Type { get; set; }
    public InvoiceStatus Status { get; set; }
    public AppLanguage Language { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? DueDate { get; set; }
    public DateTime? PaidAt { get; set; }
    public string? Notes { get; set; }

    // Order info
    public Guid OrderId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public decimal SubTotal { get; set; }
    public decimal TaxRate { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal AmountPaid { get; set; }
    public decimal RemainingCredit { get; set; }
    public string Currency { get; set; } = "DZD";
    public PaymentMethod? PaymentMethod { get; set; }

    // Client info
    public Guid? ClientId { get; set; }
    public string? ClientName { get; set; }
    public string? ClientAddress { get; set; }
    public string? ClientCity { get; set; }
    public string? ClientPhone { get; set; }
    public string? ClientNif { get; set; }

    // Seller (Tenant) info
    public string SellerName { get; set; } = string.Empty;
    public string? SellerAddress { get; set; }
    public string? SellerPhone { get; set; }
    public string? SellerLogoUrl { get; set; }
    public string? SellerNif { get; set; }
    public string? SellerNis { get; set; }
    public string? SellerRc { get; set; }
    public string? SellerArt { get; set; }

    // Items
    public List<InvoiceItemDto> Items { get; set; } = new();
}

public class InvoiceItemDto
{
    public string Reference { get; set; } = string.Empty; // Product SKU
    public string ProductName { get; set; } = string.Empty;
    public string Unit { get; set; } = "P"; // Pièce by default
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }  // H.T.
    public decimal Discount { get; set; }
    public decimal TotalPrice { get; set; } // H.T. total
}

public class CreateInvoiceDto
{
    public Guid OrderId { get; set; }
    public InvoiceType Type { get; set; } = InvoiceType.Provisoire;
    public AppLanguage Language { get; set; } = AppLanguage.FR;
    public string? Notes { get; set; }
}
