using Microsoft.AspNetCore.Mvc;
using StockFlowPro.Application.Services;

namespace StockFlowPro.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AuthService _authService;

    public AuthController(AuthService authService)
    {
        _authService = authService;
    }

    public record RegisterRequest(string TenantName, string Email, string Password, string FullName);

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        try
        {
            // Capture hardware MAC Address locally securely
            string? macAddress = GetLocalMacAddress();

            // Do not return token anymore, tenant is inactive by default.
            var result = await _authService.RegisterAsync(request.TenantName, request.Email, request.Password, request.FullName, macAddress);
            return Ok(new
            {
                Message = "Compte créé avec succès. En attente de validation."
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
    }

    public record LoginRequest(string Email, string Password);

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        try
        {
            // Capture hardware MAC Address locally securely
            string? macAddress = GetLocalMacAddress();
            
            var result = await _authService.LoginAsync(request.Email, request.Password, macAddress);
            
            if (result == null)
                return Unauthorized(new { code = "INVALID_CREDENTIALS", Message = "Identifiants invalides." });

            return Ok(new
            {
                Token = result.Value.Token,
                User = new { result.Value.User.Id, result.Value.User.Email, result.Value.User.FullName, result.Value.User.Role }
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { code = ex.Message, message = "Accès refusé" });
        }
    }

    private string? GetLocalMacAddress()
    {
        try
        {
            return System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces()
                .Where(nic => nic.OperationalStatus == System.Net.NetworkInformation.OperationalStatus.Up && 
                              nic.NetworkInterfaceType != System.Net.NetworkInformation.NetworkInterfaceType.Loopback)
                .Select(nic => nic.GetPhysicalAddress().ToString())
                .FirstOrDefault(mac => !string.IsNullOrEmpty(mac));
        }
        catch
        {
            return null; // Fallback smoothly if unable to read (e.g cloud hosted API bypassing local check)
        }
    }
}
