namespace App.Core;

// ============================================
// AUTHENTICATION DTOs
// ============================================

public class LoginRequest
{
    public string CNIC { get; set; } = "";
    public string Password { get; set; } = "";
    public bool UseEF { get; set; }
}

public class LoginResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = "";
    public string? UserID { get; set; }
    public int? RoleID { get; set; }
    public string? RoleName { get; set; }
    public string? CitizenID { get; set; }
    public string? OperatorID { get; set; }
}

// ============================================
// CITIZEN DTOs
// ============================================

public class CitizenRegistrationDto
{
    public string CNIC { get; set; } = "";
    public string FullName { get; set; } = "";
    public string? PhoneNumber { get; set; } = "";
    public int AreaID { get; set; }
    public string Address { get; set; } = "";
    public string Password { get; set; } = "";
}

public class CreateListingDto
{
    public string CitizenID { get; set; } = "";
    public int CategoryID { get; set; }
    public decimal Weight { get; set; }
}

public class PriceEstimateDto
{
    public int CategoryID { get; set; }
    public string CategoryName { get; set; } = "";
    public decimal Weight { get; set; }
    public decimal EstimatedPrice { get; set; }
    public decimal BasePricePerKg { get; set; }
}

public class ListingDto
{
    public int ListingID { get; set; }
    public DateTime CreatedAt { get; set; }
    public string CitizenID { get; set; } = "";
    public int CategoryID { get; set; }
    public string CategoryName { get; set; } = "";
    public decimal Weight { get; set; }
    public string Status { get; set; } = "";
    public decimal? EstimatedPrice { get; set; }
    public int? TransactionID { get; set; }
}

// ============================================
// OPERATOR DTOs
// ============================================

public class CollectionDto
{
    public string OperatorID { get; set; } = "";
    public int ListingID { get; set; }
    public decimal CollectedWeight { get; set; }
    public int WarehouseID { get; set; }
}

public class WarehouseDepositDto
{
    public int WarehouseID { get; set; }
    public int CategoryID { get; set; }
    public decimal Quantity { get; set; }
}

// ============================================
// GOVERNMENT DTOs
// ============================================

public class CreateOperatorDto
{
    public string CNIC { get; set; } = "";
    public string FullName { get; set; } = "";
    public string? PhoneNumber { get; set; }
    public int? RouteID { get; set; }
    public int? WarehouseID { get; set; }
}

public class UpdateCategoryPriceDto
{
    public decimal NewPrice { get; set; }
}

public class CreateCategoryDto
{
    public string CategoryName { get; set; } = "";
    public decimal BasePricePerKg { get; set; }
    public string? Description { get; set; }
}

// ============================================
// VIEW DTOs (matching database views)
// ============================================

public class CitizenProfileView
{
    public string CitizenID { get; set; } = "";
    public string FullName { get; set; } = "";
    public string PhoneNumber { get; set; } = "";
    public string? Address { get; set; }
    public int AreaID { get; set; }
    public string AreaName { get; set; } = "";
    public string City { get; set; } = "";
    public DateTime MemberSince { get; set; }
}

public class OperatorCollectionPointView
{
    public string OperatorID { get; set; } = "";
    public string OperatorName { get; set; } = "";
    public int? RouteID { get; set; }
    public string? RouteName { get; set; }
    public int ListingID { get; set; }
    public string CitizenID { get; set; } = "";
    public string CitizenName { get; set; } = "";
    public string PhoneNumber { get; set; } = "";
    public string? Address { get; set; }
    public string AreaName { get; set; } = "";
    public string CategoryName { get; set; } = "";
    public decimal Weight { get; set; }
    public decimal? EstimatedPrice { get; set; }
    public string Status { get; set; } = "";
    public string? VerificationCode { get; set; }
}

public class WarehouseInventoryView
{
    public int WarehouseID { get; set; }
    public string WarehouseName { get; set; } = "";
    public string AreaName { get; set; } = "";
    public string City { get; set; } = "";
    public double Capacity { get; set; }
    public double CurrentInventory { get; set; }
    public double CapacityUsedPercent { get; set; }
    public double AvailableCapacity { get; set; }
    public int CategoryCount { get; set; }
}

public class ActiveComplaintView
{
    public int ComplaintID { get; set; }
    public string ComplaintType { get; set; } = "";
    public string Description { get; set; } = "";
    public string Status { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public string CitizenID { get; set; } = "";
    public string CitizenName { get; set; } = "";
    public string PhoneNumber { get; set; } = "";
    public string? OperatorID { get; set; }
    public string? OperatorName { get; set; }
    public string? RouteName { get; set; }
    public string AreaName { get; set; } = "";
    public int DaysOpen { get; set; }
}

public class TransactionSummaryView
{
    public int TransactionID { get; set; }
    public string CitizenName { get; set; } = "";
    public decimal TotalAmount { get; set; }
    public string PaymentStatus { get; set; } = "";
    public DateTime TransactionDate { get; set; }
    public int ItemCount { get; set; }
    public decimal TotalWeight { get; set; }
    public string? OperatorName { get; set; }
}

public class OperatorPerformanceView
{
    public string OperatorID { get; set; } = "";
    public string FullName { get; set; } = "";
    public string PhoneNumber { get; set; } = "";
    public int? RouteID { get; set; }
    public int? WarehouseID { get; set; }
    public int TotalPickups { get; set; }
    public decimal TotalCollectedWeight { get; set; }
    public decimal TotalCollectedAmount { get; set; }
}

public class LatestStockByCategoryView
{
    public int WarehouseID { get; set; }
    public int CategoryID { get; set; }
    public string CategoryName { get; set; } = "";
    public DateTime StockDate { get; set; }
    public float CurrentWeight { get; set; }
}

// ============================================
// REPORT DTOs
// ============================================

public class HighYieldAreaReport
{
    public int AreaID { get; set; }
    public string AreaName { get; set; } = "";
    public string City { get; set; } = "";
    public int TotalListings { get; set; }
    public decimal TotalWeight { get; set; }
    public decimal TotalRevenue { get; set; }
    public long RevenueRank { get; set; }
}

public class OperatorPerformanceReport
{
    public string OperatorID { get; set; } = "";
    public string FullName { get; set; } = "";
    public int TotalCollections { get; set; }
    public decimal TotalWeightKg { get; set; }
    public int Complaints { get; set; }
    public string Rating { get; set; } = "";
}
