namespace App.Core;

/// <summary>
/// Service for citizen operations (RoleID = 2)
/// </summary>
public interface ICitizenService
{
    // ============================================
    // REGISTRATION
    // ============================================

    /// <summary>
    /// Register a new citizen account
    /// </summary>
    Task<string> RegisterCitizenAsync(CitizenRegistrationDto dto);

    // ============================================
    // WASTE LISTINGS
    // ============================================

    /// <summary>
    /// Create a new waste listing
    /// </summary>
    Task<int> CreateWasteListingAsync(CreateListingDto dto);

    /// <summary>
    /// Get all waste listings for a citizen
    /// </summary>
    Task<List<ListingDto>> GetMyListingsAsync(string citizenID);

    /// <summary>
    /// Cancel a pending waste listing
    /// </summary>
    Task<bool> CancelListingAsync(int listingID, string citizenID);

    // ============================================
    // PRICE ESTIMATION
    // ============================================

    /// <summary>
    /// Calculate price for waste using database function
    /// </summary>
    Task<decimal> CalculatePriceAsync(int categoryID, decimal weight);

    /// <summary>
    /// Get detailed price estimate with category info
    /// </summary>
    Task<PriceEstimateDto> GetPriceEstimateAsync(int categoryID, decimal weight);

    // ============================================
    // TRANSACTIONS
    // ============================================

    /// <summary>
    /// Get transaction history for a citizen
    /// </summary>
    Task<List<TransactionRecord>> GetMyTransactionsAsync(string citizenID);

    // ============================================
    // PROFILE
    // ============================================

    /// <summary>
    /// Get citizen profile from view
    /// </summary>
    Task<CitizenProfileView?> GetMyProfileAsync(string citizenID);

    // ============================================
    // REFERENCE DATA
    // ============================================

    /// <summary>
    /// Get all available areas
    /// </summary>
    Task<List<Area>> GetAreasAsync();

    /// <summary>
    /// Get all active waste categories
    /// </summary>
    Task<List<Category>> GetActiveCategoriesAsync();
}
