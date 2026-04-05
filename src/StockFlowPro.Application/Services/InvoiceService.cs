using Microsoft.EntityFrameworkCore;
using StockFlowPro.Application.DTOs;
using StockFlowPro.Application.Interfaces;
using StockFlowPro.Domain.Entities;
using StockFlowPro.Domain.Enums;

namespace StockFlowPro.Application.Services;

public interface IInvoiceService
{
    Task<IEnumerable<InvoiceDto>> GetAllAsync();
    Task<InvoiceDto?> GetByIdAsync(Guid id);
    Task<InvoiceDto> CreateFromOrderAsync(CreateInvoiceDto dto);
    Task DeleteAsync(Guid id);
}

public class InvoiceService : IInvoiceService
{
    private readonly IAppDbContext _context;

    public InvoiceService(IAppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<InvoiceDto>> GetAllAsync()
    {
        var invoices = await _context.Invoices
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync();

        var result = new List<InvoiceDto>();
        foreach (var inv in invoices)
        {
            var dto = await MapToDtoAsync(inv);
            if (dto != null) result.Add(dto);
        }
        return result;
    }

    public async Task<InvoiceDto?> GetByIdAsync(Guid id)
    {
        var invoice = await _context.Invoices.FindAsync(id);
        if (invoice == null) return null;
        return await MapToDtoAsync(invoice);
    }

    public async Task<InvoiceDto> CreateFromOrderAsync(CreateInvoiceDto dto)
    {
        // Validate order exists and is confirmed
        var order = await _context.Orders
            .Include(o => o.Client)
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == dto.OrderId);

        if (order == null) throw new KeyNotFoundException("Order not found");

        // Check if invoice already exists for this order and type
        var existing = await _context.Invoices
            .FirstOrDefaultAsync(i => i.OrderId == dto.OrderId && i.Type == dto.Type);
        if (existing != null) return (await MapToDtoAsync(existing))!;

        var typePrefix = dto.Type == InvoiceType.Officiel ? "FAC-O" : "FAC-P";
        var invoice = new Invoice
        {
            OrderId = dto.OrderId,
            InvoiceNumber = typePrefix + "-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss"),
            Status = InvoiceStatus.Draft,
            Type = dto.Type,
            Language = dto.Language,
            Notes = dto.Notes,
        };

        _context.Invoices.Add(invoice);
        await _context.SaveChangesAsync();

        return (await MapToDtoAsync(invoice))!;
    }

    public async Task DeleteAsync(Guid id)
    {
        var invoice = await _context.Invoices.FindAsync(id);
        if (invoice == null) throw new KeyNotFoundException("Invoice not found");
        _context.Invoices.Remove(invoice);
        await _context.SaveChangesAsync();
    }

    private async Task<InvoiceDto?> MapToDtoAsync(Invoice invoice)
    {
        var order = await _context.Orders
            .Include(o => o.Client)
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == invoice.OrderId);

        if (order == null) return null;

        // Get tenant info
        var tenant = await _context.Tenants
            .FirstOrDefaultAsync(t => t.Id == _context.CurrentTenantId);

        // Dynamic recalculation for Officiel invoices
        var taxRate = order.TaxRate;
        var taxAmount = order.TaxAmount;
        var totalAmount = order.TotalAmount;
        var remainingCredit = order.RemainingCredit;

        if (invoice.Type == InvoiceType.Officiel && tenant != null)
        {
            taxRate = tenant.DefaultTaxRate;
            taxAmount = order.SubTotal * (taxRate / 100m);
            totalAmount = order.SubTotal + taxAmount - order.DiscountAmount;
            remainingCredit = totalAmount - order.AmountPaid;
        }

        return new InvoiceDto
        {
            Id = invoice.Id,
            InvoiceNumber = invoice.InvoiceNumber,
            Type = invoice.Type,
            Status = invoice.Status,
            Language = invoice.Language,
            CreatedAt = invoice.CreatedAt,
            DueDate = invoice.DueDate,
            PaidAt = invoice.PaidAt,
            Notes = invoice.Notes,

            // Order
            OrderId = order.Id,
            OrderNumber = order.OrderNumber,
            SubTotal = order.SubTotal,
            TaxRate = taxRate,
            TaxAmount = taxAmount,
            DiscountAmount = order.DiscountAmount,
            TotalAmount = totalAmount,
            AmountPaid = order.AmountPaid,
            RemainingCredit = remainingCredit,
            Currency = order.Currency.ToString(),
            PaymentMethod = order.PaymentMethod,

            // Client
            ClientId = order.ClientId,
            ClientName = order.Client?.FullName,
            ClientAddress = order.Client?.Address,
            ClientCity = order.Client?.City,
            ClientPhone = order.Client?.PhoneNumber,
            ClientNif = order.Client?.Nif,

            // Seller
            SellerName = tenant?.Name ?? "Mon Entreprise",
            SellerAddress = tenant?.Address,
            SellerPhone = tenant?.PhoneNumber,
            SellerLogoUrl = tenant?.LogoUrl,
            SellerNif = tenant?.Nif,
            SellerNis = tenant?.Nis,
            SellerRc = tenant?.Rc,
            SellerArt = tenant?.Art,

            Items = (await Task.WhenAll(order.Items.Select(async i =>
            {
                var product = await _context.Products.FindAsync(i.ProductId);
                return new InvoiceItemDto
                {
                    Reference = product?.SKU ?? string.Empty,
                    ProductName = i.ProductName,
                    Unit = "P",
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice,
                    Discount = i.Discount,
                    TotalPrice = i.TotalPrice
                };
            }))).ToList()
        };
    }
}
