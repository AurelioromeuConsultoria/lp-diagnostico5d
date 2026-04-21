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
        try
        {
            Directory.CreateDirectory(_pdfDir);
            var filePath = Path.Combine(_pdfDir, $"devolutiva-{s.Id}.pdf");
            var html = BuildHtml(s);

            var chromiumPath = Environment.GetEnvironmentVariable("CHROMIUM_PATH");
            string[] sandboxArgs = ["--no-sandbox", "--disable-setuid-sandbox", "--disable-dev-shm-usage", "--disable-gpu"];
            logger.LogInformation("Gerando PDF para submission {SubmissionId}. PdfDir={PdfDir} CHROMIUM_PATH={ChromiumPath}",
                s.Id, _pdfDir, chromiumPath ?? "(null)");

            LaunchOptions launchOpts;
            if (!string.IsNullOrEmpty(chromiumPath) && File.Exists(chromiumPath))
            {
                launchOpts = new LaunchOptions { Headless = true, ExecutablePath = chromiumPath, Args = sandboxArgs };
            }
            else
            {
                var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";
                if (!string.Equals(env, "Development", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException(
                        "Chromium não encontrado. Configure a variável de ambiente CHROMIUM_PATH no servidor.");

                logger.LogInformation("CHROMIUM_PATH não definido — baixando Chromium (apenas em desenvolvimento)");
                using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));
                var download = new BrowserFetcher().DownloadAsync();
                if (await Task.WhenAny(download, Task.Delay(Timeout.Infinite, cts.Token)) != download)
                    throw new TimeoutException("Timeout ao baixar Chromium. Instale manualmente e configure CHROMIUM_PATH.");
                await download;
                launchOpts = new LaunchOptions { Headless = true, Args = sandboxArgs };
            }

            await using var browser = await Puppeteer.LaunchAsync(launchOpts);
            await using var page = await browser.NewPageAsync();
            page.DefaultNavigationTimeout = 60_000;
            page.DefaultTimeout = 60_000;

            await page.SetContentAsync(html, new NavigationOptions
            {
                WaitUntil = [WaitUntilNavigation.Networkidle2],
                Timeout = 60_000
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
        catch (Exception ex) when (ex is not InvalidOperationException and not TimeoutException)
        {
            logger.LogError(ex, "Erro ao gerar PDF para submission {Id}", s.Id);
            throw new InvalidOperationException($"Falha ao gerar PDF: {ex.Message}", ex);
        }
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
          --dark:#180E06;--orange:#C94B00;--bg:#FAFAF8;
          --text:#2C1A0E;--muted:#8A6F5E;--border:#EDE3D8;
          --ok:#15803D;--ok-bg:#F0FDF4;--ok-bd:#86EFAC;
          --warn:#B45309;--warn-bg:#FFFBEB;--warn-bd:#FCD34D;
          --crit:#B91C1C;--crit-bg:#FEF2F2;--crit-bd:#FCA5A5;
        }
        body{font-family:'DM Sans',sans-serif;background:#fff;color:var(--text);
          font-size:13px;line-height:1.75;
          -webkit-print-color-adjust:exact;print-color-adjust:exact;}

        /* ── CAPA ── */
        .capa{background:var(--dark);padding:72px 60px 56px;position:relative;overflow:hidden;}
        .capa-deco{position:absolute;right:-10px;bottom:-50px;
          font-family:'Playfair Display',serif;font-size:240px;font-weight:900;
          color:rgba(201,75,0,.06);line-height:1;pointer-events:none;user-select:none;}
        .capa-pill{display:inline-flex;align-items:center;gap:8px;
          border:1px solid rgba(201,75,0,.35);border-radius:40px;
          padding:5px 14px;margin-bottom:36px;
          font-family:'Space Grotesk',sans-serif;font-size:9px;font-weight:700;
          letter-spacing:.2em;text-transform:uppercase;color:rgba(232,213,186,.75);}
        .capa-nome{font-family:'Playfair Display',serif;font-size:52px;font-weight:900;
          color:#fff;line-height:1.0;letter-spacing:-.025em;margin-bottom:10px;}
        .capa-sub{font-size:14px;font-weight:300;letter-spacing:.06em;
          color:rgba(255,255,255,.38);margin-bottom:52px;}
        .capa-linha{height:1px;background:linear-gradient(90deg,var(--orange),rgba(201,75,0,.08));
          margin-bottom:28px;}
        .capa-rodape{display:flex;align-items:center;justify-content:space-between;}
        .capa-brand{font-family:'Space Grotesk',sans-serif;font-size:11px;font-weight:700;
          letter-spacing:.14em;text-transform:uppercase;color:rgba(255,255,255,.35);}
        .capa-data{font-size:10px;color:rgba(255,255,255,.25);}

        /* ── CONTEÚDO ── */
        .content{padding:56px 60px 0;}

        /* ── INTRO ── */
        .intro{margin-bottom:52px;padding-bottom:48px;border-bottom:1px solid var(--border);}
        .intro-text{font-family:'Playfair Display',serif;font-size:17px;font-weight:400;
          font-style:italic;color:var(--dark);line-height:1.9;}

        /* ── SEÇÕES ── */
        .secao{margin-bottom:20px;padding:30px 32px 28px;
          background:var(--bg);border-radius:10px;
          border:1px solid var(--border);border-left-width:4px;
          position:relative;overflow:hidden;}
        .secao.ok{border-left-color:var(--ok);}
        .secao.atencao{border-left-color:#F59E0B;}
        .secao.critico{border-left-color:var(--crit);}
        .secao-deco{position:absolute;top:-12px;right:16px;
          font-family:'Playfair Display',serif;font-size:80px;font-weight:900;
          color:rgba(24,14,6,.035);line-height:1;pointer-events:none;user-select:none;}
        .secao-topo{display:flex;align-items:flex-start;justify-content:space-between;
          gap:12px;margin-bottom:14px;}
        .secao-titulo{font-family:'Playfair Display',serif;font-size:19px;font-weight:700;
          color:var(--dark);line-height:1.2;letter-spacing:-.01em;}
        .badge{display:inline-flex;align-items:center;gap:5px;flex-shrink:0;
          padding:4px 10px;border-radius:20px;
          font-family:'Space Grotesk',sans-serif;font-size:8.5px;font-weight:700;
          letter-spacing:.1em;text-transform:uppercase;}
        .badge.ok{background:var(--ok-bg);color:var(--ok);border:1px solid var(--ok-bd);}
        .badge.atencao{background:var(--warn-bg);color:var(--warn);border:1px solid var(--warn-bd);}
        .badge.critico{background:var(--crit-bg);color:var(--crit);border:1px solid var(--crit-bd);}
        .badge-dot{width:5px;height:5px;border-radius:50%;background:currentColor;flex-shrink:0;}
        .secao-corpo p{color:#5A3E30;line-height:1.9;font-size:13px;
          margin-bottom:10px;text-align:justify;}

        /* ── SÍNTESE ── */
        .sintese{margin:40px 0;background:var(--dark);border-radius:10px;
          padding:40px 44px;position:relative;overflow:hidden;}
        .sintese::before{content:'';position:absolute;top:0;left:0;bottom:0;
          width:4px;background:var(--orange);}
        .sintese-label{font-family:'Space Grotesk',sans-serif;font-size:9px;font-weight:700;
          letter-spacing:.22em;text-transform:uppercase;color:var(--orange);margin-bottom:10px;}
        .sintese-titulo{font-family:'Playfair Display',serif;font-size:21px;font-weight:700;
          font-style:italic;color:#fff;line-height:1.35;margin-bottom:18px;}
        .sintese-corpo p{color:rgba(255,255,255,.62);line-height:1.9;font-size:13px;
          margin-bottom:10px;text-align:justify;}

        /* ── PRÓXIMOS PASSOS ── */
        .passos{margin:40px 0 0;}
        .passos-header{margin-bottom:32px;padding-bottom:20px;
          border-bottom:2px solid var(--border);}
        .passos-label{font-family:'Space Grotesk',sans-serif;font-size:9px;font-weight:700;
          letter-spacing:.22em;text-transform:uppercase;color:var(--orange);margin-bottom:6px;}
        .passos-titulo{font-family:'Playfair Display',serif;font-size:30px;font-weight:900;
          color:var(--dark);line-height:1.1;}
        .passo{display:flex;gap:20px;margin-bottom:28px;}
        .passo-circle{width:40px;height:40px;border-radius:50%;background:var(--orange);
          display:flex;align-items:center;justify-content:center;flex-shrink:0;margin-top:1px;
          font-family:'Space Grotesk',sans-serif;font-size:12px;font-weight:700;color:#fff;}
        .passo-titulo{font-family:'DM Sans',sans-serif;font-size:13.5px;font-weight:700;
          color:var(--dark);margin-bottom:5px;line-height:1.3;}
        .passo-corpo p{color:#6B5040;line-height:1.85;font-size:12.5px;
          text-align:justify;margin-bottom:5px;}

        /* ── RODAPÉ ── */
        .rodape{margin-top:52px;padding:20px 0 52px;
          border-top:1px solid var(--border);
          display:flex;align-items:center;justify-content:space-between;}
        .rodape-brand{font-family:'Space Grotesk',sans-serif;font-size:13px;font-weight:700;
          letter-spacing:.05em;color:var(--dark);}
        .rodape-brand em{color:var(--orange);font-style:normal;}
        .rodape-info{font-size:10px;color:var(--muted);text-align:right;line-height:1.7;}
        """;

    private static string BuildHtml(Submission s)
    {
        var primeiroNome = (s.Nome ?? "").Split(' ')[0];
        var dataGeracao = DateTime.Now.ToString("dd 'de' MMMM 'de' yyyy", new System.Globalization.CultureInfo("pt-BR"));

        var areas = new[]
        {
            (Num: "01", Titulo: "GOVERNO FINANCEIRO",            Status: s.B6GovFinanceiroStatus,   Quebra: s.B6GovFinanceiroQuebra),
            (Num: "02", Titulo: "IDENTIDADE E AUTOCONCEITO",     Status: s.B6IdentidadeAutoStatus,  Quebra: s.B6IdentidadeAutoQuebra),
            (Num: "03", Titulo: "GOVERNO INTERIOR E CONSTÂNCIA", Status: s.B6GovInteriorStatus,     Quebra: s.B6GovInteriorQuebra),
            (Num: "04", Titulo: "AMBIENTE E ALIANÇAS",           Status: s.B6AmbienteStatus,        Quebra: s.B6AmbienteQuebra),
            (Num: "05", Titulo: "ESPIRITUALIDADE E DIREÇÃO",     Status: s.B6EspiritualidadeStatus, Quebra: s.B6EspiritualidadeQuebra),
        };

        var statusInfo = new Dictionary<string, (string Label, string Cls)>
        {
            ["ok"]      = ("Em bom estado",      "ok"),
            ["atencao"] = ("Requer atenção",      "atencao"),
            ["critico"] = ("Quebra identificada", "critico"),
        };

        var secoesHtml = string.Join("", areas.Select(a =>
        {
            var quebra = (a.Quebra ?? "").Trim();
            if (string.IsNullOrEmpty(a.Status) && string.IsNullOrEmpty(quebra)) return "";

            statusInfo.TryGetValue(a.Status ?? "", out var st);
            var statusCls = st.Cls ?? "";
            var badgeHtml = statusCls.Length > 0
                ? $"<span class=\"badge {statusCls}\"><span class=\"badge-dot\"></span>{Esc(st.Label)}</span>"
                : "";
            var corpoHtml = quebra.Length > 0 ? $"<div class=\"secao-corpo\">{Nl2p(quebra)}</div>" : "";

            return $"""
              <div class="secao {statusCls}">
                <div class="secao-deco">{a.Num}</div>
                <div class="secao-topo">
                  <h2 class="secao-titulo">{Esc(a.Titulo)}</h2>
                  {badgeHtml}
                </div>
                {corpoHtml}
              </div>
              """;
        }));

        var sinteseHtml = !string.IsNullOrWhiteSpace(s.B6SinteseGeral)
            ? $"""
              <div class="sintese">
                <div class="sintese-label">Síntese</div>
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
              <div class="passo-circle">{i + 1}</div>
              <div>
                <div class="passo-titulo">{Esc(p.Item1)}</div>
                <div class="passo-corpo">{Nl2p(p.Item2)}</div>
              </div>
            </div>
            """));

        var passosHtml = $"""
            <div class="passos">
              <div class="passos-header">
                <div class="passos-label">Orientação</div>
                <h2 class="passos-titulo">Seus Próximos Passos</h2>
              </div>
              {passosItens}
            </div>
            """;

        return $"""
            <!DOCTYPE html>
            <html lang="pt-BR">
            <head>
            <meta charset="UTF-8"/>
            <title>Diagnóstico 5D — {Esc(s.Nome)}</title>
            <style>{Css}</style>
            </head>
            <body>
            <div class="capa">
              <div class="capa-deco">5D</div>
              <div class="capa-pill">✦ Diagnóstico 5D &nbsp;·&nbsp; Relatório Personalizado</div>
              <div class="capa-nome">{Esc(s.Nome)}</div>
              <div class="capa-sub">Governo Interior &amp; Prosperidade</div>
              <div class="capa-linha"></div>
              <div class="capa-rodape">
                <div class="capa-brand">@sandrolopez</div>
                <div class="capa-data">{dataGeracao}</div>
              </div>
            </div>
            <div class="content">
              <div class="intro">
                <p class="intro-text">{Esc(primeiroNome)}, o que você vai ler aqui não é um relatório técnico. É um espelho. Leia com calma, sem pressa — e sem se defender do que aparecer.</p>
              </div>
              {secoesHtml}
              {sinteseHtml}
              {passosHtml}
              <div class="rodape">
                <div class="rodape-brand">Sandro<em>Lopez</em></div>
                <div class="rodape-info">Relatório pessoal e intransferível<br>{dataGeracao}</div>
              </div>
            </div>
            </body>
            </html>
            """;
    }
}
