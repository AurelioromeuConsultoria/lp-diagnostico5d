using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Diagnostico5D.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(IConfiguration configuration) : ControllerBase
{
    [HttpPost("login")]
    public IActionResult Login([FromBody] LoginRequest request)
    {
        var adminEmail = configuration["Admin:Email"] ?? "admin@diagnostico5d.com";
        var adminSenha = configuration["Admin:Senha"] ?? "admin123";

        if (request.Email != adminEmail || request.Senha != adminSenha)
            return Unauthorized(new { message = "Email ou senha inválidos" });

        var jwtKey = configuration["Jwt:Key"] ?? "diagnostico5d-secret-key-change-in-production";
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.Email, adminEmail),
            new Claim(ClaimTypes.Name, "Admin"),
            new Claim(ClaimTypes.Role, "admin"),
        };

        var token = new JwtSecurityToken(
            issuer: "diagnostico5d",
            audience: "diagnostico5d",
            claims: claims,
            expires: DateTime.UtcNow.AddDays(7),
            signingCredentials: creds
        );

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

        return Ok(new
        {
            token = tokenString,
            usuario = new { nome = "Admin", email = adminEmail }
        });
    }

    [HttpGet("me")]
    public IActionResult Me()
    {
        var email = User.FindFirst(ClaimTypes.Email)?.Value;
        var nome = User.FindFirst(ClaimTypes.Name)?.Value;

        if (email is null) return Unauthorized();

        return Ok(new { nome, email });
    }
}

public record LoginRequest(string Email, string Senha);
