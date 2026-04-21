namespace Diagnostico5D.API.Services;

public interface IEvolutionApiService
{
    Task<EvolutionApiResult> EnviarMensagemTextoAsync(
        string numero,
        string mensagem,
        CancellationToken cancellationToken = default);

    Task<EvolutionApiResult> EnviarDocumentoAsync(
        string numero,
        string caminhoArquivo,
        string nomeArquivo,
        string? caption = null,
        CancellationToken cancellationToken = default);
}

public class EvolutionApiResult
{
    public bool Sucesso { get; set; }
    public string? MensagemErro { get; set; }
    public int StatusCode { get; set; }
    public string? MessageId { get; set; }
}
