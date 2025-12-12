namespace App.Core;

/// <summary>
/// Service for operator operations (RoleID = 3)
/// </summary>
public interface IOperatorService
{
    // ============================================
    // OPERATOR INFO
    // ============================================

    /// <summary>
    /// Get operator details including assigned route and warehouse
    /// </summary>
    Task<Operator?> GetOperatorDetailsAsync(string operatorID);

    // ============================================
    // COLLECTION POINTS
    // ============================================

    /// <summary>
    /// Get pending collection points on operator's assigned route
    /// </summary>
    Task<List<OperatorCollectionPointView>> GetMyCollectionPointsAsync(string operatorID);

    // ============================================
    // PERFORM COLLECTION
    // ============================================

    /// <summary>
    /// Collect waste from a listing and process payment
    /// </summary>
    Task<CollectionResultDto> CollectWasteAsync(CollectionDto dto);

    // ============================================
    // WAREHOUSE OPERATIONS
    // ============================================

    /// <summary>
    /// Deposit collected waste at warehouse
    /// </summary>
    Task<bool> DepositWasteAsync(WarehouseDepositDto dto);

    // ============================================
    // PERFORMANCE
    // ============================================

    /// <summary>
    /// Get collection history for operator
    /// </summary>
    Task<List<Collection>> GetMyCollectionHistoryAsync(string operatorID);

    /// <summary>
    /// Get performance statistics for operator
    /// </summary>
    Task<OperatorPerformanceView?> GetMyPerformanceAsync(string operatorID);

    // ============================================
    // COMPLAINTS
    // ============================================

    /// <summary>
    /// Get active complaints assigned to operator
    /// </summary>
    Task<List<ActiveComplaintView>> GetMyComplaintsAsync(string operatorID);

    /// <summary>
    /// Update complaint status (operator can mark as In Progress or Resolved)
    /// </summary>
    Task<bool> UpdateComplaintStatusAsync(int complaintID, string newStatus);
}
