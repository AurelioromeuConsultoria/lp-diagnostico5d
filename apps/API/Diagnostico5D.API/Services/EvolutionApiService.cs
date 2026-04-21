using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Diagnostico5D.API.Configuration;
using Diagnostico5D.API.Utils;
using Microsoft.Extensions.Options;

namespace Diagnostico5D.API.Services;

public class EvolutionApiService : IEvolutionApiService
{
    private readonly HttpClient _httpClient;
    private readonly EvolutionApiSettings _settings;
    private readonly ILogger<EvolutionApiService> _logger;

    public EvolutionApiService(
        HttpClient httpClient,
        IOptions<EvolutionApiSettings> settings,
        ILogger<EvolutionApiService> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;

        _httpClient.Timeout = TimeSpan.FromSeconds(_settings.TimeoutSeconds);

        if (!string.IsNullOrEmpty(_settings.BaseUrl))
            _httpClient.BaseAddress = new Uri(_settings.BaseUrl.TrimEnd('/') + "/");

        if (!string.IsNullOrEmpty(_settings.ApiKey))
            _httpClient.DefaultRequestHeaders.Add("apikey", _settings.ApiKey);
    }

    public async Task<EvolutionApiResult> EnviarMensagemTextoAsync(
        string numero,
        string mensagem,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_settings.BaseUrl))
            return new EvolutionApiResult { Sucesso = false, MensagemErro = "Evolution API BaseUrl não configurada", StatusCode = 500 };

        if (string.IsNullOrWhiteSpace(_settings.InstanceName))
            return new EvolutionApiResult { Sucesso = false, MensagemErro = "Evolution API InstanceName não configurada", StatusCode = 500 };

        if (string.IsNullOrWhiteSpace(numero))
            return new EvolutionApiResult { Sucesso = false, MensagemErro = "Número não pode ser vazio", StatusCode = 400 };

        if (string.IsNullOrWhiteSpace(mensagem))
            return new EvolutionApiResult { Sucesso = false, MensagemErro = "Mensagem não pode ser vazia", StatusCode = 400 };

        string numeroFormatado;
        try
        {
            numeroFormatado = TelefoneUtils.FormatarParaEvolutionApi(numero, _settings.CodigoPaisPadrao);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao formatar número {Numero}", numero);
            return new EvolutionApiResult { Sucesso = false, MensagemErro = $"Número inválido: {ex.Message}", StatusCode = 400 };
        }

        var request = new
        {
            number = numeroFormatado,
            text = mensagem,
            delay = Math.Max(0, _settings.DelayMs),
            linkPreview = false
        };

        var endpoint = $"message/sendText/{_settings.InstanceName}";

        for (int tentativa = 1; tentativa <= _settings.MaxRetries; tentativa++)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync(endpoint, request, cancellationToken);
                var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    string? messageId = null;
                    try
                    {
                        var doc = JsonDocument.Parse(responseContent);
                        if (doc.RootElement.TryGetProperty("key", out var key) &&
                            key.TryGetProperty("id", out var id))
                            messageId = id.GetString();
                    }
                    catch { /* ignora erro de parse */ }

                    _logger.LogInformation("Mensagem enviada para {Numero} (id: {MessageId})", numeroFormatado, messageId);
                    return new EvolutionApiResult { Sucesso = true, StatusCode = (int)response.StatusCode, MessageId = messageId };
                }

                var isTransient = IsTransientFailure(response.StatusCode);
                _logger.LogWarning("Falha Evolution API - Status: {Status}, Tentativa: {T}/{Max}",
                    response.StatusCode, tentativa, _settings.MaxRetries);

                if (!isTransient || tentativa >= _settings.MaxRetries)
                    return new EvolutionApiResult
                    {
                        Sucesso = false,
                        StatusCode = (int)response.StatusCode,
                        MensagemErro = responseContent
                    };

                await Task.Delay(ObterBackoff(tentativa), cancellationToken);
            }
            catch (TaskCanceledException)
            {
                if (tentativa >= _settings.MaxRetries)
                    return new EvolutionApiResult { Sucesso = false, MensagemErro = "Timeout", StatusCode = 408 };

                await Task.Delay(ObterBackoff(tentativa), cancellationToken);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Erro de conexão com Evolution API");
                if (tentativa >= _settings.MaxRetries)
                    return new EvolutionApiResult { Sucesso = false, MensagemErro = ex.Message, StatusCode = 0 };

                await Task.Delay(ObterBackoff(tentativa), cancellationToken);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Configuração inválida da Evolution API");
                return new EvolutionApiResult { Sucesso = false, MensagemErro = ex.Message, StatusCode = 500 };
            }
            catch (UriFormatException ex)
            {
                _logger.LogError(ex, "URL inválida da Evolution API: {BaseUrl}", _settings.BaseUrl);
                return new EvolutionApiResult { Sucesso = false, MensagemErro = "Evolution API BaseUrl inválida", StatusCode = 500 };
            }
        }

        return new EvolutionApiResult { Sucesso = false, MensagemErro = "Falha após todas as tentativas", StatusCode = 500 };
    }

    public async Task<EvolutionApiResult> EnviarDocumentoAsync(
        string numero,
        string caminhoArquivo,
        string nomeArquivo,
        string? caption = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_settings.BaseUrl))
            return new EvolutionApiResult { Sucesso = false, MensagemErro = "Evolution API BaseUrl não configurada", StatusCode = 500 };

        if (string.IsNullOrWhiteSpace(_settings.InstanceName))
            return new EvolutionApiResult { Sucesso = false, MensagemErro = "Evolution API InstanceName não configurada", StatusCode = 500 };

        if (string.IsNullOrWhiteSpace(numero))
            return new EvolutionApiResult { Sucesso = false, MensagemErro = "Número não pode ser vazio", StatusCode = 400 };

        if (!File.Exists(caminhoArquivo))
            return new EvolutionApiResult { Sucesso = false, MensagemErro = "Arquivo PDF não encontrado", StatusCode = 400 };

        string numeroFormatado;
        try
        {
            numeroFormatado = TelefoneUtils.FormatarParaEvolutionApi(numero, _settings.CodigoPaisPadrao);
        }
        catch (Exception ex)
        {
            return new EvolutionApiResult { Sucesso = false, MensagemErro = $"Número inválido: {ex.Message}", StatusCode = 400 };
        }

        const long MaxBytes = 25 * 1024 * 1024; // 25 MB
        var fileInfo = new FileInfo(caminhoArquivo);
        if (fileInfo.Length > MaxBytes)
            return new EvolutionApiResult
            {
                Sucesso = false,
                MensagemErro = $"PDF muito grande ({fileInfo.Length / 1024 / 1024} MB). Máximo permitido: 25 MB.",
                StatusCode = 400
            };

        var bytes = await File.ReadAllBytesAsync(caminhoArquivo, cancellationToken);
        var base64 = Convert.ToBase64String(bytes);

        var request = new
        {
            number = numeroFormatado,
            mediatype = "document",
            media = base64,
            fileName = nomeArquivo,
            caption = caption ?? "",
            delay = Math.Max(0, _settings.DelayMs),
        };

        var endpoint = $"message/sendMedia/{_settings.InstanceName}";

        for (int tentativa = 1; tentativa <= _settings.MaxRetries; tentativa++)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync(endpoint, request, cancellationToken);
                var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Documento enviado para {Numero} ({Arquivo})", numeroFormatado, nomeArquivo);
                    return new EvolutionApiResult { Sucesso = true, StatusCode = (int)response.StatusCode };
                }

                if (!IsTransientFailure(response.StatusCode) || tentativa >= _settings.MaxRetries)
                    return new EvolutionApiResult { Sucesso = false, StatusCode = (int)response.StatusCode, MensagemErro = responseContent };

                await Task.Delay(ObterBackoff(tentativa), cancellationToken);
            }
            catch (TaskCanceledException)
            {
                if (tentativa >= _settings.MaxRetries)
                    return new EvolutionApiResult { Sucesso = false, MensagemErro = "Timeout", StatusCode = 408 };
                await Task.Delay(ObterBackoff(tentativa), cancellationToken);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Erro de conexão com Evolution API ao enviar documento");
                if (tentativa >= _settings.MaxRetries)
                    return new EvolutionApiResult { Sucesso = false, MensagemErro = ex.Message, StatusCode = 0 };
                await Task.Delay(ObterBackoff(tentativa), cancellationToken);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Configuração inválida da Evolution API");
                return new EvolutionApiResult { Sucesso = false, MensagemErro = ex.Message, StatusCode = 500 };
            }
            catch (UriFormatException ex)
            {
                _logger.LogError(ex, "URL inválida da Evolution API: {BaseUrl}", _settings.BaseUrl);
                return new EvolutionApiResult { Sucesso = false, MensagemErro = "Evolution API BaseUrl inválida", StatusCode = 500 };
            }
        }

        return new EvolutionApiResult { Sucesso = false, MensagemErro = "Falha após todas as tentativas", StatusCode = 500 };
    }

    private static bool IsTransientFailure(HttpStatusCode status)
    {
        var code = (int)status;
        return code is >= 500 and < 600 || status == (HttpStatusCode)429;
    }

    private TimeSpan ObterBackoff(int tentativa)
    {
        var segundos = _settings.RetryDelaySeconds * Math.Pow(2, tentativa - 1);
        return TimeSpan.FromSeconds(Math.Min(segundos, 60));
    }
}
