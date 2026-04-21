using Diagnostico5D.API.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Diagnostico5D.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(IUserService userService, IConfiguration configuration) : ControllerBase
{
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var user = await userService.AuthenticateAsync(request.Email, request.Senha);
        if (user is null)
            return Unauthorized(new { message = "Email ou senha inválidos" });

        var jwtKey = configuration["Jwt:Key"] ?? "diagnostico5d-secret-key-change-in-production";
        var key    = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var creds  = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Name, user.Nome),
            new Claim(ClaimTypes.Role, "admin"),
        };

        var token = new JwtSecurityToken(
            issuer:            "diagnostico5d",
            audience:          "diagnostico5d",
            claims:            claims,
            expires:           DateTime.UtcNow.AddDays(7),
            signingCredentials: creds
        );

        return Ok(new
        {
            token   = new JwtSecurityTokenHandler().WriteToken(token),
            usuario = new { nome = user.Nome, email = user.Email }
        });
    }

    [HttpGet("me")]
    public IActionResult Me()
    {
        var email = User.FindFirst(ClaimTypes.Email)?.Value;
        var nome  = User.FindFirst(ClaimTypes.Name)?.Value;
        if (email is null) return Unauthorized();
        return Ok(new { nome, email });
    }
}

public record LoginRequest(string Email, string Senha);
