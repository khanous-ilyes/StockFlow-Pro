using Microsoft.EntityFrameworkCore;
using StockFlowPro.Domain.Entities;
using StockFlowPro.Application.Interfaces;

namespace StockFlowPro.Infrastructure.Persistence;

public class AppDbContext : DbContext, IAppDbContext
{
    private Guid _currentTenantId;

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public Guid CurrentTenantId => _currentTenantId;

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = new CancellationToken())
    {
        foreach (var entry in ChangeTracker.Entries<TenantEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    if (entry.Entity.TenantId == Guid.Empty && _currentTenantId != Guid.Empty)
                    {
                        entry.Entity.TenantId = _currentTenantId;
                    }
                    break;
            }
        }
        
        return base.SaveChangesAsync(cancellationToken);
    }

    public DbSet<Tenant> Tenants { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<Product> Products { get; set; }
    public DbSet<Client> Clients { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderItem> OrderItems { get; set; }
    public DbSet<Invoice> Invoices { get; set; }
    public DbSet<StockMovement> StockMovements { get; set; }
    public DbSet<ClientPayment> ClientPayments { get; set; }
    public DbSet<Notification> Notifications { get; set; }
    public DbSet<OfflineLicense> OfflineLicenses { get; set; }

    // Pour l'instant, on n'a pas HttpContext injecté ici pour simplifier.
    // Il sera injecté via un Interceptor ou géré explicitement. On simule pour la conception.
    public void SetCurrentTenant(Guid tenantId)
    {
        _currentTenantId = tenantId;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Global Query Filters pour la Multi-tenancy
        modelBuilder.Entity<User>().HasQueryFilter(e => e.TenantId == _currentTenantId);
        modelBuilder.Entity<Category>().HasQueryFilter(e => e.TenantId == _currentTenantId);
        modelBuilder.Entity<Product>().HasQueryFilter(e => e.TenantId == _currentTenantId);
        modelBuilder.Entity<Client>().HasQueryFilter(e => e.TenantId == _currentTenantId);
        modelBuilder.Entity<Order>().HasQueryFilter(e => e.TenantId == _currentTenantId);
        modelBuilder.Entity<Invoice>().HasQueryFilter(e => e.TenantId == _currentTenantId);
        modelBuilder.Entity<StockMovement>().HasQueryFilter(e => e.TenantId == _currentTenantId);
        modelBuilder.Entity<ClientPayment>().HasQueryFilter(e => e.TenantId == _currentTenantId);
        modelBuilder.Entity<Notification>().HasQueryFilter(e => e.TenantId == _currentTenantId);

        // Clés uniques et configurations de base
        modelBuilder.Entity<Tenant>().HasIndex(t => t.Slug).IsUnique();
        modelBuilder.Entity<User>().HasIndex(u => u.Email).IsUnique();

        modelBuilder.Entity<Product>()
            .HasIndex(p => new { p.TenantId, p.SKU })
            .IsUnique();

        // Relations
        modelBuilder.Entity<Order>()
            .HasMany(o => o.Invoices)
            .WithOne()
            .HasForeignKey(i => i.OrderId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Category>()
            .HasMany(c => c.SubCategories)
            .WithOne(c => c.Parent)
            .HasForeignKey(c => c.ParentId)
            .OnDelete(DeleteBehavior.Restrict);
            
        modelBuilder.Entity<OrderItem>()
            .HasOne(oi => oi.Order)
            .WithMany(o => o.Items)
            .HasForeignKey(oi => oi.OrderId)
            .OnDelete(DeleteBehavior.Cascade);
            
        // Éviter des cycles sur cascades Delete avec SQLite :
        foreach (var relationship in modelBuilder.Model.GetEntityTypes().SelectMany(e => e.GetForeignKeys()))
        {
            if (relationship.DeleteBehavior == DeleteBehavior.Cascade && !relationship.IsOwnership)
            {
                relationship.DeleteBehavior = DeleteBehavior.Restrict;
            }
        }

        // Data Seeding - Default SuperAdmin
        var superAdminTenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");
        modelBuilder.Entity<Tenant>().HasData(new Tenant
        {
            Id = superAdminTenantId,
            Name = "Super Admin System",
            Slug = "superadmin-system",
            IsActive = true,
            Mode = StockFlowPro.Domain.Enums.SubscriptionMode.Cloud
        });

        modelBuilder.Entity<User>().HasData(new User
        {
            Id = Guid.Parse("00000000-0000-0000-0000-000000000002"),
            TenantId = superAdminTenantId,
            Email = "superadmin@stockflowpro.dz",
            FullName = "Super Administrator",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("SuperAdmin@123!", 12),
            Role = StockFlowPro.Domain.Enums.UserRole.SuperAdmin,
            IsActive = true
        });
    }
}
