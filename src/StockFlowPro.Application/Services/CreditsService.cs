using Microsoft.EntityFrameworkCore;
using StockFlowPro.Application.DTOs;
using StockFlowPro.Application.Interfaces;
using StockFlowPro.Domain.Entities;
using StockFlowPro.Domain.Enums;

namespace StockFlowPro.Application.Services;

public interface ICreditsService
{
    /// <summary>Returns all confirmed orders that still have a remaining credit balance.</summary>
    Task<IEnumerable<CreditLineDto>> GetCreditLinesAsync();

    /// <summary>Returns credit lines for a specific client.</summary>
    Task<IEnumerable<CreditLineDto>> GetCreditLinesByClientAsync(Guid clientId);

    /// <summary>Records an installment payment against an order credit.</summary>
    Task<CreditPaymentDto> RecordPaymentAsync(RecordCreditPaymentDto dto, Guid userId);
}

public class CreditsService : ICreditsService
{
    private readonly IAppDbContext _context;

    public CreditsService(IAppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<CreditLineDto>> GetCreditLinesAsync()
    {
        var orders = await _context.Orders
            .Include(o => o.Client)
            .Include(o => o.Items)
            .Where(o => o.Status == OrderStatus.Confirmed && o.RemainingCredit > 0)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

        return await BuildCreditLines(orders);
    }

    public async Task<IEnumerable<CreditLineDto>> GetCreditLinesByClientAsync(Guid clientId)
    {
        var orders = await _context.Orders
            .Include(o => o.Client)
            .Include(o => o.Items)
            .Where(o => o.ClientId == clientId && o.Status == OrderStatus.Confirmed && o.RemainingCredit > 0)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

        return await BuildCreditLines(orders);
    }

    public async Task<CreditPaymentDto> RecordPaymentAsync(RecordCreditPaymentDto dto, Guid userId)
    {
        var order = await _context.Orders
            .Include(o => o.Client)
            .FirstOrDefaultAsync(o => o.Id == dto.OrderId);

        if (order == null) throw new KeyNotFoundException("Order not found");
        if (order.RemainingCredit <= 0) throw new InvalidOperationException("This order has no remaining credit.");

        // Clamp payment to remaining credit
        var paymentAmount = Math.Min(dto.Amount, order.RemainingCredit);

        // Update order payment tracking
        order.AmountPaid += paymentAmount;
        order.RemainingCredit -= paymentAmount;
        order.UpdatedAt = DateTime.UtcNow;

        // Update client's current balance
        if (order.Client != null)
        {
            order.Client.CurrentBalance = Math.Max(0, order.Client.CurrentBalance - paymentAmount);
        }

        // Record the payment in ClientPayments table for history
        var clientPayment = new ClientPayment
        {
            ClientId = order.ClientId ?? Guid.Empty,
            Amount = paymentAmount,
            Method = dto.Method,
            Notes = dto.Notes,
            Reference = order.OrderNumber,
            CreatedByUserId = userId
        };
        _context.ClientPayments.Add(clientPayment);

        await _context.SaveChangesAsync();

        return new CreditPaymentDto
        {
            Id = clientPayment.Id,
            OrderId = order.Id,
            Amount = paymentAmount,
            Method = dto.Method,
            Notes = dto.Notes,
            CreatedAt = clientPayment.CreatedAt
        };
    }

    private async Task<IEnumerable<CreditLineDto>> BuildCreditLines(List<Order> orders)
    {
        var orderIds = orders.Select(o => o.Id).ToList();

        // Load recent payments for these orders (matched by Reference = OrderNumber)
        var orderNumbers = orders.Select(o => o.OrderNumber).ToList();
        var payments = await _context.ClientPayments
            .Where(p => p.Reference != null && orderNumbers.Contains(p.Reference))
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

        return orders.Select(o =>
        {
            var orderPayments = payments
                .Where(p => p.Reference == o.OrderNumber)
                .Select(p => new CreditPaymentDto
                {
                    Id = p.Id,
                    OrderId = o.Id,
                    Amount = p.Amount,
                    Method = p.Method,
                    Notes = p.Notes,
                    CreatedAt = p.CreatedAt
                }).ToList();

            return new CreditLineDto
            {
                OrderId = o.Id,
                OrderNumber = o.OrderNumber,
                ClientId = o.ClientId ?? Guid.Empty,
                ClientName = o.Client?.FullName ?? "—",
                ClientPhone = o.Client?.PhoneNumber,
                TotalAmount = o.TotalAmount,
                AmountPaid = o.AmountPaid,
                RemainingCredit = o.RemainingCredit,
                OrderDate = o.CreatedAt,
                Payments = orderPayments
            };
        });
    }
}
