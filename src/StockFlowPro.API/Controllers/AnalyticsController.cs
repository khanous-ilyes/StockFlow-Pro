using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockFlowPro.Application.Interfaces;
using StockFlowPro.Domain.Enums;

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

        var confirmedOrders = orders.Where(o => o.Status != OrderStatus.Draft && o.Status != OrderStatus.Cancelled).ToList();

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

    [HttpGet("supplier-performance")]
    public async Task<IActionResult> GetSupplierPerformance([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
    {
        var ordersQuery = _context.Orders
            .Include(o => o.Items)
            .ThenInclude(i => i.Product)
            .Where(o => o.Status != OrderStatus.Draft && o.Status != OrderStatus.Cancelled) // Confirmed orders
            .AsQueryable();

        if (startDate.HasValue)
            ordersQuery = ordersQuery.Where(o => o.CreatedAt >= startDate.Value);
        if (endDate.HasValue)
            ordersQuery = ordersQuery.Where(o => o.CreatedAt <= endDate.Value);

        var confirmedOrders = await ordersQuery.ToListAsync();

        var supplierSales = confirmedOrders
            .SelectMany(o => o.Items)
            .Where(i => i.Product?.SupplierId != null)
            .GroupBy(i => i.Product!.SupplierId)
            .Select(g => new {
                SupplierId = g.Key,
                TotalSales = g.Sum(i => i.Quantity * i.UnitPrice),
                QuantitySold = g.Sum(i => i.Quantity)
            })
            .ToList();

        var suppliers = await _context.Suppliers.ToListAsync();

        var result = suppliers.Select(s => {
            var sales = supplierSales.FirstOrDefault(x => x.SupplierId == s.Id);
            return new {
                supplierId = s.Id,
                supplierName = s.Name,
                totalSales = sales?.TotalSales ?? 0,
                quantitySold = sales?.QuantitySold ?? 0,
                currentDebt = s.TotalDebt
            };
        }).OrderByDescending(x => x.totalSales).ToList();

        return Ok(result);
    }

    [HttpGet("product-buyers/{productId}")]
    public async Task<IActionResult> GetProductBuyers(Guid productId, [FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
    {
        var ordersQuery = _context.Orders
            .Include(o => o.Items)
            .Include(o => o.Client)
            .Where(o => o.Status != OrderStatus.Draft && o.Status != OrderStatus.Cancelled && o.Items.Any(i => i.ProductId == productId))
            .AsQueryable();

        if (startDate.HasValue)
            ordersQuery = ordersQuery.Where(o => o.CreatedAt >= startDate.Value);
        if (endDate.HasValue)
            ordersQuery = ordersQuery.Where(o => o.CreatedAt <= endDate.Value);

        var orders = await ordersQuery.ToListAsync();

        var buyers = orders.Where(o => o.Client != null)
            .GroupBy(o => o.Client!.Id)
            .Select(g => new {
                clientId = g.Key,
                clientName = g.First().Client!.FullName,
                totalPurchasedQuantity = g.SelectMany(o => o.Items).Where(i => i.ProductId == productId).Sum(i => i.Quantity),
                totalSpentOnProduct = g.SelectMany(o => o.Items).Where(i => i.ProductId == productId).Sum(i => i.TotalPrice),
                lastPurchaseDate = g.Max(o => o.CreatedAt)
            })
            .OrderByDescending(x => x.totalSpentOnProduct)
            .ToList();

        var totalQuantity = buyers.Sum(b => b.totalPurchasedQuantity);
        var totalRevenue = buyers.Sum(b => b.totalSpentOnProduct);

        return Ok(new {
            productId,
            buyers,
            totalQuantity,
            totalRevenue
        });
    }

    [HttpGet("client-history/{clientId}")]
    public async Task<IActionResult> GetClientHistory(Guid clientId)
    {
        var client = await _context.Clients.FindAsync(clientId);
        if (client == null) return NotFound("Client not found");

        var orders = await _context.Orders
            .Include(o => o.Items)
            .ThenInclude(i => i.Product)
            .Where(o => o.ClientId == clientId && o.Status != OrderStatus.Draft && o.Status != OrderStatus.Cancelled)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

        var favoriteProducts = orders.SelectMany(o => o.Items)
            .GroupBy(i => i.ProductId)
            .Select(g => new {
                productId = g.Key,
                productName = g.First().ProductName,
                quantityBought = g.Sum(i => i.Quantity),
                totalSpent = g.Sum(i => i.TotalPrice)
            })
            .OrderByDescending(x => x.totalSpent)
            .Take(10)
            .ToList();

        var result = new {
            client = new { client.Id, client.FullName, client.TotalPurchases, client.CurrentBalance },
            totalOrders = orders.Count,
            totalSpent = orders.Sum(o => o.TotalAmount),
            recentOrders = orders.Select(o => new {
                o.Id,
                o.OrderNumber,
                o.TotalAmount,
                o.CreatedAt,
                o.AmountPaid,
                o.RemainingCredit,
                itemsCount = o.Items.Count
            }),
            favoriteProducts
        };

        return Ok(result);
    }

    [HttpGet("stock-status")]
    public async Task<IActionResult> GetStockStatus([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
    {
        var products = await _context.Products.Include(p => p.Category).OrderBy(p => p.Name).ToListAsync();

        // Get confirmed order items within date range
        var ordersQuery = _context.Orders.Include(o => o.Items)
            .Where(o => o.Status != OrderStatus.Draft && o.Status != OrderStatus.Cancelled)
            .AsQueryable();
        if (startDate.HasValue) ordersQuery = ordersQuery.Where(o => o.CreatedAt >= startDate.Value);
        if (endDate.HasValue) ordersQuery = ordersQuery.Where(o => o.CreatedAt <= endDate.Value);
        var confirmedOrders = await ordersQuery.ToListAsync();

        // Get stock movements (purchases / entries) within date range
        var movementsQuery = _context.StockMovements.AsQueryable();
        if (startDate.HasValue) movementsQuery = movementsQuery.Where(m => m.CreatedAt >= startDate.Value);
        if (endDate.HasValue) movementsQuery = movementsQuery.Where(m => m.CreatedAt <= endDate.Value);
        var movements = await movementsQuery.ToListAsync();

        var soldByProduct = confirmedOrders
            .SelectMany(o => o.Items)
            .GroupBy(i => i.ProductId)
            .ToDictionary(g => g.Key, g => new { Qty = g.Sum(i => i.Quantity), Revenue = g.Sum(i => i.TotalPrice) });

        var purchasedByProduct = movements
            .Where(m => m.Type == StockFlowPro.Domain.Enums.StockMovementType.In)
            .GroupBy(m => m.ProductId)
            .ToDictionary(g => g.Key, g => g.Sum(m => m.Quantity));

        int index = 1;
        var rows = products.Select(p =>
        {
            var sold = soldByProduct.GetValueOrDefault(p.Id);
            var purchased = purchasedByProduct.GetValueOrDefault(p.Id, 0);
            decimal qtySold = sold?.Qty ?? 0;
            decimal revenue = sold?.Revenue ?? 0;
            decimal stockInitial = p.StockQuantity + qtySold - purchased;

            return new
            {
                no = index++,
                id = p.Id,
                designation = p.Name,
                sku = p.SKU,
                category = p.Category?.Name,
                stockInitial = stockInitial,
                quantityPurchased = purchased,
                quantitySold = qtySold,
                stockActual = p.StockQuantity,
                costPrice = p.CostPrice,
                sellingPrice = p.UnitPrice,
                revenue = revenue,
                percentage = stockInitial > 0 ? Math.Round((qtySold / stockInitial) * 100, 1) : 0
            };
        }).ToList();

        return Ok(new
        {
            rows,
            totalProducts = rows.Count,
            totalStockValue = products.Sum(p => p.StockQuantity * p.CostPrice),
            totalRevenue = rows.Sum(r => r.revenue)
        });
    }

    [HttpGet("stock-card/{productId}")]
    public async Task<IActionResult> GetStockCard(Guid productId)
    {
        var product = await _context.Products.Include(p => p.Category).FirstOrDefaultAsync(p => p.Id == productId);
        if (product == null) return NotFound("Product not found");

        // Get all stock movements for this product
        var movements = await _context.StockMovements
            .Where(m => m.ProductId == productId)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync();

        // Get all confirmed order items for this product with order details
        var orderItems = await _context.Orders
            .Include(o => o.Items)
            .Include(o => o.Client)
            .Where(o => o.Status != OrderStatus.Draft && o.Status != OrderStatus.Cancelled && o.Items.Any(i => i.ProductId == productId))
            .OrderBy(o => o.CreatedAt)
            .ToListAsync();

        // Build operation log
        var operations = new List<object>();
        int opNo = 1;

        // 1. INITIAL entry (compute from movements)
        decimal initialStock = product.StockQuantity;
        decimal totalSold = orderItems.SelectMany(o => o.Items).Where(i => i.ProductId == productId).Sum(i => i.Quantity);
        decimal totalPurchased = movements.Where(m => m.Type == StockFlowPro.Domain.Enums.StockMovementType.In).Sum(m => m.Quantity);
        decimal computedInitial = initialStock + totalSold - totalPurchased;

        operations.Add(new
        {
            no = opNo++,
            operation = "INITIAL",
            reference = "",
            clientOrSupplier = "",
            date = (DateTime?)null,
            entryQty = computedInitial,
            entryPrice = (decimal?)null,
            exitQty = (decimal?)null,
            exitPrice = (decimal?)null,
            value = 0m
        });

        // 2. Purchase entries (stock movements type = In)
        foreach (var m in movements.Where(m => m.Type == StockFlowPro.Domain.Enums.StockMovementType.In))
        {
            operations.Add(new
            {
                no = opNo++,
                operation = "Achat",
                reference = m.Reference ?? "",
                clientOrSupplier = m.Reason ?? "",
                date = (DateTime?)m.CreatedAt,
                entryQty = (decimal?)m.Quantity,
                entryPrice = (decimal?)product.CostPrice,
                exitQty = (decimal?)null,
                exitPrice = (decimal?)null,
                value = m.Quantity * product.CostPrice
            });
        }

        // 3. Sale entries (from confirmed orders)
        foreach (var order in orderItems)
        {
            var items = order.Items.Where(i => i.ProductId == productId);
            foreach (var item in items)
            {
                operations.Add(new
                {
                    no = opNo++,
                    operation = "Vente BL",
                    reference = order.OrderNumber,
                    clientOrSupplier = order.Client?.FullName ?? "Comptoir",
                    date = (DateTime?)order.CreatedAt,
                    entryQty = (decimal?)null,
                    entryPrice = (decimal?)null,
                    exitQty = (decimal?)item.Quantity,
                    exitPrice = (decimal?)item.UnitPrice,
                    value = item.TotalPrice
                });
            }
        }

        // Summary boxes
        var summary = new
        {
            initialStock = computedInitial,
            purchasedQty = totalPurchased,
            purchasedReturns = 0m,
            purchasedReal = totalPurchased,
            soldBLQty = totalSold,
            soldReturns = 0m,
            soldReal = totalSold,
            counterSalesQty = 0m,
            counterReturns = 0m,
            counterReal = 0m,
            currentStock = product.StockQuantity
        };

        return Ok(new
        {
            product = new
            {
                product.Id,
                product.Name,
                product.SKU,
                category = product.Category?.Name,
                product.CostPrice,
                product.UnitPrice,
                product.Unit
            },
            operations,
            summary
        });
    }
}
