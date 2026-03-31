namespace Diagnostico5D.API.Models;

public class Submission
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string? Whatsapp { get; set; }
    public string Status { get; set; } = "parcial"; // parcial | completo
    public int UltimoBloco { get; set; } = 0;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
    public DateTime? ConcluidoEm { get; set; }

    // Bloco 1 — Identidade e Posição
    public string? Q1 { get; set; }
    public string? Q2 { get; set; }
    public string? Q3 { get; set; }
    public string? Q4 { get; set; }
    public string? Q5 { get; set; }

    // Bloco 2 — Governo Interior
    public string? Q6 { get; set; }
    public string? Q7 { get; set; }
    public string? Q8 { get; set; }
    public string? Q9 { get; set; }
    public string? Q10 { get; set; }

    // Bloco 3 — Preparação e Processo
    public string? Q11 { get; set; }
    public string? Q12 { get; set; }
    public string? Q13 { get; set; }
    public string? Q14 { get; set; }
    public string? Q15 { get; set; }

    // Bloco 4 — Fé, Ação e Aliança
    public string? Q16 { get; set; }
    public string? Q17 { get; set; }
    public string? Q18 { get; set; }
    public string? Q19 { get; set; }
    public string? Q20 { get; set; }

    // Bloco 5 — Prosperidade e Leis
    public string? Q21 { get; set; }
    public string? Q22 { get; set; }
    public string? Q23 { get; set; }
    public string? Q24 { get; set; }
    public string? Q25 { get; set; }

    // Bloco 6 — Diagnóstico Final (preenchido pelo mentor)
    public string? B6IdentidadeStatus { get; set; }
    public string? B6IdentidadeQuebra { get; set; }
    public string? B6GovernoStatus { get; set; }
    public string? B6GovernoQuebra { get; set; }
    public string? B6PreparacaoStatus { get; set; }
    public string? B6PreparacaoQuebra { get; set; }
    public string? B6FeAcaoStatus { get; set; }
    public string? B6FeAcaoQuebra { get; set; }
    public string? B6ProsperidadeStatus { get; set; }
    public string? B6ProsperidadeQuebra { get; set; }
    public string? B6Gargalo { get; set; }
    public string? B6ErroInvisivel { get; set; }
    public string? B6ProximoMovimento { get; set; }

    // WhatsApp
    public bool WhatsappEnviado { get; set; } = false;
    public DateTime? WhatsappEnviadoEm { get; set; }

    // Mentor
    public bool MentorRevisado { get; set; } = false;
    public string? MentorObservacao { get; set; }
}
