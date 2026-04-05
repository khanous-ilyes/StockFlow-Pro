using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockFlowPro.Application.Interfaces;
using System;
using System.Threading.Tasks;

namespace StockFlowPro.API.Controllers;

public class UpdateProfileDto
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

public class ChangePasswordDto
{
    public string CurrentPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class ProfileController : ControllerBase
{
    private readonly IAppDbContext _context;

    public ProfileController(IAppDbContext context)
    {
        _context = context;
    }

    private Guid GetCurrentUserId()
    {
        var claim = User.FindFirst("sub") ?? User.FindFirst("userId") ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        return Guid.TryParse(claim?.Value, out var id) ? id : Guid.Empty;
    }

    [HttpGet]
    public async Task<IActionResult> GetProfile()
    {
        var userId = GetCurrentUserId();
        var user = await _context.Users.FindAsync(userId);
        if (user == null) return NotFound();

        return Ok(new
        {
            id = user.Id,
            fullName = user.FullName,
            email = user.Email,
            role = user.Role.ToString(),
            isActive = user.IsActive
        });
    }

    [HttpPut]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto dto)
    {
        var userId = GetCurrentUserId();
        var user = await _context.Users.FindAsync(userId);
        if (user == null) return NotFound();

        // Check if email is already taken by another user
        var emailTaken = await _context.Users.AnyAsync(u => u.Email == dto.Email && u.Id != userId);
        if (emailTaken)
            return BadRequest(new { message = "Cet email est déjà utilisé par un autre compte." });

        user.FullName = dto.FullName;
        user.Email = dto.Email;
        await _context.SaveChangesAsync();

        return Ok(new { message = "Profil mis à jour avec succès." });
    }

    [HttpPut("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
    {
        var userId = GetCurrentUserId();
        var user = await _context.Users.FindAsync(userId);
        if (user == null) return NotFound();

        if (!BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, user.PasswordHash))
            return BadRequest(new { message = "Mot de passe actuel incorrect." });

        if (dto.NewPassword.Length < 6)
            return BadRequest(new { message = "Le nouveau mot de passe doit contenir au moins 6 caractères." });

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Mot de passe modifié avec succès." });
    }
}
