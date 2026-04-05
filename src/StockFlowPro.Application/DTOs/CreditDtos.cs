using StockFlowPro.Domain.Enums;

namespace StockFlowPro.Application.DTOs;

/// <summary>
/// Represents a credit debt line (an order with remaining credit)
/// </summary>
public class CreditLineDto
{
    public Guid OrderId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public Guid ClientId { get; set; }
    public string ClientName { get; set; } = string.Empty;
    public string? ClientPhone { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal AmountPaid { get; set; }
    public decimal RemainingCredit { get; set; }
    public DateTime OrderDate { get; set; }
    public List<CreditPaymentDto> Payments { get; set; } = new();
}

/// <summary>
/// A recorded payment (installment) against a credit
/// </summary>
public class CreditPaymentDto
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public decimal Amount { get; set; }
    public PaymentMethod Method { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// DTO to record a new payment installment
/// </summary>
public class RecordCreditPaymentDto
{
    public Guid OrderId { get; set; }
    public decimal Amount { get; set; }
    public PaymentMethod Method { get; set; } = PaymentMethod.Cash;
    public string? Notes { get; set; }
}
