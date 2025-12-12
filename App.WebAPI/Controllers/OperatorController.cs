using Microsoft.AspNetCore.Mvc;
using App.Core;
using App.Factory;

namespace App.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OperatorController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly string _connectionString;
    private readonly bool _useEf;

    public OperatorController(IConfiguration configuration)
    {
        _configuration = configuration;
        _connectionString = _configuration.GetConnectionString("SmartWasteDB")!;
        _useEf = _configuration.GetValue<bool>("AppSettings:UseEntityFramework");
    }

    private bool ShouldUseEF()
    {
        // Allow frontend to force mode via Header "X-Use-EF: true/false"
        if (Request.Headers.TryGetValue("X-Use-EF", out var headerValue))
        {
            Console.WriteLine($"[OperatorController] X-Use-EF header found: {headerValue} -> Using {(headerValue.ToString().ToLower() == "true" ? "EF" : "SP")}");
            return headerValue.ToString().ToLower() == "true";
        }
        Console.WriteLine($"[OperatorController] No X-Use-EF header, using config default: {(_useEf ? "EF" : "SP")}");
        return _useEf;
    }

    // GET: api/operator/details/{operatorID}
    [HttpGet("details/{operatorID}")]
    public async Task<IActionResult> GetDetails(string operatorID)
    {
        try
        {
            var service = ServiceFactory.CreateOperatorService(ShouldUseEF(), _connectionString);
            var details = await service.GetOperatorDetailsAsync(operatorID);
            
            if (details == null) return NotFound(new { message = "Operator not found" });
            return Ok(details);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    // GET: api/operator/collections/{operatorID}
    [HttpGet("collections/{operatorID}")]
    public async Task<IActionResult> GetMyCollectionPoints(string operatorID)
    {
        try
        {
            var service = ServiceFactory.CreateOperatorService(ShouldUseEF(), _connectionString);
            var points = await service.GetMyCollectionPointsAsync(operatorID);
            return Ok(points);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    // POST: api/operator/collect
    [HttpPost("collect")]
    public async Task<IActionResult> CollectWaste([FromBody] CollectionDto dto)
    {
        try
        {
            Console.WriteLine($"[OperatorController] CollectWaste - OperatorID: '{dto.OperatorID}', ListingID: {dto.ListingID}, Weight: {dto.CollectedWeight}, WarehouseID: {dto.WarehouseID}");
            var service = ServiceFactory.CreateOperatorService(ShouldUseEF(), _connectionString);
            var result = await service.CollectWasteAsync(dto);
            
            Console.WriteLine($"[OperatorController] CollectWaste - Success");
            Console.WriteLine($"[OperatorController]   CollectionID: {result.CollectionID}");
            if (result.TransactionID.HasValue)
            {
                Console.WriteLine($"[OperatorController]   TransactionID: {result.TransactionID}");
                Console.WriteLine($"[OperatorController]   Payment: Rs.{result.PaymentAmount}");
                Console.WriteLine($"[OperatorController]   Code: {result.VerificationCode}");
            }
            
            return Ok(result);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[OperatorController] CollectWaste ERROR: {ex.Message}");
            Console.WriteLine($"[OperatorController] CollectWaste STACK: {ex.StackTrace}");
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    // POST: api/operator/deposit
    [HttpPost("deposit")]
    public async Task<IActionResult> DepositWaste([FromBody] WarehouseDepositDto dto)
    {
        try
        {
            Console.WriteLine($"[OperatorController] DepositWaste - WarehouseID: {dto.WarehouseID}, CategoryID: {dto.CategoryID}, Quantity: {dto.Quantity}");
            var service = ServiceFactory.CreateOperatorService(ShouldUseEF(), _connectionString);
            var success = await service.DepositWasteAsync(dto);

            Console.WriteLine($"[OperatorController] DepositWaste - Result: {(success ? "SUCCESS" : "FAILED")}");
            if (success) return Ok(new { success = true, message = "Waste deposited at warehouse" });
            return BadRequest(new { success = false, message = "Deposit failed" });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[OperatorController] DepositWaste - EXCEPTION: {ex.Message}");
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    // GET: api/operator/history/{operatorID}
    [HttpGet("history/{operatorID}")]
    public async Task<IActionResult> GetHistory(string operatorID)
    {
        try
        {
            Console.WriteLine($"[OperatorController] GetHistory - OperatorID: '{operatorID}'");
            var service = ServiceFactory.CreateOperatorService(ShouldUseEF(), _connectionString);
            var history = await service.GetMyCollectionHistoryAsync(operatorID);
            Console.WriteLine($"[OperatorController] GetHistory - Retrieved {history.Count} records");
            return Ok(history);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[OperatorController] GetHistory ERROR: {ex.Message}");
            Console.WriteLine($"[OperatorController] GetHistory STACK: {ex.StackTrace}");
            return StatusCode(500, new { message = ex.Message });
        }
    }

    // GET: api/operator/performance/{operatorID}
    [HttpGet("performance/{operatorID}")]
    public async Task<IActionResult> GetPerformance(string operatorID)
    {
        try
        {
            var service = ServiceFactory.CreateOperatorService(ShouldUseEF(), _connectionString);
            var performance = await service.GetMyPerformanceAsync(operatorID);
            return Ok(performance);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    // GET: api/operator/complaints/{operatorID}
    [HttpGet("complaints/{operatorID}")]
    public async Task<IActionResult> GetMyComplaints(string operatorID)
    {
        try
        {
            var service = ServiceFactory.CreateOperatorService(ShouldUseEF(), _connectionString);
            var complaints = await service.GetMyComplaintsAsync(operatorID);
            return Ok(complaints);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    // PUT: api/operator/complaint/status
    [HttpPut("complaint/status")]
    public async Task<IActionResult> UpdateComplaintStatus([FromBody] UpdateComplaintDto dto)
    {
        try
        {
            var service = ServiceFactory.CreateOperatorService(ShouldUseEF(), _connectionString);
            var success = await service.UpdateComplaintStatusAsync(dto.ComplaintID, dto.Status);
            
            if (success) return Ok(new { success = true, message = "Complaint status updated" });
            return NotFound(new { success = false, message = "Complaint not found" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }
}
