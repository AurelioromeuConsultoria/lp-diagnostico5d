using Diagnostico5D.API.Configuration;
using Diagnostico5D.API.Data;
using Diagnostico5D.API.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
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
var logger = app.Logger;

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

// Serve static files BEFORE authentication (correct ASP.NET Core order)
// Cache-Control: no-cache for HTML, immutable for hashed assets
static void ApplyStaticCacheHeaders(StaticFileResponseContext ctx)
{
    var headers = ctx.Context.Response.Headers;
    var ext = Path.GetExtension(ctx.File.Name).ToLowerInvariant();
    // HTML: never cache (so new deployments always fetch fresh asset URLs)
    if (ext == ".html")
        headers.CacheControl = "no-cache, no-store, must-revalidate";
    // Hashed assets (JS/CSS): immutable forever (filename changes on content change)
    else if (ext is ".js" or ".css" or ".woff" or ".woff2" or ".ttf" or ".ico" or ".png" or ".svg" or ".webp")
        headers.CacheControl = "public, max-age=31536000, immutable";
}

app.UseDefaultFiles();
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ApplyStaticCacheHeaders
});

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Startup diagnostics for the React admin bundle in production.
var webRootPath = app.Environment.WebRootPath ?? Path.Combine(app.Environment.ContentRootPath, "wwwroot");
var adminRootPath = Path.Combine(webRootPath, "admin");
var adminAssetsPath = Path.Combine(adminRootPath, "assets");
var contentTypeProvider = new FileExtensionContentTypeProvider();
logger.LogInformation("Admin web root: {AdminRootPath} | exists={AdminRootExists}", adminRootPath, Directory.Exists(adminRootPath));
logger.LogInformation("Admin assets path: {AdminAssetsPath} | exists={AdminAssetsExists}", adminAssetsPath, Directory.Exists(adminAssetsPath));
if (Directory.Exists(adminAssetsPath))
{
    var adminAssetFiles = Directory.GetFiles(adminAssetsPath)
        .Select(Path.GetFileName)
        .OrderBy(name => name)
        .ToArray();
    var adminAssetSample = adminAssetFiles.Take(20).ToArray();
    logger.LogInformation("Admin assets count={AdminAssetsCount} sample=[{AdminAssetsSample}]",
        adminAssetFiles.Length,
        string.Join(", ", adminAssetSample));
}

// Explicit /admin static mapping. This avoids the SPA fallback swallowing asset requests
// when the admin bundle is published under wwwroot/admin inside the container.
if (Directory.Exists(adminRootPath))
{
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(adminRootPath),
        RequestPath = "/admin",
        OnPrepareResponse = ApplyStaticCacheHeaders
    });
}

static bool TryResolveAdminFile(string rootPath, string relativePath, out string fullPath)
{
    fullPath = Path.GetFullPath(Path.Combine(rootPath, relativePath.Replace('/', Path.DirectorySeparatorChar)));
    var normalizedRoot = Path.GetFullPath(rootPath) + Path.DirectorySeparatorChar;
    return fullPath.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase) && File.Exists(fullPath);
}

IResult ServeAdminFile(string fullPath)
{
    if (!contentTypeProvider.TryGetContentType(fullPath, out var contentType))
        contentType = "application/octet-stream";

    return TypedResults.PhysicalFile(fullPath, contentType, enableRangeProcessing: false, lastModified: File.GetLastWriteTimeUtc(fullPath));
}

app.MapGet("/admin/assets/{**assetPath}", (string assetPath) =>
{
    if (string.IsNullOrWhiteSpace(assetPath) || !TryResolveAdminFile(adminAssetsPath, assetPath, out var fullPath))
        return Results.NotFound();

    return ServeAdminFile(fullPath);
});

app.MapGet("/admin/{fileName}", (string fileName) =>
{
    if (string.IsNullOrWhiteSpace(fileName) || fileName.Contains('/') || fileName.Contains('\\'))
        return Results.NotFound();

    if (!Path.HasExtension(fileName) || !TryResolveAdminFile(adminRootPath, fileName, out var fullPath))
        return Results.NotFound();

    return ServeAdminFile(fullPath);
});

// SPA fallback for admin React app — only for route paths, not asset files (.js/.css/etc.)
app.MapFallback("/admin/{**path}", async (HttpContext ctx) =>
{
    if (Path.HasExtension(ctx.Request.Path.Value))
    {
        ctx.Response.StatusCode = 404;
        return;
    }
    var env = ctx.RequestServices.GetRequiredService<IWebHostEnvironment>();
    var indexPath = Path.Combine(env.WebRootPath ?? "wwwroot", "admin", "index.html");
    if (!File.Exists(indexPath))
    {
        ctx.Response.StatusCode = 503;
        await ctx.Response.WriteAsync("Admin not deployed");
        return;
    }
    ctx.Response.ContentType = "text/html; charset=utf-8";
    await ctx.Response.SendFileAsync(indexPath);
});

app.Run();
