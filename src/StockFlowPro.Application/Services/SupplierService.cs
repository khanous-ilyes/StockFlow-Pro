using Microsoft.EntityFrameworkCore;
using StockFlowPro.Application.Interfaces;
using StockFlowPro.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace StockFlowPro.Application.Services;

public class SupplierService
{
    private readonly IAppDbContext _context;

    public SupplierService(IAppDbContext context)
    {
        _context = context;
    }

    public async Task<List<Supplier>> GetAllSuppliersAsync()
    {
        return await _context.Suppliers
            .OrderBy(s => s.Name)
            .ToListAsync();
    }

    public async Task<Supplier> CreateSupplierAsync(Supplier supplier)
    {
        _context.Suppliers.Add(supplier);
        await _context.SaveChangesAsync();
        return supplier;
    }

    public async Task<Supplier?> GetSupplierAsync(Guid id)
    {
        return await _context.Suppliers
            .Include(s => s.Payments)
            .FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task<Supplier?> UpdateSupplierAsync(Guid id, Supplier updateData)
    {
        var supplier = await _context.Suppliers.FindAsync(id);
        if (supplier == null) return null;

        supplier.Name = updateData.Name;
        supplier.Phone = updateData.Phone;
        supplier.Address = updateData.Address;
        supplier.Category = updateData.Category;

        await _context.SaveChangesAsync();
        return supplier;
    }

    public async Task<bool> DeleteSupplierAsync(Guid id)
    {
        var supplier = await _context.Suppliers.FindAsync(id);
        if (supplier == null) return false;

        bool hasProducts = await _context.Products.AnyAsync(p => p.SupplierId == id);
        if (hasProducts)
        {
            throw new InvalidOperationException("Cannot delete supplier with active products. Please remove the supplier from products first.");
        }

        _context.Suppliers.Remove(supplier);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<SupplierPayment> AddPaymentAsync(Guid supplierId, decimal amount, string? notes, string? reference, Guid userId)
    {
        var supplier = await _context.Suppliers.FindAsync(supplierId);
        if (supplier == null) throw new InvalidOperationException("Supplier not found");

        var payment = new SupplierPayment
        {
            SupplierId = supplierId,
            Amount = amount,
            Date = DateTime.UtcNow,
            Notes = notes,
            Reference = reference,
            CreatedByUserId = userId
        };

        supplier.AmountPaid += amount;
        supplier.TotalDebt -= amount; // Decrement debt as we paid!

        if (supplier.TotalDebt < 0)
        {
            // If they paid more than what they owe, maybe it becomes negative. But typically handle that gracefully.
            supplier.TotalDebt = 0;
        }

        _context.SupplierPayments.Add(payment);
        await _context.SaveChangesAsync();

        return payment;
    }
}
