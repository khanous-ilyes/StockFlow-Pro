using Microsoft.EntityFrameworkCore;
using StockFlowPro.Domain.Entities;
using System.Threading;
using System.Threading.Tasks;

namespace StockFlowPro.Application.Interfaces;

public interface IAppDbContext
{
    DbSet<Tenant> Tenants { get; set; }
    DbSet<User> Users { get; set; }
    DbSet<Category> Categories { get; set; }
    DbSet<Product> Products { get; set; }
    DbSet<Client> Clients { get; set; }
    DbSet<Order> Orders { get; set; }
    DbSet<OrderItem> OrderItems { get; set; }
    DbSet<Invoice> Invoices { get; set; }
    DbSet<StockMovement> StockMovements { get; set; }
    DbSet<ClientPayment> ClientPayments { get; set; }
    DbSet<Notification> Notifications { get; set; }
    DbSet<OfflineLicense> OfflineLicenses { get; set; }

    Guid CurrentTenantId { get; }
    void SetCurrentTenant(Guid tenantId);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
