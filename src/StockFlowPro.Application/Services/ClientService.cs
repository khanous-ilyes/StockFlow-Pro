using Microsoft.EntityFrameworkCore;
using StockFlowPro.Application.DTOs;
using StockFlowPro.Application.Interfaces;
using StockFlowPro.Domain.Entities;

namespace StockFlowPro.Application.Services;

public interface IClientService
{
    Task<IEnumerable<ClientDto>> GetAllAsync();
    Task<ClientDto?> GetByIdAsync(Guid id);
    Task<ClientDto> CreateAsync(CreateClientDto dto);
    Task UpdateAsync(Guid id, CreateClientDto dto);
    Task DeleteAsync(Guid id);
    Task<IEnumerable<OrderDto>> GetClientOrdersAsync(Guid clientId);
}

public class ClientService : IClientService
{
    private readonly IAppDbContext _context;

    public ClientService(IAppDbContext context)
    {
        _context = context;
    }

    private static ClientDto ToDto(Client c, int orderCount = 0) => new()
    {
        Id = c.Id,
        FullName = c.FullName,
        Email = c.Email,
        PhoneNumber = c.PhoneNumber,
        Address = c.Address,
        City = c.City,
        Notes = c.Notes,
        Nif = c.Nif,
        CreditLimit = c.CreditLimit,
        CurrentBalance = c.CurrentBalance,
        TotalPurchases = c.TotalPurchases,
        IsActive = c.IsActive,
        CreatedAt = c.CreatedAt,
        OrderCount = orderCount
    };

    public async Task<IEnumerable<ClientDto>> GetAllAsync()
    {
        var clients = await _context.Clients
            .Include(c => c.Orders)
            .ToListAsync();
        return clients.Select(c => ToDto(c, c.Orders.Count));
    }

    public async Task<ClientDto?> GetByIdAsync(Guid id)
    {
        var c = await _context.Clients
            .Include(c => c.Orders)
            .FirstOrDefaultAsync(c => c.Id == id);
        if (c == null) return null;
        return ToDto(c, c.Orders.Count);
    }

    public async Task<ClientDto> CreateAsync(CreateClientDto dto)
    {
        var client = new Client
        {
            FullName = dto.FullName,
            Email = dto.Email,
            PhoneNumber = dto.PhoneNumber,
            Address = dto.Address,
            City = dto.City,
            Notes = dto.Notes,
            Nif = dto.Nif,
            CreditLimit = dto.CreditLimit,
            IsActive = dto.IsActive
        };

        _context.Clients.Add(client);
        await _context.SaveChangesAsync();

        return ToDto(client);
    }

    public async Task UpdateAsync(Guid id, CreateClientDto dto)
    {
        var client = await _context.Clients.FindAsync(id);
        if (client == null) throw new KeyNotFoundException("Client not found");

        client.FullName = dto.FullName;
        client.Email = dto.Email;
        client.PhoneNumber = dto.PhoneNumber;
        client.Address = dto.Address;
        client.City = dto.City;
        client.Notes = dto.Notes;
        client.Nif = dto.Nif;
        client.CreditLimit = dto.CreditLimit;
        client.IsActive = dto.IsActive;

        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var client = await _context.Clients
            .Include(c => c.Orders)
                .ThenInclude(o => o.Items)
            .Include(c => c.Payments)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (client == null) throw new KeyNotFoundException("Client not found");

        // Cascade-delete orders and their items manually (SQLite doesn't support ON DELETE CASCADE by default with EF)
        foreach (var order in client.Orders)
        {
            _context.OrderItems.RemoveRange(order.Items);
        }
        _context.Orders.RemoveRange(client.Orders);
        _context.ClientPayments.RemoveRange(client.Payments);
        _context.Clients.Remove(client);

        await _context.SaveChangesAsync();
    }

    public async Task<IEnumerable<OrderDto>> GetClientOrdersAsync(Guid clientId)
    {
        var orders = await _context.Orders
            .Where(o => o.ClientId == clientId)
            .Include(o => o.Items)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

        return orders.Select(o => new OrderDto
        {
            Id = o.Id,
            OrderNumber = o.OrderNumber,
            Status = o.Status,
            ClientId = o.ClientId,
            ClientName = null,
            SubTotal = o.SubTotal,
            TaxRate = o.TaxRate,
            TaxAmount = o.TaxAmount,
            DiscountAmount = o.DiscountAmount,
            TotalAmount = o.TotalAmount,
            Currency = o.Currency,
            Notes = o.Notes,
            CreatedAt = o.CreatedAt,
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
        });
    }
}
