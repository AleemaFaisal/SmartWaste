namespace App.Core;

// ============================================
// LOOKUP/REFERENCE TABLES
// ============================================

public class UserRole
{
    public int RoleID { get; set; }
    public string RoleName { get; set; } = "";
}

public class Area
{
    public int AreaID { get; set; }
    public string AreaName { get; set; } = "";
    public string City { get; set; } = "";

    // Navigation properties
    public List<Citizen> Citizens { get; set; } = new();
    public List<Route> Routes { get; set; } = new();
    public List<Warehouse> Warehouses { get; set; } = new();
}

public class Category
{
    public int CategoryID { get; set; }
    public string CategoryName { get; set; } = "";
    public decimal BasePricePerKg { get; set; }
    public string? Description { get; set; }

    // Navigation properties
    public List<WasteListing> WasteListings { get; set; } = new();
}

// ============================================
// USER MANAGEMENT
// ============================================

public class User
{
    public string UserID { get; set; } = ""; // CNIC format: 12345-1234567-1
    public string PasswordHash { get; set; } = "";
    public int RoleID { get; set; }
    public DateTime CreatedAt { get; set; }

    // Navigation properties
    public UserRole? Role { get; set; }
}

public class Citizen
{
    public string CitizenID { get; set; } = ""; // Foreign key to Users.UserID (CNIC)
    public string FullName { get; set; } = "";
    public string? PhoneNumber { get; set; } = "";
    public int AreaID { get; set; }
    public string? Address { get; set; }

    // Navigation properties
    public User? User { get; set; }
    public Area? Area { get; set; }
    public List<WasteListing> WasteListings { get; set; } = new();
    public List<TransactionRecord> Transactions { get; set; } = new();
    public List<Complaint> Complaints { get; set; } = new();
}

public class Operator
{
    public string OperatorID { get; set; } = ""; // Foreign key to Users.UserID (CNIC)
    public string FullName { get; set; } = "";
    public string PhoneNumber { get; set; } = "";
    public int? RouteID { get; set; }
    public int? WarehouseID { get; set; }
    public string Status { get; set; } = "Available"; // Available, Busy, Offline

    // Navigation properties
    public User? User { get; set; }
    public Route? Route { get; set; }
    public Warehouse? Warehouse { get; set; }
    public List<Collection> Collections { get; set; } = new();
    public List<TransactionRecord> Transactions { get; set; } = new();
}



// ============================================
// INFRASTRUCTURE
// ============================================

public class Route
{
    public string RouteName { get; set; } = "";
    public int AreaID { get; set; }
    public int? RouteID { get; set; }
    // public Route? Route { get; set; }


    // Navigation properties
    public Area? Area { get; set; }
    public List<Operator> Operators { get; set; } = new();
}

public class Warehouse
{
    public int WarehouseID { get; set; }
    public string WarehouseName { get; set; } = "";
    public int AreaID { get; set; }
    public string Address { get; set; } = "";
    public double Capacity { get; set; }
    public double CurrentInventory { get; set; }

    // Navigation properties
    public Area? Area { get; set; }
    public List<Operator> Operators { get; set; } = new();
    public List<WarehouseStock> Stock { get; set; } = new();
    public List<Collection> Collections { get; set; } = new();
}

public class WarehouseStock
{
    public int WarehouseID { get; set; }
    public int CategoryID { get; set; }
    public double CurrentWeight { get; set; }
    public DateTime LastUpdated { get; set; }

    // Navigation properties
    public Warehouse? Warehouse { get; set; }
    public Category? Category { get; set; }
}

// ============================================
// PARTITIONED TABLES (Composite Keys)
// ============================================

public class WasteListing
{
    // Composite primary key for partitioning
    public int ListingID { get; set; }
    public DateTime CreatedAt { get; set; }

    // Foreign keys
    public string CitizenID { get; set; } = "";
    public int CategoryID { get; set; }

    // Data
    public decimal Weight { get; set; }
    public string Status { get; set; } = "Pending"; // Pending, Collected, Completed, Cancelled
    public decimal? EstimatedPrice { get; set; }
    public int? TransactionID { get; set; }
    public string CategoryName => Category?.CategoryName ?? "";


    // Navigation properties
    public Citizen? Citizen { get; set; }
    public Category? Category { get; set; }
    public List<Collection> Collections { get; set; } = new();
}

public class TransactionRecord
{
    // Composite primary key for partitioning
    public int TransactionID { get; set; }
    public DateTime TransactionDate { get; set; }

    // Foreign keys
    public string CitizenID { get; set; } = "";
    public string? OperatorID { get; set; }

    // Data
    public decimal TotalAmount { get; set; }
    public string PaymentStatus { get; set; } = "Pending"; // Pending, Completed, Failed
    public string? PaymentMethod { get; set; }
    public string? VerificationCode { get; set; }

    // Navigation properties
    public Citizen? Citizen { get; set; }
    public Operator? Operator { get; set; }
}

public class Collection
{
    // Composite primary key for partitioning
    public int CollectionID { get; set; }
    public DateTime CollectedDate { get; set; }

    // Foreign keys
    public string OperatorID { get; set; } = "";
    public int ListingID { get; set; }
    public int WarehouseID { get; set; }

    // Data
    public decimal CollectedWeight { get; set; }
    public string? PhotoProof { get; set; }
    public bool IsVerified { get; set; }

    // Navigation properties
    public Operator? Operator { get; set; }
    public Warehouse? Warehouse { get; set; }
}

// ============================================
// COMPLAINTS
// ============================================

public class Complaint
{
    public int ComplaintID { get; set; }
    public string CitizenID { get; set; } = "";
    public string? OperatorID { get; set; }
    public string ComplaintType { get; set; } = "";
    public string Description { get; set; } = "";
    public string Status { get; set; } = "Open"; // Open, In Progress, Resolved, Closed
    public DateTime CreatedAt { get; set; }

    // Navigation properties
    public Citizen? Citizen { get; set; }
    public Operator? Operator { get; set; }
}
