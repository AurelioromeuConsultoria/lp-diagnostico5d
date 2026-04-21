using Diagnostico5D.API.DTOs;
using Diagnostico5D.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Diagnostico5D.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController(IUserService userService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll() =>
        Ok(await userService.GetAllAsync());

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateUserRequest req)
    {
        var (success, error) = await userService.CreateAsync(req);
        if (!success) return BadRequest(new { message = error });
        return Ok(new { success = true });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateUserRequest req)
    {
        var (success, error) = await userService.UpdateAsync(id, req);
        if (!success) return BadRequest(new { message = error });
        return Ok(new { success = true });
    }

    [HttpPatch("{id}/senha")]
    public async Task<IActionResult> ChangePassword(int id, [FromBody] AdminChangePasswordRequest req)
    {
        var (success, error) = await userService.AdminChangePasswordAsync(id, req);
        if (!success) return BadRequest(new { message = error });
        return Ok(new { success = true });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var success = await userService.DeleteAsync(id);
        if (!success) return BadRequest(new { message = "Não foi possível excluir. Pelo menos um usuário deve existir." });
        return Ok(new { success = true });
    }
}
