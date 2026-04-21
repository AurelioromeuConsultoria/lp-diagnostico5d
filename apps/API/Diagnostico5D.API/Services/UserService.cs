using Diagnostico5D.API.Data;
using Diagnostico5D.API.DTOs;
using Diagnostico5D.API.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Diagnostico5D.API.Services;

public interface IUserService
{
    Task<IEnumerable<UserDto>> GetAllAsync();
    Task<UserDto?> GetByIdAsync(int id);
    Task<(bool Success, string? Error)> CreateAsync(CreateUserRequest req);
    Task<(bool Success, string? Error)> UpdateAsync(int id, UpdateUserRequest req);
    Task<(bool Success, string? Error)> ChangePasswordAsync(int id, ChangePasswordRequest req);
    Task<(bool Success, string? Error)> AdminChangePasswordAsync(int id, AdminChangePasswordRequest req);
    Task<bool> DeleteAsync(int id);
    Task<User?> AuthenticateAsync(string email, string senha);
}

public class UserService(AppDbContext db) : IUserService
{
    private readonly PasswordHasher<User> _hasher = new();

    public async Task<IEnumerable<UserDto>> GetAllAsync()
    {
        var users = await db.Users.OrderBy(u => u.Nome).ToListAsync();
        return users.Select(ToDto);
    }

    public async Task<UserDto?> GetByIdAsync(int id)
    {
        var user = await db.Users.FindAsync(id);
        return user is null ? null : ToDto(user);
    }

    public async Task<(bool Success, string? Error)> CreateAsync(CreateUserRequest req)
    {
        if (await db.Users.AnyAsync(u => u.Email == req.Email.Trim().ToLower()))
            return (false, "Email já cadastrado.");

        var user = new User
        {
            Nome     = req.Nome.Trim(),
            Email    = req.Email.Trim().ToLower(),
            Ativo    = true,
            CriadoEm = DateTime.Now,
        };
        user.SenhaHash = _hasher.HashPassword(user, req.Senha);

        db.Users.Add(user);
        await db.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> UpdateAsync(int id, UpdateUserRequest req)
    {
        var user = await db.Users.FindAsync(id);
        if (user is null) return (false, "Usuário não encontrado.");

        var emailTaken = await db.Users
            .AnyAsync(u => u.Email == req.Email.Trim().ToLower() && u.Id != id);
        if (emailTaken) return (false, "Email já cadastrado por outro usuário.");

        user.Nome  = req.Nome.Trim();
        user.Email = req.Email.Trim().ToLower();
        user.Ativo = req.Ativo;

        await db.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> ChangePasswordAsync(int id, ChangePasswordRequest req)
    {
        var user = await db.Users.FindAsync(id);
        if (user is null) return (false, "Usuário não encontrado.");

        var result = _hasher.VerifyHashedPassword(user, user.SenhaHash, req.SenhaAtual);
        if (result == PasswordVerificationResult.Failed)
            return (false, "Senha atual incorreta.");

        user.SenhaHash = _hasher.HashPassword(user, req.NovaSenha);
        await db.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> AdminChangePasswordAsync(int id, AdminChangePasswordRequest req)
    {
        var user = await db.Users.FindAsync(id);
        if (user is null) return (false, "Usuário não encontrado.");

        user.SenhaHash = _hasher.HashPassword(user, req.NovaSenha);
        await db.SaveChangesAsync();
        return (true, null);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var user = await db.Users.FindAsync(id);
        if (user is null) return false;

        if (await db.Users.CountAsync() <= 1)
            return false; // não permite deletar o último usuário

        db.Users.Remove(user);
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<User?> AuthenticateAsync(string email, string senha)
    {
        var user = await db.Users
            .FirstOrDefaultAsync(u => u.Email == email.Trim().ToLower() && u.Ativo);
        if (user is null) return null;

        var result = _hasher.VerifyHashedPassword(user, user.SenhaHash, senha);
        return result == PasswordVerificationResult.Failed ? null : user;
    }

    private static UserDto ToDto(User u) => new(u.Id, u.Nome, u.Email, u.Ativo, u.CriadoEm);
}
