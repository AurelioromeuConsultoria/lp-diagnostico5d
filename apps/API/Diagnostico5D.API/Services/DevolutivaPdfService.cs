using System.Net;
using Diagnostico5D.API.Models;
using PuppeteerSharp;
using PuppeteerSharp.Media;

namespace Diagnostico5D.API.Services;

public interface IDevolutivaPdfService
{
    Task<string> GerarAsync(Submission submission, CancellationToken ct = default);
}

public class DevolutivaPdfService(IConfiguration configuration, ILogger<DevolutivaPdfService> logger)
    : IDevolutivaPdfService
{
    private readonly string _pdfDir = ResolvePdfDir(configuration);

    private static string ResolvePdfDir(IConfiguration cfg)
    {
        var conn = cfg.GetConnectionString("DefaultConnection") ?? "";
        var dbPath = conn.Split(';')
            .FirstOrDefault(p => p.TrimStart().StartsWith("Data Source=", StringComparison.OrdinalIgnoreCase))
            ?.Split('=', 2)[1].Trim()
            ?? "/app/data/diagnostico5d.db";
        return Path.Combine(Path.GetDirectoryName(dbPath) ?? "/app/data", "pdfs");
    }

    public async Task<string> GerarAsync(Submission s, CancellationToken ct = default)
    {
        Directory.CreateDirectory(_pdfDir);
        var filePath = Path.Combine(_pdfDir, $"devolutiva-{s.Id}.pdf");
        var html = BuildHtml(s);

        var chromiumPath = Environment.GetEnvironmentVariable("CHROMIUM_PATH");
        string[] sandboxArgs = ["--no-sandbox", "--disable-setuid-sandbox", "--disable-dev-shm-usage", "--disable-gpu"];

        LaunchOptions launchOpts;
        if (!string.IsNullOrEmpty(chromiumPath) && File.Exists(chromiumPath))
        {
            launchOpts = new LaunchOptions { Headless = true, ExecutablePath = chromiumPath, Args = sandboxArgs };
        }
        else
        {
            logger.LogInformation("CHROMIUM_PATH não definido — baixando Chromium para desenvolvimento");
            await new BrowserFetcher().DownloadAsync();
            launchOpts = new LaunchOptions { Headless = true, Args = sandboxArgs };
        }

        await using var browser = await Puppeteer.LaunchAsync(launchOpts);
        await using var page = await browser.NewPageAsync();

        await page.SetContentAsync(html, new NavigationOptions
        {
            WaitUntil = [WaitUntilNavigation.Networkidle2],
            Timeout = 30_000
        });

        await page.PdfAsync(filePath, new PdfOptions
        {
            Format = PaperFormat.A4,
            PrintBackground = true,
            MarginOptions = new MarginOptions { Top = "0", Bottom = "0", Left = "0", Right = "0" }
        });

        logger.LogInformation("PDF gerado em {Path}", filePath);
        return filePath;
    }

    // ── Template ──────────────────────────────────────────────────────────────

    private static string Esc(string? s) => WebUtility.HtmlEncode(s ?? "");

    private static string Nl2p(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return "";
        return string.Join("", s.Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(p => $"<p>{Esc(p.Trim())}</p>"));
    }

    // CSS separado para evitar conflito de chaves com interpolação C#
    private const string Css = """
        *{box-sizing:border-box;margin:0;padding:0}
        :root{
          --dark:#180E06;--dark-2:#251507;--orange:#C94B00;
          --warm:#F5EDE0;--off:#FAFAF7;--text-2:#6B5040;
          --text-3:#A08878;--sand:#E8D5BA;
        }
        body{font-family:'DM Sans',sans-serif;background:#fff;color:var(--dark);
          font-size:14px;line-height:1.75;-webkit-print-color-adjust:exact;print-color-adjust:exact;}
        .page{max-width:700px;margin:0 auto;padding:0 0 60px}
        .capa{background:var(--dark);padding:56px 48px 48px;position:relative;overflow:hidden;margin-bottom:52px;}
        .capa::after{content:'';position:absolute;bottom:0;left:0;right:0;height:3px;
          background:linear-gradient(90deg,var(--orange),#E8621A,transparent);}
        .capa-eyebrow{font-family:'Space Grotesk',sans-serif;font-size:10px;font-weight:600;
          letter-spacing:.22em;text-transform:uppercase;color:var(--text-3);margin-bottom:20px;}
        .capa-title{font-family:'Playfair Display',serif;font-size:38px;font-weight:900;
          color:#fff;line-height:1.1;letter-spacing:-.01em;margin-bottom:6px;}
        .capa-subtitle{font-family:'Playfair Display',serif;font-size:16px;font-weight:400;
          font-style:italic;color:var(--sand);margin-bottom:32px;}
        .capa-meta{display:flex;align-items:center;gap:16px;border-top:1px solid rgba(255,255,255,.1);padding-top:20px;margin-top:4px;}
        .capa-avatar{width:44px;height:44px;border-radius:50%;background:var(--orange);
          display:flex;align-items:center;justify-content:center;font-family:'Playfair Display',serif;
          font-size:18px;font-weight:700;color:#fff;flex-shrink:0;}
        .capa-nome{font-family:'DM Sans',sans-serif;font-size:15px;font-weight:500;color:#fff;line-height:1.3;}
        .capa-brand{font-size:11px;color:var(--text-3);margin-top:2px;}
        .intro{padding:0 48px;margin-bottom:48px;}
        .intro-text{font-size:14.5px;font-style:italic;color:var(--text-2);line-height:1.9;
          border-left:3px solid var(--orange);padding-left:20px;}
        .secao{padding:0 48px;margin-bottom:48px}
        .secao-kicker{display:flex;align-items:center;gap:10px;margin-bottom:10px;}
        .secao-num{font-family:'Space Grotesk',sans-serif;font-size:11px;font-weight:700;
          letter-spacing:.14em;color:var(--orange);}
        .secao-badge{display:inline-flex;align-items:center;gap:6px;font-family:'Space Grotesk',sans-serif;
          font-size:10px;font-weight:600;letter-spacing:.1em;text-transform:uppercase;color:var(--text-3);}
        .dot{width:7px;height:7px;border-radius:50%;display:inline-block;flex-shrink:0}
        .secao-titulo{font-family:'Playfair Display',serif;font-size:22px;font-weight:700;
          color:var(--dark);line-height:1.2;margin-bottom:16px;}
        .secao-corpo p{color:var(--text-2);margin-bottom:12px;line-height:1.85;text-align:justify;}
        .secao-rule{height:1px;background:linear-gradient(90deg,rgba(201,75,0,.25),transparent);margin-top:36px;}
        .sintese{margin:52px 48px;background:var(--dark);border-radius:4px;padding:40px 44px;
          position:relative;overflow:hidden;}
        .sintese::before{content:'';position:absolute;top:0;left:0;bottom:0;width:4px;background:var(--orange);}
        .sintese-eyebrow{font-family:'Space Grotesk',sans-serif;font-size:10px;font-weight:600;
          letter-spacing:.22em;text-transform:uppercase;color:var(--orange);margin-bottom:12px;}
        .sintese-titulo{font-family:'Playfair Display',serif;font-size:20px;font-weight:700;
          font-style:italic;color:#fff;margin-bottom:20px;line-height:1.35;}
        .sintese-corpo p{color:rgba(255,255,255,.72);margin-bottom:12px;line-height:1.85;
          text-align:justify;font-size:13.5px;}
        .passos{padding:0 48px;margin-top:52px}
        .passos-eyebrow{font-family:'Space Grotesk',sans-serif;font-size:10px;font-weight:600;
          letter-spacing:.22em;text-transform:uppercase;color:var(--orange);margin-bottom:8px;}
        .passos-titulo{font-family:'Playfair Display',serif;font-size:26px;font-weight:700;
          color:var(--dark);margin-bottom:4px;}
        .passos-rule{height:2px;background:linear-gradient(90deg,var(--orange),rgba(201,75,0,.12));margin:20px 0 36px;}
        .passo{display:flex;gap:24px;margin-bottom:32px;padding-bottom:32px;border-bottom:1px solid rgba(24,14,6,.07);}
        .passo:last-child{border-bottom:none;padding-bottom:0}
        .passo-left{flex-shrink:0;padding-top:3px}
        .passo-num{display:flex;align-items:center;justify-content:center;width:40px;height:40px;
          background:var(--dark);border-radius:2px;font-family:'Playfair Display',serif;
          font-size:15px;font-weight:700;color:var(--orange);}
        .passo-right{flex:1}
        .passo-titulo{font-family:'DM Sans',sans-serif;font-size:14.5px;font-weight:700;
          color:var(--dark);margin-bottom:8px;line-height:1.3;}
        .passo-corpo p{color:var(--text-2);line-height:1.85;font-size:13.5px;text-align:justify;margin-bottom:6px;}
        .rodape{margin:56px 48px 0;padding-top:20px;border-top:1px solid rgba(24,14,6,.1);
          display:flex;align-items:center;justify-content:space-between;}
        .rodape-brand{font-family:'Playfair Display',serif;font-size:13px;font-weight:700;color:var(--dark);}
        .rodape-brand span{color:var(--orange)}
        .rodape-info{font-size:10.5px;color:var(--text-3);text-align:right;line-height:1.7;}
        """;

    private static string BuildHtml(Submission s)
    {
        var primeiroNome = (s.Nome ?? "").Split(' ')[0];
        var inicial = primeiroNome.Length > 0 ? primeiroNome[0].ToString().ToUpper() : "?";
        var dataGeracao = DateTime.Now.ToString("dd/MM/yyyy");

        var areas = new[]
        {
            (Num: "01", Titulo: "GOVERNO FINANCEIRO",            Status: s.B6GovFinanceiroStatus,   Quebra: s.B6GovFinanceiroQuebra),
            (Num: "02", Titulo: "IDENTIDADE E AUTOCONCEITO",     Status: s.B6IdentidadeAutoStatus,  Quebra: s.B6IdentidadeAutoQuebra),
            (Num: "03", Titulo: "GOVERNO INTERIOR E CONSTÂNCIA", Status: s.B6GovInteriorStatus,     Quebra: s.B6GovInteriorQuebra),
            (Num: "04", Titulo: "AMBIENTE E ALIANÇAS",           Status: s.B6AmbienteStatus,        Quebra: s.B6AmbienteQuebra),
            (Num: "05", Titulo: "ESPIRITUALIDADE E DIREÇÃO",     Status: s.B6EspiritualidadeStatus, Quebra: s.B6EspiritualidadeQuebra),
        };

        var statusInfo = new Dictionary<string, (string Label, string Dot)>
        {
            ["ok"]      = ("Em bom estado",      "#16a34a"),
            ["atencao"] = ("Requer atenção",      "#d97706"),
            ["critico"] = ("Quebra identificada", "#dc2626"),
        };

        var secoesHtml = string.Join("", areas.Select(a =>
        {
            var quebra = (a.Quebra ?? "").Trim();
            if (string.IsNullOrEmpty(a.Status) && string.IsNullOrEmpty(quebra)) return "";
            var badgeHtml = a.Status != null && statusInfo.TryGetValue(a.Status, out var b)
                ? $"<span class=\"secao-badge\"><span class=\"dot\" style=\"background:{b.Dot}\"></span>{Esc(b.Label)}</span>"
                : "";
            var corpoHtml = quebra.Length > 0 ? $"<div class=\"secao-corpo\">{Nl2p(quebra)}</div>" : "";
            return $"""
              <div class="secao">
                <div class="secao-kicker">
                  <span class="secao-num">{a.Num}</span>
                  {badgeHtml}
                </div>
                <h2 class="secao-titulo">{Esc(a.Titulo)}</h2>
                {corpoHtml}
                <div class="secao-rule"></div>
              </div>
              """;
        }));

        var sinteseHtml = !string.IsNullOrWhiteSpace(s.B6SinteseGeral)
            ? $"""
              <div class="sintese">
                <div class="sintese-eyebrow">Síntese</div>
                <h2 class="sintese-titulo">O padrão que aparece nas cinco dimensões</h2>
                <div class="sintese-corpo">{Nl2p(s.B6SinteseGeral)}</div>
              </div>
              """
            : "";

        var passos = new[]
        {
            ("Crie um ritual de silêncio diário",
             "Não começa com um devocional de 40 dias. Começa com cinco minutos, todo dia, sem exceção. Você senta, fecha o celular, e fica quieto na presença de Deus. Sem expectativa de ouvir algo grandioso. Só você ali. Intimidade não se pede — se pratica."),
            ("Escolha uma coisa e vai até o fim",
             "Só uma. Não três. Uma. Você escolhe o que mais importa agora e decide que vai terminar, mesmo quando a empolgação for embora. Especialmente quando a empolgação for embora. É aí que o governo começa — quando você faz o que decidiu mesmo sem sentir vontade."),
            ("Governe o que já está na sua mão",
             "Antes de expandir, organize o que você já tem. O próximo nível não vem de uma oportunidade nova — vem de você honrar, com excelência e constância, o que já foi entregue. Quem é fiel no pouco recebe autoridade sobre o muito."),
            ("Para de pedir direção antes de criar intimidade",
             "Você pede GPS mas não ligou o motor. A direção que você está buscando vem de dentro — de uma relação real com Deus, não de um sinal do céu que dispensa o processo. Você começa o silêncio diário, e a clareza vem. Não antes."),
            ("Protege sua mente como se ela valesse ouro",
             "Você passa horas consumindo o que o mundo oferece. Isso não é descanso — é mais ruído entrando. Define um horário em que o celular para. Sua mente precisa de silêncio para poder governar — e ela ainda não tem isso."),
        };

        var passosItens = string.Join("", passos.Select((p, i) =>
            $"""
            <div class="passo">
              <div class="passo-left"><span class="passo-num">{(i + 1):D2}</span></div>
              <div class="passo-right">
                <div class="passo-titulo">{Esc(p.Item1)}</div>
                <div class="passo-corpo">{Nl2p(p.Item2)}</div>
              </div>
            </div>
            """));

        var passosHtml = $"""
            <div class="passos">
              <div class="passos-eyebrow">Orientação</div>
              <h2 class="passos-titulo">Seus Próximos Passos</h2>
              <div class="passos-rule"></div>
              {passosItens}
            </div>
            """;

        return $"""
            <!DOCTYPE html>
            <html lang="pt-BR">
            <head>
            <meta charset="UTF-8"/>
            <title>Diagnóstico 5D — {Esc(s.Nome)}</title>
            <link rel="preconnect" href="https://fonts.googleapis.com"/>
            <link href="https://fonts.googleapis.com/css2?family=Playfair+Display:ital,wght@0,400;0,700;0,900;1,400&family=DM+Sans:ital,wght@0,300;0,400;0,500;1,300&family=Space+Grotesk:wght@500;600;700&display=swap" rel="stylesheet"/>
            <style>
            {Css}
            </style>
            </head>
            <body>
            <div class="page">
              <div class="capa">
                <div class="capa-eyebrow">Diagnóstico 5D &bull; Relatório Personalizado</div>
                <div class="capa-title">Diagnóstico<br>5D</div>
                <div class="capa-subtitle">Governo Interior &amp; Prosperidade</div>
                <div class="capa-meta">
                  <div class="capa-avatar">{Esc(inicial)}</div>
                  <div>
                    <div class="capa-nome">{Esc(s.Nome)}</div>
                    <div class="capa-brand">@sandrolopez &bull; Governo &amp; Finanças</div>
                  </div>
                </div>
              </div>
              <div class="intro">
                <p class="intro-text">{Esc(primeiroNome)}, o que você vai ler aqui não é um relatório técnico. É um espelho. Leia com calma, sem pressa — e sem se defender do que aparecer.</p>
              </div>
              {secoesHtml}
              {sinteseHtml}
              {passosHtml}
              <div class="rodape">
                <div class="rodape-brand">Sandro<span>Lopez</span></div>
                <div class="rodape-info">Relatório pessoal e intransferível<br>Gerado em {dataGeracao}</div>
              </div>
            </div>
            </body>
            </html>
            """;
    }
}
