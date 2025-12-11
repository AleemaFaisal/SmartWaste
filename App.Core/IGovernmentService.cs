namespace App.Core;

/// <summary>
/// Service for government regulator operations (RoleID = 1)
/// </summary>
public interface IGovernmentService
{
    // ============================================
    // WAREHOUSE ANALYTICS
    // ============================================

    /// <summary>
    /// Get warehouse inventory view (all or specific warehouse)
    /// </summary>
    Task<List<WarehouseInventoryView>> GetWarehouseInventoryAsync(int? warehouseID = null);

    /// <summary>
    /// Get all warehouses
    /// </summary>
    Task<List<Warehouse>> GetAllWarehousesAsync();

    // ============================================
    // REPORTS
    // ============================================

    /// <summary>
    /// Analyze high-yield areas using stored procedure
    /// </summary>
    Task<List<HighYieldAreaReport>> AnalyzeHighYieldAreasAsync(DateTime? startDate = null, DateTime? endDate = null);

    /// <summary>
    /// Get operator performance report using stored procedure
    /// </summary>
    Task<List<OperatorPerformanceReport>> GetOperatorPerformanceReportAsync();

    // ============================================
    // CATEGORY MANAGEMENT
    // ============================================

    /// <summary>
    /// Get all waste categories (active and inactive)
    /// </summary>
    Task<List<Category>> GetAllCategoriesAsync();

    /// <summary>
    /// Create a new waste category
    /// </summary>
    Task<int> CreateCategoryAsync(CreateCategoryDto dto);

    /// <summary>
    /// Update base price for a category (triggers price update in listings)
    /// </summary>
    Task<bool> UpdateCategoryPriceAsync(int categoryID, decimal newPrice);

    /// <summary>
    /// Delete a waste category
    /// </summary>
    Task<bool> DeleteCategoryAsync(int categoryID);

    // ============================================
    // OPERATOR MANAGEMENT
    // ============================================

    /// <summary>
    /// Create a new operator account
    /// </summary>
    Task<string> CreateOperatorAsync(CreateOperatorDto dto);

    /// <summary>
    /// Assign operator to a route and warehouse
    /// </summary>
    Task<bool> AssignOperatorToRouteAsync(string operatorID, int routeID, int warehouseID);

    /// <summary>
    /// Deactivate an operator
    /// </summary>
    Task<bool> DeactivateOperatorAsync(string operatorID);

    /// <summary>
    /// Get all operators
    /// </summary>
    Task<List<Operator>> GetAllOperatorsAsync();

    // ============================================
    // COMPLAINTS
    // ============================================

    /// <summary>
    /// Get all complaints (optionally filtered by status)
    /// </summary>
    Task<List<Complaint>> GetAllComplaintsAsync(string? status = null);

    /// <summary>
    /// Update complaint status
    /// </summary>
    Task<bool> UpdateComplaintStatusAsync(int complaintID, string newStatus);

    // ============================================
    // ROUTES & AREAS
    // ============================================

    /// <summary>
    /// Get all routes
    /// </summary>
    Task<List<Route>> GetAllRoutesAsync();

    /// <summary>
    /// Get all areas
    /// </summary>
    Task<List<Area>> GetAllAreasAsync();
}
