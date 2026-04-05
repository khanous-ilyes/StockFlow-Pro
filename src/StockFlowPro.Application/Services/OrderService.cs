using Microsoft.EntityFrameworkCore;
using StockFlowPro.Application.DTOs;
using StockFlowPro.Application.Interfaces;
using StockFlowPro.Domain.Entities;
using StockFlowPro.Domain.Enums;

namespace StockFlowPro.Application.Services;

public interface IOrderService
{
    Task<IEnumerable<OrderDto>> GetAllAsync();
    Task<OrderDto?> GetByIdAsync(Guid id);
    Task<OrderDto> CreateAsync(CreateOrderDto dto, Guid userId);
    Task<OrderDto> UpdateAsync(Guid id, UpdateOrderDto dto);
    Task DeleteAsync(Guid id);
    Task<OrderDto> ConfirmOrderAsync(Guid id);
    Task<OrderDto> CancelOrderAsync(Guid id, decimal finalLosses);
}

public class OrderService : IOrderService
{
    private readonly IAppDbContext _context;

    public OrderService(IAppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<OrderDto>> GetAllAsync()
    {
        var orders = await _context.Orders
            .Include(o => o.Client)
            .Include(o => o.Items)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

        return orders.Select(MapToDto);
    }

    public async Task<OrderDto?> GetByIdAsync(Guid id)
    {
        var order = await _context.Orders
            .Include(o => o.Client)
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id);
            
        return order == null ? null : MapToDto(order);
    }

    public async Task<OrderDto> CreateAsync(CreateOrderDto dto, Guid userId)
    {
        var order = new Order
        {
            OrderNumber = "ORD-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss"),
            Status = OrderStatus.Draft,
            ClientId = dto.ClientId,
            TaxRate = dto.TaxRate,
            DiscountAmount = dto.DiscountAmount,
            Currency = dto.Currency,
            Notes = dto.Notes,
            PaymentMethod = dto.PaymentMethod,
            CreatedByUserId = userId
        };

        decimal subTotal = 0;

        foreach (var itemDto in dto.Items)
        {
            var product = await _context.Products.FindAsync(itemDto.ProductId);
            if (product == null) continue;

            var unitPrice = itemDto.UnitPrice > 0 ? itemDto.UnitPrice : product.UnitPrice;
            var totalPrice = (itemDto.Quantity * unitPrice) - itemDto.Discount;

            var orderItem = new OrderItem
            {
                ProductId = product.Id,
                ProductName = product.Name,
                Quantity = itemDto.Quantity,
                UnitPrice = unitPrice,
                Discount = itemDto.Discount,
                TotalPrice = totalPrice
            };

            subTotal += totalPrice;
            order.Items.Add(orderItem);
        }

        order.SubTotal = subTotal;
        order.TaxAmount = subTotal * (order.TaxRate / 100);
        order.TotalAmount = subTotal + order.TaxAmount - order.DiscountAmount;
        order.OrderExpenses = dto.OrderExpenses;

        var amountPaid = Math.Min(dto.AmountPaid, order.TotalAmount);
        order.AmountPaid = amountPaid;
        order.RemainingCredit = order.TotalAmount - amountPaid;

        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        return MapToDto(order);
    }

    public async Task<OrderDto> UpdateAsync(Guid id, UpdateOrderDto dto)
    {
        var order = await _context.Orders.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == id);
        if (order == null) throw new KeyNotFoundException("Order not found");
        if (order.Status != OrderStatus.Draft) throw new InvalidOperationException("Only draft orders can be updated.");

        order.Notes = dto.Notes;
        order.OrderExpenses = dto.OrderExpenses;
        order.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return MapToDto(order);
    }

    public async Task<OrderDto> ConfirmOrderAsync(Guid id)
    {
        var order = await _context.Orders
            .Include(o => o.Items)
            .Include(o => o.Client)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null) throw new KeyNotFoundException("Order not found");
        if (order.Status != OrderStatus.Draft) throw new InvalidOperationException("Only draft orders can be confirmed.");

        foreach (var item in order.Items)
        {
            var product = await _context.Products.FindAsync(item.ProductId);
            if (product != null)
            {
                product.StockQuantity -= item.Quantity;
                product.UpdatedAt = DateTime.UtcNow;
            }
        }

        if (order.Client != null)
        {
            order.Client.TotalPurchases += order.TotalAmount;
            order.Client.CurrentBalance += order.RemainingCredit;
        }

        order.Status = OrderStatus.Confirmed;
        order.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return MapToDto(order);
    }

    public async Task<OrderDto> CancelOrderAsync(Guid id, decimal finalLosses)
    {
        var order = await _context.Orders
            .Include(o => o.Items)
            .Include(o => o.Client)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null) throw new KeyNotFoundException("Order not found");
        if (order.Status == OrderStatus.Cancelled) throw new InvalidOperationException("Order is already cancelled.");

        if (order.Status == OrderStatus.Confirmed)
        {
            foreach (var item in order.Items)
            {
                var product = await _context.Products.FindAsync(item.ProductId);
                if (product != null)
                {
                    product.StockQuantity += item.Quantity;
                    product.UpdatedAt = DateTime.UtcNow;
                }
            }

            if (order.Client != null)
            {
                order.Client.TotalPurchases -= order.TotalAmount;
                if (order.Client.TotalPurchases < 0) order.Client.TotalPurchases = 0;
                
                order.Client.CurrentBalance -= order.RemainingCredit;
                if (order.Client.CurrentBalance < 0) order.Client.CurrentBalance = 0;
            }
        }

        order.Status = OrderStatus.Cancelled;
        order.OrderExpenses = finalLosses;
        order.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return MapToDto(order);
    }

    public async Task DeleteAsync(Guid id)
    {
        var order = await _context.Orders.FindAsync(id);
        if (order == null) throw new KeyNotFoundException("Order not found");
        if (order.Status != OrderStatus.Draft && order.Status != OrderStatus.Cancelled)
            throw new InvalidOperationException("Cannot delete validated or completed orders.");

        _context.Orders.Remove(order);
        await _context.SaveChangesAsync();
    }

    private static OrderDto MapToDto(Order o)
    {
        return new OrderDto
        {
            Id = o.Id,
            OrderNumber = o.OrderNumber,
            Status = o.Status,
            ClientId = o.ClientId,
            ClientName = o.Client?.FullName,
            SubTotal = o.SubTotal,
            TaxRate = o.TaxRate,
            TaxAmount = o.TaxAmount,
            DiscountAmount = o.DiscountAmount,
            TotalAmount = o.TotalAmount,
            Currency = o.Currency,
            Notes = o.Notes,
            CreatedAt = o.CreatedAt,
            PaymentMethod = o.PaymentMethod,
            AmountPaid = o.AmountPaid,
            RemainingCredit = o.RemainingCredit,
            OrderExpenses = o.OrderExpenses,
            Items = o.Items.Select(i => new OrderItemDto
            {
                Id = i.Id,
                ProductId = i.ProductId,
                ProductName = i.ProductName,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice,
                Discount = i.Discount,
                TotalPrice = i.TotalPrice
            }).ToList()
        };
    }
}
