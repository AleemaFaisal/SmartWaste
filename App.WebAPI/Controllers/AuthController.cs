using Microsoft.AspNetCore.Mvc;
using App.Core;
using App.Factory;

namespace App.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IConfiguration _configuration;

    public AuthController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    /// <summary>
    /// Determines whether to use Entity Framework based on request header or default config
    /// </summary>
    private bool ShouldUseEF()
    {
        // Check if there's a custom header to override the default
        if (Request.Headers.TryGetValue("X-Use-EF", out var headerValue))
        {
            return headerValue.ToString().ToLower() == "true";
        }

        // Fall back to configuration default
        return _configuration.GetValue<bool>("AppSettings:UseEntityFramework");
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        try
        {
            var connectionString = _configuration.GetConnectionString("SmartWasteDB");
            var useEf = ShouldUseEF();

            var authService = ServiceFactory.CreateAuthService(useEf, connectionString!);
            var result = await authService.LoginAsync(request.CNIC, request.Password);

            if (result.Success)
            {
                return Ok(new
                {
                    success = true,
                    user = new
                    {
                        userID = result.UserID,
                        roleID = result.RoleID,
                        roleName = result.RoleName
                    },
                    message = result.Message
                });
            }

            return Unauthorized(new
            {
                success = false,
                message = result.Message ?? "Invalid credentials"
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                success = false,
                message = $"Login error: {ex.Message}"
            });
        }
    }
}

public record LoginRequest(string CNIC, string Password);
