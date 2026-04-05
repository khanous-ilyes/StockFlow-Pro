using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockFlowPro.Application.Interfaces;

namespace StockFlowPro.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class AnalyticsController : ControllerBase
{
    private readonly IAppDbContext _context;

    public AnalyticsController(IAppDbContext context)
    {
        _context = context;
    }

    [HttpGet("kpis")]
    public async Task<IActionResult> GetKpis([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
    {
        var ordersQuery = _context.Orders.Include(o => o.Items).AsQueryable();

        if (startDate.HasValue)
        {
            ordersQuery = ordersQuery.Where(o => o.CreatedAt >= startDate.Value);
        }
        if (endDate.HasValue)
        {
            ordersQuery = ordersQuery.Where(o => o.CreatedAt <= endDate.Value);
        }

        var orders = await ordersQuery.ToListAsync();

        var products = await _context.Products.ToListAsync();

        var confirmedOrders = orders.Where(o => (int)o.Status == 1).ToList();

        // CA Brut = TotalAmount of confirmed orders
        decimal grossRevenue = confirmedOrders.Sum(o => o.TotalAmount);
        
        // Losses = sum of OrderExpenses (can exist on confirmed or cancelled orders)
        decimal totalLosses = orders.Sum(o => o.OrderExpenses);

        // Net Revenue = Gross - Losses
        decimal netRevenue = grossRevenue - totalLosses;

        // Stock Value = sum of (Quantity * UnitPrice) roughly, or just total items
        decimal stockValue = products.Sum(p => p.StockQuantity * p.UnitPrice);

        // Low Stock
        var lowStock = products
            .Where(p => p.StockQuantity <= p.MinStockAlert)
            .OrderBy(p => p.StockQuantity)
            .Take(5)
            .Select(p => new {
                id = p.Id,
                name = p.Name,
                sku = p.SKU,
                stockQuantity = p.StockQuantity,
                minStockAlert = p.MinStockAlert
            })
            .ToList();

        // Sales Data (Last 6 months)
        var salesData = Enumerable.Range(0, 6)
            .Select(i => DateTime.UtcNow.AddMonths(-i))
            .Select(d => new {
                month = d.ToString("MMM", new System.Globalization.CultureInfo("fr-FR")),
                amount = confirmedOrders.Where(o => o.CreatedAt.Year == d.Year && o.CreatedAt.Month == d.Month).Sum(o => o.TotalAmount)
            })
            .Reverse()
            .ToList();

        // Best Client
        var bestClientGroup = confirmedOrders
            .Where(o => o.ClientId != null)
            .GroupBy(o => o.ClientId)
            .OrderByDescending(g => g.Sum(o => o.TotalAmount))
            .FirstOrDefault();

        object bestClient = null;
        if (bestClientGroup != null)
        {
            var client = await _context.Clients.FindAsync(bestClientGroup.Key);
            bestClient = new {
                id = client?.Id,
                name = client?.FullName ?? "Inconnu",
                totalAmount = bestClientGroup.Sum(o => o.TotalAmount)
            };
        }

        // Best Product (By Quantity Sold)
        var bestProductGroup = confirmedOrders
            .SelectMany(o => o.Items)
            .GroupBy(i => i.ProductId)
            .OrderByDescending(g => g.Sum(i => i.Quantity))
            .FirstOrDefault();

        object bestProduct = null;
        if (bestProductGroup != null)
        {
            var product = products.FirstOrDefault(p => p.Id == bestProductGroup.Key);
            bestProduct = new {
                id = product?.Id,
                name = product?.Name ?? bestProductGroup.First().ProductName,
                quantitySold = bestProductGroup.Sum(i => i.Quantity)
            };
        }

        var data = new
        {
            GrossRevenue = grossRevenue,
            TotalLosses = totalLosses,
            NetRevenue = netRevenue,
            OrdersCount = confirmedOrders.Count(),
            StockValue = stockValue,
            LowStock = lowStock,
            SalesData = salesData,
            BestClient = bestClient,
            BestProduct = bestProduct
        };

        return Ok(data);
    }
}
