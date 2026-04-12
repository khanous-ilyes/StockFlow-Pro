using Microsoft.EntityFrameworkCore;
using StockFlowPro.Application.DTOs;
using StockFlowPro.Application.Interfaces;
using StockFlowPro.Domain.Entities;

namespace StockFlowPro.Application.Services;

public interface IProductService
{
    Task<IEnumerable<ProductDto>> GetAllAsync();
    Task<ProductDto?> GetByIdAsync(Guid id);
    Task<ProductDto> CreateAsync(CreateProductDto dto);
    Task UpdateAsync(Guid id, CreateProductDto dto);
    Task DeleteAsync(Guid id);
}

public class ProductService : IProductService
{
    private readonly IAppDbContext _context;

    public ProductService(IAppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<ProductDto>> GetAllAsync()
    {
        var products = await _context.Products.ToListAsync();
        return products.Select(p => new ProductDto
        {
            Id = p.Id,
            Name = p.Name,
            SKU = p.SKU,
            Description = p.Description,
            UnitPrice = p.UnitPrice,
            CostPrice = p.CostPrice,
            StockQuantity = p.StockQuantity,
            MinStockAlert = p.MinStockAlert,
            CategoryId = p.CategoryId,
            ImageUrl = p.ImageUrl,
            Unit = p.Unit,
            IsActive = p.IsActive,
            SupplierId = p.SupplierId
        });
    }

    public async Task<ProductDto?> GetByIdAsync(Guid id)
    {
        var product = await _context.Products.FindAsync(id);
        if (product == null) return null;

        return new ProductDto
        {
            Id = product.Id,
            Name = product.Name,
            SKU = product.SKU,
            Description = product.Description,
            UnitPrice = product.UnitPrice,
            CostPrice = product.CostPrice,
            StockQuantity = product.StockQuantity,
            MinStockAlert = product.MinStockAlert,
            CategoryId = product.CategoryId,
            ImageUrl = product.ImageUrl,
            Unit = product.Unit,
            IsActive = product.IsActive,
            SupplierId = product.SupplierId
        };
    }

    public async Task<ProductDto> CreateAsync(CreateProductDto dto)
    {
        var product = new Product
        {
            Name = dto.Name,
            SKU = dto.SKU,
            Description = dto.Description,
            UnitPrice = dto.UnitPrice,
            CostPrice = dto.CostPrice,
            StockQuantity = dto.StockQuantity,
            MinStockAlert = dto.MinStockAlert,
            CategoryId = dto.CategoryId,
            ImageUrl = dto.ImageUrl,
            Unit = dto.Unit,
            IsActive = dto.IsActive,
            SupplierId = dto.SupplierId
        };

        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        if (dto.SupplierId.HasValue && dto.PaymentIsCredit && dto.StockQuantity > 0 && dto.CostPrice > 0)
        {
            var supplier = await _context.Suppliers.FindAsync(dto.SupplierId.Value);
            if (supplier != null)
            {
                var purchaseTotal = dto.StockQuantity * dto.CostPrice;
                supplier.TotalDebt += purchaseTotal;
                await _context.SaveChangesAsync();
            }
        }

        return new ProductDto
        {
            Id = product.Id,
            Name = product.Name,
            SKU = product.SKU,
            Description = product.Description,
            UnitPrice = product.UnitPrice,
            CostPrice = product.CostPrice,
            StockQuantity = product.StockQuantity,
            MinStockAlert = product.MinStockAlert,
            CategoryId = product.CategoryId,
            ImageUrl = product.ImageUrl,
            Unit = product.Unit,
            IsActive = product.IsActive,
            SupplierId = product.SupplierId
        };
    }

    public async Task UpdateAsync(Guid id, CreateProductDto dto)
    {
        var product = await _context.Products.FindAsync(id);
        if (product == null) throw new KeyNotFoundException("Product not found");

        product.Name = dto.Name;
        product.SKU = dto.SKU;
        product.Description = dto.Description;
        product.UnitPrice = dto.UnitPrice;
        product.CostPrice = dto.CostPrice;
        product.StockQuantity = dto.StockQuantity;
        product.MinStockAlert = dto.MinStockAlert;
        product.CategoryId = dto.CategoryId;
        product.ImageUrl = dto.ImageUrl;
        product.Unit = dto.Unit;
        product.IsActive = dto.IsActive;
        product.SupplierId = dto.SupplierId;

        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var product = await _context.Products.FindAsync(id);
        if (product == null) throw new KeyNotFoundException("Product not found");

        _context.Products.Remove(product);
        await _context.SaveChangesAsync();
    }
}
