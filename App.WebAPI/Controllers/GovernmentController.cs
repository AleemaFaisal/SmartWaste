using Microsoft.AspNetCore.Mvc;
using App.Core;
using App.Factory;

namespace App.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GovernmentController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly string _connectionString;
    private readonly bool _useEf;

    public GovernmentController(IConfiguration configuration)
    {
        _configuration = configuration;
        _connectionString = _configuration.GetConnectionString("SmartWasteDB")!;
        _useEf = _configuration.GetValue<bool>("AppSettings:UseEntityFramework");
    }

    private bool ShouldUseEF()
    {
        if (Request.Headers.TryGetValue("X-Use-EF", out var headerValue))
        {
            var useEf = headerValue.ToString().ToLower() == "true";
            Console.WriteLine($"[GovernmentController] Using EF? {useEf}");
            return useEf;
        }

        Console.WriteLine($"[GovernmentController] Using default EF = {_useEf}");
        return _useEf;
    }

    // ============================================
    // WAREHOUSE ANALYTICS
    // ============================================

    // GET: api/government/warehouse-inventory
    [HttpGet("warehouse-inventory")]
    public async Task<IActionResult> GetWarehouseInventory([FromQuery] int? warehouseID = null)
    {
        try
        {
            var service = ServiceFactory.CreateGovernmentService(ShouldUseEF(), _connectionString);
            var result = await service.GetWarehouseInventoryAsync(warehouseID);
            return Ok(result);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            return StatusCode(500, new { message = ex.Message });
        }
    }

    // GET: api/government/warehouses
    [HttpGet("warehouses")]
    public async Task<IActionResult> GetAllWarehouses()
    {
        try
        {
            var service = ServiceFactory.CreateGovernmentService(ShouldUseEF(), _connectionString);
            var result = await service.GetAllWarehousesAsync();
            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    // ============================================
    // REPORTS
    // ============================================

    // GET: api/government/reports/high-yield?startDate=&endDate=
    [HttpGet("reports/high-yield")]
    public async Task<IActionResult> GetHighYieldAreas([FromQuery] DateTime? startDate = null, [FromQuery] DateTime? endDate = null)
    {
        try
        {
            var service = ServiceFactory.CreateGovernmentService(ShouldUseEF(), _connectionString);
            var result = await service.AnalyzeHighYieldAreasAsync(startDate, endDate);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    // GET: api/government/reports/operator-performance
    [HttpGet("reports/operator-performance")]
    public async Task<IActionResult> GetOperatorPerformanceReport()
    {
        try
        {
            var service = ServiceFactory.CreateGovernmentService(ShouldUseEF(), _connectionString);
            var result = await service.GetOperatorPerformanceReportAsync();
            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    // ============================================
    // CATEGORY MANAGEMENT
    // ============================================

    // GET: api/government/categories
    [HttpGet("categories")]
    public async Task<IActionResult> GetAllCategories()
    {
        try
        {
            var service = ServiceFactory.CreateGovernmentService(ShouldUseEF(), _connectionString);
            var categories = await service.GetAllCategoriesAsync();
            return Ok(categories);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    // POST: api/government/categories
    [HttpPost("categories")]
    public async Task<IActionResult> CreateCategory([FromBody] CreateCategoryDto dto)
    {
        try
        {
            var service = ServiceFactory.CreateGovernmentService(ShouldUseEF(), _connectionString);
            var id = await service.CreateCategoryAsync(dto);

            return Ok(new { success = true, categoryID = id, message = "Category created successfully" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    // PUT: api/government/categories/price
    [HttpPut("categories/{categoryID}/price")]
    public async Task<IActionResult> UpdateCategoryPrice(int categoryID, [FromBody] UpdateCategoryPriceDto dto)
    {
        try
        {
            var service = ServiceFactory.CreateGovernmentService(ShouldUseEF(), _connectionString);
            var success = await service.UpdateCategoryPriceAsync(categoryID, dto.NewPrice);

            if (success)
                return Ok(new { success = true, message = "Category price updated" });

            return BadRequest(new { success = false, message = "Failed to update price" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    // DELETE: api/government/categories/{categoryID}
    [HttpDelete("categories/{categoryID}")]
    public async Task<IActionResult> DeleteCategory(int categoryID)
    {
        try
        {
            var service = ServiceFactory.CreateGovernmentService(ShouldUseEF(), _connectionString);
            var success = await service.DeleteCategoryAsync(categoryID);

            if (success)
                return Ok(new { success = true, message = "Category deleted successfully" });

            return BadRequest(new { success = false, message = "Failed to delete category. It has linked user records." });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    // ============================================
    // OPERATOR MANAGEMENT
    // ============================================

    // POST: api/government/operators
    [HttpPost("operators")]
    public async Task<IActionResult> CreateOperator([FromBody] CreateOperatorDto dto)
    {
        try
        {
            var service = ServiceFactory.CreateGovernmentService(ShouldUseEF(), _connectionString);
            var operatorId = await service.CreateOperatorAsync(dto);

            return Ok(new { success = true, operatorId, message = "Operator account created" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    // PUT: api/government/operators/assign
    [HttpPut("operators/{operatorID}/assign")]
    public async Task<IActionResult> AssignOperator(string operatorID, [FromBody] AssignOperatorRequest req)
    {
        try
        {
            var service = ServiceFactory.CreateGovernmentService(ShouldUseEF(), _connectionString);

            var success = await service.AssignOperatorToRouteAsync(operatorID, req.RouteID, req.WarehouseID);

            if (success)
                return Ok(new { success = true, message = "Operator assigned successfully" });

            return BadRequest(new { success = false, message = "Failed to assign operator" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    // PUT: api/government/operators/deactivate/{operatorID}
    [HttpPut("operators/deactivate/{operatorID}")]
    public async Task<IActionResult> DeactivateOperator(string operatorID)
    {
        try
        {
            var service = ServiceFactory.CreateGovernmentService(ShouldUseEF(), _connectionString);
            var success = await service.DeactivateOperatorAsync(operatorID);

            if (success)
                return Ok(new { success = true, message = "Operator deactivated" });

            return BadRequest(new { success = false, message = "Failed to deactivate operator" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    // GET: api/government/operators
    [HttpGet("operators")]
    public async Task<IActionResult> GetAllOperators()
    {
        try
        {
            var service = ServiceFactory.CreateGovernmentService(ShouldUseEF(), _connectionString);
            var result = await service.GetAllOperatorsAsync();
            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    // ============================================
    // COMPLAINTS
    // ============================================

    // GET: api/government/complaints?status=Pending
    [HttpGet("complaints")]
    public async Task<IActionResult> GetAllComplaints([FromQuery] string? status = null)
    {
        try
        {
            var service = ServiceFactory.CreateGovernmentService(ShouldUseEF(), _connectionString);
            var result = await service.GetAllComplaintsAsync(status);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    // PUT: api/government/complaints/status
    [HttpPut("complaints/{complaintID}/status")]
    public async Task<IActionResult> UpdateComplaintStatus(int complaintID, [FromBody] UpdateComplaintStatusRequest req)
    {
        try
        {
            var service = ServiceFactory.CreateGovernmentService(ShouldUseEF(), _connectionString);

            var success = await service.UpdateComplaintStatusAsync(complaintID, req.newStatus);

            if (success)
                return Ok(new { success = true, message = "Complaint status updated" });

            return BadRequest(new { success = false, message = "Failed to update complaint status" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    // ============================================
    // ROUTES & AREAS
    // ============================================

    // GET: api/government/routes
    [HttpGet("routes")]
    public async Task<IActionResult> GetAllRoutes()
    {
        try
        {
            var service = ServiceFactory.CreateGovernmentService(ShouldUseEF(), _connectionString);
            var result = await service.GetAllRoutesAsync();
            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    // GET: api/government/areas
    [HttpGet("areas")]
    public async Task<IActionResult> GetAllAreas()
    {
        try
        {
            var service = ServiceFactory.CreateGovernmentService(ShouldUseEF(), _connectionString);
            var result = await service.GetAllAreasAsync();
            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }
}

// =====================
// REQUEST RECORDS
// =====================

public record AssignOperatorRequest(int RouteID, int WarehouseID);
public record UpdateComplaintStatusRequest(string newStatus);
