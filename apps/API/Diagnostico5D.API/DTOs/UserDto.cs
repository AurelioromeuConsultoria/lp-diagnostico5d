namespace Diagnostico5D.API.DTOs;

public record UserDto(int Id, string Nome, string Email, bool Ativo, DateTime CriadoEm);

public record CreateUserRequest(string Nome, string Email, string Senha);

public record UpdateUserRequest(string Nome, string Email, bool Ativo);

public record ChangePasswordRequest(string SenhaAtual, string NovaSenha);

public record AdminChangePasswordRequest(string NovaSenha);
