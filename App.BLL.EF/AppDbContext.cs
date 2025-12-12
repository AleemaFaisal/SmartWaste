using Microsoft.EntityFrameworkCore;
using App.Core;

namespace App.BLL.EF;

public class AppDbContext : DbContext
{
    // ============================================
    // ENTITY DBSETS
    // ============================================

    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Citizen> Citizens => Set<Citizen>();
    public DbSet<Operator> Operators => Set<Operator>();
    public DbSet<Area> Areas => Set<Area>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Warehouse> Warehouses => Set<Warehouse>();
    public DbSet<Route> Routes => Set<Route>();
    public DbSet<WasteListing> WasteListings => Set<WasteListing>();
    public DbSet<TransactionRecord> TransactionRecords => Set<TransactionRecord>();
    public DbSet<Collection> Collections => Set<Collection>();
    public DbSet<Complaint> Complaints => Set<Complaint>();
    public DbSet<WarehouseStock> WarehouseStocks => Set<WarehouseStock>();

    // ============================================
    // VIEW DBSETS (Keyless)
    // ============================================

    public DbSet<CitizenProfileView> CitizenProfiles => Set<CitizenProfileView>();
    public DbSet<OperatorCollectionPointView> OperatorCollectionPoints => Set<OperatorCollectionPointView>();
    public DbSet<WarehouseInventoryView> WarehouseInventory => Set<WarehouseInventoryView>();
    public DbSet<OperatorPerformanceView> OperatorPerformance => Set<OperatorPerformanceView>();
    public DbSet<TransactionSummaryView> TransactionSummaries => Set<TransactionSummaryView>();
    public DbSet<LatestStockByCategoryView> LatestStockByCategory => Set<LatestStockByCategoryView>();
    public DbSet<ActiveComplaintView> ActiveComplaints => Set<ActiveComplaintView>();

    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Set default schema
        modelBuilder.HasDefaultSchema("WasteManagement");

