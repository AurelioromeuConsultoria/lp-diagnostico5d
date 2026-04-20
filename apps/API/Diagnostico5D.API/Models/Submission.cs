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
    public string? B6GovFinanceiroStatus { get; set; }
    public string? B6GovFinanceiroQuebra { get; set; }
    public string? B6IdentidadeAutoStatus { get; set; }
    public string? B6IdentidadeAutoQuebra { get; set; }
    public string? B6GovInteriorStatus { get; set; }
    public string? B6GovInteriorQuebra { get; set; }
    public string? B6AmbienteStatus { get; set; }
    public string? B6AmbienteQuebra { get; set; }
    public string? B6EspiritualidadeStatus { get; set; }
    public string? B6EspiritualidadeQuebra { get; set; }
    public string? B6SinteseGeral { get; set; }
    public string? B6Passos { get; set; } // JSON: [{titulo, texto}, ...]

    // Fase do fluxo Kanban
    public string Fase { get; set; } = "novo";

    // WhatsApp
    public bool WhatsappEnviado { get; set; } = false;
    public DateTime? WhatsappEnviadoEm { get; set; }

    // Mentor
    public bool MentorRevisado { get; set; } = false;
    public string? MentorObservacao { get; set; }
}
