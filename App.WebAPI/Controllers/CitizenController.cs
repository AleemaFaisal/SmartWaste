using Microsoft.AspNetCore.Mvc;
using App.Core;
using App.Factory;

namespace App.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CitizenController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly string _connectionString;
    private readonly bool _useEf;

    public CitizenController(IConfiguration configuration)
    {
        _configuration = configuration;
        _connectionString = _configuration.GetConnectionString("SmartWasteDB")!;
        _useEf = _configuration.GetValue<bool>("AppSettings:UseEntityFramework");
    }

    /// <summary>
    /// Determines whether to use Entity Framework based on request header or default config
    /// </summary>
    private bool ShouldUseEF()
    {
        // Check if there's a custom header to override the default
        if (Request.Headers.TryGetValue("X-Use-EF", out var headerValue))
        {
            var useEf = headerValue.ToString().ToLower() == "true";
            Console.WriteLine($"[CitizenController] X-Use-EF header found: {headerValue} -> Using {(useEf ? "EF" : "SP")}");
            return useEf;
        }

        // Fall back to configuration default
        Console.WriteLine($"[CitizenController] No X-Use-EF header, using config default: {(_useEf ? "EF" : "SP")}");
        return _useEf;
    }

    // GET: api/citizen/profile/{citizenID}
    [HttpGet("profile/{citizenID}")]
    public async Task<IActionResult> GetProfile(string citizenID)
    {
        try
        {
            var service = ServiceFactory.CreateCitizenService(ShouldUseEF(), _connectionString);
            var profile = await service.GetMyProfileAsync(citizenID);

            if (profile == null)
            {
                return NotFound(new { message = "Profile not found" });
            }

            return Ok(profile);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    // GET: api/citizen/listings/{citizenID}
    [HttpGet("listings/{citizenID}")]
    public async Task<IActionResult> GetListings(string citizenID)
    {
        try
        {
            var service = ServiceFactory.CreateCitizenService(ShouldUseEF(), _connectionString);
            var listings = await service.GetMyListingsAsync(citizenID);

            return Ok(listings);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    // POST: api/citizen/listings
    [HttpPost("listings")]
    public async Task<IActionResult> CreateListing([FromBody] CreateListingDto dto)
    {
        try
        {
            var service = ServiceFactory.CreateCitizenService(ShouldUseEF(), _connectionString);
            var listingId = await service.CreateWasteListingAsync(dto);

            if (listingId > 0)
            {
                return Ok(new { success = true, listingId, message = "Listing created successfully" });
            }

            return BadRequest(new { success = false, message = "Failed to create listing" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    // PUT: api/citizen/listings/{listingID}/cancel
    [HttpPut("listings/{listingID}/cancel")]
    public async Task<IActionResult> CancelListing(int listingID, [FromBody] CancelListingRequest request)
    {
        try
        {
            Console.WriteLine($"[CitizenController] CancelListing - ListingID: {listingID}, CitizenID: '{request.CitizenID}'");

            var service = ServiceFactory.CreateCitizenService(ShouldUseEF(), _connectionString);
            var success = await service.CancelListingAsync(listingID, request.CitizenID);

            if (success)
            {
                return Ok(new { success = true, message = "Listing cancelled successfully" });
            }

            return BadRequest(new { success = false, message = "Failed to cancel listing" });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CitizenController] CancelListing ERROR: {ex.Message}");
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    // GET: api/citizen/transactions/{citizenID}
    [HttpGet("transactions/{citizenID}")]
    public async Task<IActionResult> GetTransactions(string citizenID)
    {
        try
        {
            var service = ServiceFactory.CreateCitizenService(ShouldUseEF(), _connectionString);
            var transactions = await service.GetMyTransactionsAsync(citizenID);

            return Ok(transactions);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    // GET: api/citizen/categories
    [HttpGet("categories")]
    public async Task<IActionResult> GetCategories()
    {
        try
        {
            var service = ServiceFactory.CreateCitizenService(ShouldUseEF(), _connectionString);
            var categories = await service.GetActiveCategoriesAsync();

            return Ok(categories);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    // GET: api/citizen/areas
    [HttpGet("areas")]
    public async Task<IActionResult> GetAreas()
    {
        try
        {
            var service = ServiceFactory.CreateCitizenService(ShouldUseEF(), _connectionString);
            var areas = await service.GetAreasAsync();

            return Ok(areas);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    // POST: api/citizen/price-estimate
    [HttpPost("price-estimate")]
    public async Task<IActionResult> GetPriceEstimate([FromBody] PriceEstimateRequest request)
    {
        try
        {
            var service = ServiceFactory.CreateCitizenService(ShouldUseEF(), _connectionString);
            var estimate = await service.GetPriceEstimateAsync(request.CategoryID, request.Weight);

            return Ok(estimate);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    // POST: api/citizen/register
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] CitizenRegistrationDto dto)
    {
        try
        {
            var service = ServiceFactory.CreateCitizenService(ShouldUseEF(), _connectionString);
            var citizenId = await service.RegisterCitizenAsync(dto);

            return Ok(new { success = true, citizenId, message = "Registration successful" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }
}

public record CancelListingRequest(string CitizenID);
public record PriceEstimateRequest(int CategoryID, decimal Weight);
