using Diagnostico5D.API.Configuration;
using Diagnostico5D.API.Data;
using Diagnostico5D.API.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ── Database ───────────────────────────────────────────────────────────
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Data Source=/app/data/diagnostico5d.db";

// Garante que o diretório do banco existe (necessário para volume Docker)
var dbPath = connectionString
    .Split(';')
    .FirstOrDefault(p => p.TrimStart().StartsWith("Data Source=", StringComparison.OrdinalIgnoreCase))
    ?.Split('=', 2)[1]
    .Trim();
if (!string.IsNullOrEmpty(dbPath) && Path.IsPathRooted(dbPath))
{
    var dir = Path.GetDirectoryName(dbPath);
    if (!string.IsNullOrEmpty(dir))
    {
        try { Directory.CreateDirectory(dir); }
        catch { /* path não acessível localmente — só necessário no Docker */ }
    }
}

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(connectionString));

// ── Services ───────────────────────────────────────────────────────────
builder.Services.AddScoped<ISubmissionService, SubmissionService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IDevolutivaPdfService, DevolutivaPdfService>();

// ── Evolution API ──────────────────────────────────────────────────────
builder.Services.Configure<EvolutionApiSettings>(
    builder.Configuration.GetSection("EvolutionApi"));
builder.Services.AddHttpClient<IEvolutionApiService, EvolutionApiService>();

// ── JWT Auth ───────────────────────────────────────────────────────────
var jwtKey = builder.Configuration["Jwt:Key"] ?? "diagnostico5d-secret-key-change-in-production";
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = "diagnostico5d",
            ValidAudience = "diagnostico5d",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        };
        // Não rejeitar automaticamente requests com token inválido em rotas sem [Authorize]
        options.Events = new JwtBearerEvents
        {
            OnChallenge = context =>
            {
                // Só retorna 401 se a rota exige autenticação
                if (!context.Response.HasStarted)
                    context.HandleResponse();
                return Task.CompletedTask;
            }
        };
    });

// ── Controllers ────────────────────────────────────────────────────────
builder.Services.AddControllers();

// ── OpenAPI (.NET 10 built-in) ─────────────────────────────────────────
builder.Services.AddOpenApi();

// ── CORS (same as AppIgreja pattern) ───────────────────────────────────
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? [];

builder.Services.AddCors(options =>
{
    options.AddPolicy("DefaultPolicy", policy =>
    {
        if (allowedOrigins.Length > 0)
            policy.WithOrigins(allowedOrigins);
        else
            policy.AllowAnyOrigin();

        policy.AllowAnyHeader().AllowAnyMethod();
    });
});

var app = builder.Build();

// ── Migrations on startup ──────────────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();

    // Adiciona colunas novas em bases existentes (SQLite não suporta migrations automáticas com EnsureCreated)
    var novasColunas = new[]
    {
        "ALTER TABLE Submissions ADD COLUMN ConcluidoEm TEXT NULL",
        "ALTER TABLE Submissions ADD COLUMN WhatsappEnviado INTEGER NOT NULL DEFAULT 0",
        "ALTER TABLE Submissions ADD COLUMN WhatsappEnviadoEm TEXT NULL",
        "ALTER TABLE Submissions ADD COLUMN MentorRevisado INTEGER NOT NULL DEFAULT 0",
        "ALTER TABLE Submissions ADD COLUMN MentorObservacao TEXT NULL",
        "ALTER TABLE Submissions ADD COLUMN Fase TEXT NOT NULL DEFAULT 'novo'",
        // Renomear colunas do Bloco 6 para os novos títulos das dimensões
        "ALTER TABLE Submissions RENAME COLUMN B6IdentidadeStatus TO B6GovFinanceiroStatus",
        "ALTER TABLE Submissions RENAME COLUMN B6IdentidadeQuebra TO B6GovFinanceiroQuebra",
        "ALTER TABLE Submissions RENAME COLUMN B6GovernoStatus TO B6IdentidadeAutoStatus",
        "ALTER TABLE Submissions RENAME COLUMN B6GovernoQuebra TO B6IdentidadeAutoQuebra",
        "ALTER TABLE Submissions RENAME COLUMN B6PreparacaoStatus TO B6GovInteriorStatus",
        "ALTER TABLE Submissions RENAME COLUMN B6PreparacaoQuebra TO B6GovInteriorQuebra",
        "ALTER TABLE Submissions RENAME COLUMN B6FeAcaoStatus TO B6AmbienteStatus",
        "ALTER TABLE Submissions RENAME COLUMN B6FeAcaoQuebra TO B6AmbienteQuebra",
        "ALTER TABLE Submissions RENAME COLUMN B6ProsperidadeStatus TO B6EspiritualidadeStatus",
        "ALTER TABLE Submissions RENAME COLUMN B6ProsperidadeQuebra TO B6EspiritualidadeQuebra",
        "ALTER TABLE Submissions RENAME COLUMN B6Gargalo TO B6SinteseGeral",
        "ALTER TABLE Submissions ADD COLUMN DevolutivaPdfPath TEXT NULL",
        // Tabela de usuários
        @"CREATE TABLE IF NOT EXISTS Users (
            Id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
            Nome TEXT NOT NULL,
            Email TEXT NOT NULL,
            SenhaHash TEXT NOT NULL,
            Ativo INTEGER NOT NULL DEFAULT 1,
            CriadoEm TEXT NOT NULL DEFAULT (datetime('now','localtime'))
        )",
    };
    foreach (var sql in novasColunas)
    {
        try { db.Database.ExecuteSqlRaw(sql); } catch { /* coluna/tabela já existe */ }
    }

    // Seed: se não há usuários, cria o admin inicial a partir das configurações
    if (!db.Users.Any())
    {
        var userService = scope.ServiceProvider.GetRequiredService<IUserService>();
        var adminEmail  = app.Configuration["Admin:Email"] ?? "admin@diagnostico5d.com";
        var adminSenha  = app.Configuration["Admin:Senha"] ?? "mudar123";
        await userService.CreateAsync(new Diagnostico5D.API.DTOs.CreateUserRequest("Admin", adminEmail, adminSenha));
    }
}

// ── Middleware pipeline ────────────────────────────────────────────────
app.MapOpenApi();

app.UseCors("DefaultPolicy");

app.UseAuthentication();

// Serve static files (index.html, diagnostico.html) from wwwroot
app.UseDefaultFiles();
app.UseStaticFiles();

app.UseAuthorization();
app.MapControllers();

// SPA fallback for admin React app at /admin (scoped to /admin/* only)
app.MapFallbackToFile("/admin/{**path}", "admin/index.html");

app.Run();
