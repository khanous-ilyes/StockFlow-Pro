using Microsoft.EntityFrameworkCore;
using StockFlowPro.Application.DTOs;
using StockFlowPro.Application.Interfaces;
using StockFlowPro.Domain.Entities;

namespace StockFlowPro.Application.Services;

public interface ICategoryService
{
    Task<IEnumerable<CategoryDto>> GetAllAsync();
    Task<CategoryDto?> GetByIdAsync(Guid id);
    Task<CategoryDto> CreateAsync(CreateCategoryDto dto);
    Task UpdateAsync(Guid id, CreateCategoryDto dto);
    Task DeleteAsync(Guid id);
}

public class CategoryService : ICategoryService
{
    private readonly IAppDbContext _context;

    public CategoryService(IAppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<CategoryDto>> GetAllAsync()
    {
        var categories = await _context.Categories.ToListAsync();
        return categories.Select(c => new CategoryDto
        {
            Id = c.Id,
            Name = c.Name,
            Description = c.Description,
            ParentId = c.ParentId,
            ColorHex = c.ColorHex,
            IconName = c.IconName,
            SortOrder = c.SortOrder,
            IsActive = c.IsActive
        });
    }

    public async Task<CategoryDto?> GetByIdAsync(Guid id)
    {
        var category = await _context.Categories.FindAsync(id);
        if (category == null) return null;

        return new CategoryDto
        {
            Id = category.Id,
            Name = category.Name,
            Description = category.Description,
            ParentId = category.ParentId,
            ColorHex = category.ColorHex,
            IconName = category.IconName,
            SortOrder = category.SortOrder,
            IsActive = category.IsActive
        };
    }

    public async Task<CategoryDto> CreateAsync(CreateCategoryDto dto)
    {
        var category = new Category
        {
            Name = dto.Name,
            Description = dto.Description,
            ParentId = dto.ParentId,
            ColorHex = dto.ColorHex,
            IconName = dto.IconName,
            SortOrder = dto.SortOrder,
            IsActive = dto.IsActive
        };

        _context.Categories.Add(category);
        await _context.SaveChangesAsync();

        return new CategoryDto
        {
            Id = category.Id,
            Name = category.Name,
            Description = category.Description,
            ParentId = category.ParentId,
            ColorHex = category.ColorHex,
            IconName = category.IconName,
            SortOrder = category.SortOrder,
            IsActive = category.IsActive
        };
    }

    public async Task UpdateAsync(Guid id, CreateCategoryDto dto)
    {
        var category = await _context.Categories.FindAsync(id);
        if (category == null) throw new KeyNotFoundException("Category not found");

        category.Name = dto.Name;
        category.Description = dto.Description;
        category.ParentId = dto.ParentId;
        category.ColorHex = dto.ColorHex;
        category.IconName = dto.IconName;
        category.SortOrder = dto.SortOrder;
        category.IsActive = dto.IsActive;

        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var category = await _context.Categories.FindAsync(id);
        if (category == null) throw new KeyNotFoundException("Category not found");

        _context.Categories.Remove(category);
        await _context.SaveChangesAsync();
    }
}