        // ============================================
        // LOOKUP/REFERENCE TABLES
        // ============================================

        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.ToTable("UserRole");
            entity.HasKey(e => e.RoleID);
            entity.Property(e => e.RoleID).UseIdentityColumn();
            entity.Property(e => e.RoleName).HasMaxLength(50).IsRequired();
        });

        modelBuilder.Entity<Area>(entity =>
        {
            entity.ToTable("Area");
            entity.HasKey(e => e.AreaID);
            entity.Property(e => e.AreaID).UseIdentityColumn();
            entity.Property(e => e.AreaName).HasMaxLength(255).IsRequired();
            entity.Property(e => e.City).HasMaxLength(100).IsRequired();
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.ToTable("Category");
            entity.HasKey(e => e.CategoryID);
            entity.Property(e => e.CategoryID).UseIdentityColumn();
            entity.Property(e => e.CategoryName).HasMaxLength(255).IsRequired();
            entity.Property(e => e.BasePricePerKg).HasColumnType("decimal(10,2)").IsRequired();
            entity.Property(e => e.Description).HasColumnType("text");
        });

        // ============================================
        // USER MANAGEMENT
        // ============================================

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Users");
            entity.HasKey(e => e.UserID);
            entity.Property(e => e.UserID).HasMaxLength(15).IsRequired();
            entity.Property(e => e.PasswordHash).HasMaxLength(255).IsRequired();
            entity.Property(e => e.RoleID).IsRequired();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()");

            entity.HasOne(e => e.Role)
                .WithMany()
                .HasForeignKey(e => e.RoleID)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Citizen>(entity =>
        {
            entity.ToTable("Citizen");
            entity.HasKey(e => e.CitizenID);
            entity.Property(e => e.CitizenID).HasMaxLength(15).IsRequired();
            entity.Property(e => e.FullName).HasMaxLength(255).IsRequired();
            entity.Property(e => e.PhoneNumber).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Address).HasMaxLength(500);

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.CitizenID)
                .HasPrincipalKey(u => u.UserID)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Area)
                .WithMany(a => a.Citizens)
                .HasForeignKey(e => e.AreaID)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Operator>(entity =>
        {
            entity.ToTable("Operator");
            entity.HasKey(e => e.OperatorID);
            entity.Property(e => e.OperatorID).HasMaxLength(15).IsRequired();
            entity.Property(e => e.FullName).HasMaxLength(255).IsRequired();
            entity.Property(e => e.PhoneNumber).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Status).HasMaxLength(50).HasDefaultValue("Available");

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.OperatorID)
                .HasPrincipalKey(u => u.UserID)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Route)
                .WithMany(r => r.Operators)
                .HasForeignKey(e => e.RouteID)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.Warehouse)
                .WithMany(w => w.Operators)
                .HasForeignKey(e => e.WarehouseID)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // ============================================
        // INFRASTRUCTURE
        // ============================================

        modelBuilder.Entity<Route>(entity =>
        {
            entity.ToTable("Route");
            entity.HasKey(e => e.RouteID);
            entity.Property(e => e.RouteID).UseIdentityColumn();
            entity.Property(e => e.RouteName).HasMaxLength(255).IsRequired();

            entity.HasOne(e => e.Area)
                .WithMany(a => a.Routes)
                .HasForeignKey(e => e.AreaID)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Warehouse>(entity =>
        {
            entity.ToTable("Warehouse");
            entity.HasKey(e => e.WarehouseID);
            entity.Property(e => e.WarehouseID).UseIdentityColumn();
            entity.Property(e => e.WarehouseName).HasMaxLength(255).IsRequired();
            entity.Property(e => e.Address).HasMaxLength(500).IsRequired();
            entity.Property(e => e.Capacity).IsRequired();
            entity.Property(e => e.CurrentInventory).HasDefaultValue(0);

            entity.HasOne(e => e.Area)
                .WithMany(a => a.Warehouses)
                .HasForeignKey(e => e.AreaID)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<WarehouseStock>(entity =>
        {
            entity.ToTable("WarehouseStock");
            entity.HasKey(e => new { e.WarehouseID, e.CategoryID });
            entity.Property(e => e.CurrentWeight).IsRequired();
            entity.Property(e => e.LastUpdated).HasDefaultValueSql("GETDATE()");

            entity.HasOne(e => e.Warehouse)
                .WithMany(w => w.Stock)
                .HasForeignKey(e => e.WarehouseID)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Category)
                .WithMany()
                .HasForeignKey(e => e.CategoryID)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ============================================
        // PARTITIONED TABLES (Composite Keys)
        // ============================================

        modelBuilder.Entity<WasteListing>(entity =>
        {
            entity.ToTable("WasteListing", tb => tb.UseSqlOutputClause(false));
            // Composite primary key for partitioning
            entity.HasKey(e => new { e.ListingID, e.CreatedAt });
            entity.Property(e => e.ListingID).UseIdentityColumn().ValueGeneratedOnAdd();
            entity.Property(e => e.CitizenID).HasMaxLength(15).IsRequired();
            entity.Property(e => e.Weight).HasColumnType("decimal(10,2)").IsRequired();
            entity.Property(e => e.EstimatedPrice).HasColumnType("decimal(10,2)");
            entity.Property(e => e.Status).HasMaxLength(50).HasDefaultValue("Pending");
            entity.Property(e => e.CreatedAt).ValueGeneratedNever(); // We set this in code

            entity.HasOne(e => e.Citizen)
                .WithMany(c => c.WasteListings)
                .HasForeignKey(e => e.CitizenID)
                .HasPrincipalKey(c => c.CitizenID)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Category)
                .WithMany(c => c.WasteListings)
                .HasForeignKey(e => e.CategoryID)
                .OnDelete(DeleteBehavior.Restrict);
            
            // Ignore Collections navigation - Collection.ListingID is not a true FK relationship
            entity.Ignore(e => e.Collections);
        });

        modelBuilder.Entity<TransactionRecord>(entity =>
        {
            entity.ToTable("TransactionRecord", tb => tb.UseSqlOutputClause(false));
            // Composite primary key for partitioning
            entity.HasKey(e => new { e.TransactionID, e.TransactionDate });
            entity.Property(e => e.TransactionID).UseIdentityColumn();
            entity.Property(e => e.CitizenID).HasMaxLength(15).IsRequired();
            entity.Property(e => e.OperatorID).HasMaxLength(15);
            entity.Property(e => e.TotalAmount).HasColumnType("decimal(10,2)").IsRequired();
            entity.Property(e => e.PaymentStatus).HasMaxLength(50).HasDefaultValue("Pending");
            entity.Property(e => e.PaymentMethod).HasMaxLength(50);
            entity.Property(e => e.VerificationCode).HasMaxLength(100);
            entity.Property(e => e.TransactionDate).HasDefaultValueSql("GETDATE()");

            entity.HasOne(e => e.Citizen)
                .WithMany(c => c.Transactions)
                .HasForeignKey(e => e.CitizenID)
                .HasPrincipalKey(c => c.CitizenID)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Operator)
                .WithMany(o => o.Transactions)
                .HasForeignKey(e => e.OperatorID)
                .HasPrincipalKey(o => o.OperatorID)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Collection>(entity =>
        {
            entity.ToTable("Collection", tb => tb.UseSqlOutputClause(false));
            // Composite primary key for partitioning
            entity.HasKey(e => new { e.CollectionID, e.CollectedDate });
            entity.Property(e => e.CollectionID).UseIdentityColumn();
            entity.Property(e => e.OperatorID).HasMaxLength(15).IsRequired();
            entity.Property(e => e.ListingID).IsRequired(); // Explicit column - not a navigation property
            entity.Property(e => e.WarehouseID).IsRequired();
            entity.Property(e => e.CollectedWeight).HasColumnType("decimal(10,2)").IsRequired();
            entity.Property(e => e.PhotoProof).HasMaxLength(255);
            entity.Property(e => e.IsVerified).HasDefaultValue(false);

            entity.HasOne(e => e.Operator)
                .WithMany(o => o.Collections)
                .HasForeignKey(e => e.OperatorID)
                .HasPrincipalKey(o => o.OperatorID)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Warehouse)
                .WithMany(w => w.Collections)
                .HasForeignKey(e => e.WarehouseID)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ============================================
        // COMPLAINTS
        // ============================================

        modelBuilder.Entity<Complaint>(entity =>
        {
            entity.ToTable("Complaint");
            entity.HasKey(e => e.ComplaintID);
            entity.Property(e => e.ComplaintID).UseIdentityColumn();
            entity.Property(e => e.CitizenID).HasMaxLength(15).IsRequired();
            entity.Property(e => e.OperatorID).HasMaxLength(15);
            entity.Property(e => e.ComplaintType).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Description).HasColumnType("text").IsRequired();
            entity.Property(e => e.Status).HasMaxLength(50).HasDefaultValue("Open");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()");

            entity.HasOne(e => e.Citizen)
                .WithMany(c => c.Complaints)
                .HasForeignKey(e => e.CitizenID)
                .HasPrincipalKey(c => c.CitizenID)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Operator)
                .WithMany()
                .HasForeignKey(e => e.OperatorID)
                .HasPrincipalKey(o => o.OperatorID)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // ============================================
        // VIEWS (Keyless Entities)
        // ============================================

        modelBuilder.Entity<CitizenProfileView>(entity =>
        {
            entity.ToView("vw_CitizenProfile");
            entity.HasNoKey();
        });

        modelBuilder.Entity<OperatorCollectionPointView>(entity =>
        {
            entity.ToView("vw_OperatorCollectionPoints");
            entity.HasNoKey();
        });

        modelBuilder.Entity<WarehouseInventoryView>(entity =>
        {
            entity.ToView("vw_WarehouseInventory");
            entity.HasNoKey();
        });

        modelBuilder.Entity<OperatorPerformanceView>(entity =>
        {
            entity.ToView("vw_OperatorPerformance");
            entity.HasNoKey();
        });

        modelBuilder.Entity<TransactionSummaryView>(entity =>
        {
            entity.ToView("vw_TransactionSummary");
            entity.HasNoKey();
        });

        modelBuilder.Entity<LatestStockByCategoryView>(entity =>
        {
            entity.ToView("vw_LatestStockByCategory");
            entity.HasNoKey();
        });

        modelBuilder.Entity<ActiveComplaintView>(entity =>
        {
            entity.ToView("vw_ActiveComplaints");
            entity.HasNoKey();
        });
    }

    // ============================================
    // USER-DEFINED FUNCTIONS (for LINQ integration)
    // ============================================

    /// <summary>
    /// Calculate price using database function
    /// </summary>
    [DbFunction("fn_CalculatePrice", "WasteManagement")]
    public decimal CalculatePrice(int categoryID, decimal weight)
    {
        throw new NotImplementedException("This method is translated to SQL");
    }

    /// <summary>
    /// Validate CNIC using database function
    /// </summary>
    [DbFunction("fn_ValidateCNIC", "WasteManagement")]
    public bool ValidateCNIC(string cnic)
    {
        throw new NotImplementedException("This method is translated to SQL");
    }

    /// <summary>
    /// Check warehouse capacity using database function
    /// </summary>
    [DbFunction("fn_CheckWarehouseCapacity", "WasteManagement")]
    public bool CheckWarehouseCapacity(int warehouseID, float additionalWeight)
    {
        throw new NotImplementedException("This method is translated to SQL");
    }
}
