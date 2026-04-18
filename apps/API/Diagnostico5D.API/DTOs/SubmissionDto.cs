namespace Diagnostico5D.API.DTOs;

public record SubmissionDto(
    int Id,
    string Nome,
    string? Whatsapp,
    string Status,
    int UltimoBloco,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    DateTime? ConcluidoEm,
    string? Q1,  string? Q2,  string? Q3,  string? Q4,  string? Q5,
    string? Q6,  string? Q7,  string? Q8,  string? Q9,  string? Q10,
    string? Q11, string? Q12, string? Q13, string? Q14, string? Q15,
    string? Q16, string? Q17, string? Q18, string? Q19, string? Q20,
    string? Q21, string? Q22, string? Q23, string? Q24, string? Q25,
    string? B6IdentidadeStatus,  string? B6IdentidadeQuebra,
    string? B6GovernoStatus,     string? B6GovernoQuebra,
    string? B6PreparacaoStatus,  string? B6PreparacaoQuebra,
    string? B6FeAcaoStatus,      string? B6FeAcaoQuebra,
    string? B6ProsperidadeStatus,string? B6ProsperidadeQuebra,
    string? B6Gargalo,
    string? B6ErroInvisivel,
    string? B6ProximoMovimento,
    bool WhatsappEnviado,
    DateTime? WhatsappEnviadoEm,
    bool MentorRevisado,
    string? MentorObservacao,
    string Fase
);

public record CreateSubmissionRequest(
    string Nome,
    string? Whatsapp,
    string Status,
    int UltimoBloco,
    string? Q1,  string? Q2,  string? Q3,  string? Q4,  string? Q5,
    string? Q6,  string? Q7,  string? Q8,  string? Q9,  string? Q10,
    string? Q11, string? Q12, string? Q13, string? Q14, string? Q15,
    string? Q16, string? Q17, string? Q18, string? Q19, string? Q20,
    string? Q21, string? Q22, string? Q23, string? Q24, string? Q25
);

public record UpdateSubmissionRequest(
    string Nome,
    string? Whatsapp,
    string Status,
    int UltimoBloco,
    string? Q1,  string? Q2,  string? Q3,  string? Q4,  string? Q5,
    string? Q6,  string? Q7,  string? Q8,  string? Q9,  string? Q10,
    string? Q11, string? Q12, string? Q13, string? Q14, string? Q15,
    string? Q16, string? Q17, string? Q18, string? Q19, string? Q20,
    string? Q21, string? Q22, string? Q23, string? Q24, string? Q25
);

public record Bloco6Request(
    string? B6IdentidadeStatus,  string? B6IdentidadeQuebra,
    string? B6GovernoStatus,     string? B6GovernoQuebra,
    string? B6PreparacaoStatus,  string? B6PreparacaoQuebra,
    string? B6FeAcaoStatus,      string? B6FeAcaoQuebra,
    string? B6ProsperidadeStatus,string? B6ProsperidadeQuebra,
    string? B6Gargalo,
    string? B6ErroInvisivel,
    string? B6ProximoMovimento
);

public record EditarCadastroRequest(string Nome, string? Whatsapp);

public record MentorRequest(bool Revisado, string? Observacao);

public record FaseRequest(string Fase);

public record CriarConvidadoRequest(string Nome, string? Whatsapp);

public record LookupResponse(bool Found, SubmissionDto? Record);

public record CreateResponse(bool Success, int Id);
public record UpdateResponse(bool Success);
