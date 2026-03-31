using Diagnostico5D.API.Data;
using Diagnostico5D.API.DTOs;
using Diagnostico5D.API.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace Diagnostico5D.API.Services;

public class SubmissionService(AppDbContext db, IEvolutionApiService evolutionApi, ILogger<SubmissionService> logger) : ISubmissionService
{
    public async Task<IEnumerable<SubmissionDto>> GetAllAsync()
    {
        var submissions = await db.Submissions
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync();

        return submissions.Select(ToDto);
    }

    public async Task<SubmissionDto?> GetByIdAsync(int id)
    {
        var submission = await db.Submissions.FindAsync(id);
        return submission is null ? null : ToDto(submission);
    }

    public async Task<LookupResponse> LookupByWhatsappAsync(string whatsapp)
    {
        var digits = Regex.Replace(whatsapp, @"\D", "");

        var submission = await db.Submissions
            .Where(s => s.Status == "parcial")
            .OrderByDescending(s => s.UpdatedAt)
            .ToListAsync();

        var match = submission.FirstOrDefault(s =>
            Regex.Replace(s.Whatsapp ?? "", @"\D", "") == digits);

        if (match is null)
            return new LookupResponse(false, null);

        return new LookupResponse(true, ToDto(match));
    }

    public async Task<CreateResponse> CreateAsync(CreateSubmissionRequest req)
    {
        var submission = new Submission
        {
            Nome        = req.Nome.Trim(),
            Whatsapp    = req.Whatsapp?.Trim(),
            Status      = req.Status,
            UltimoBloco = req.UltimoBloco,
            CreatedAt   = DateTime.Now,
            UpdatedAt   = DateTime.Now,
            Q1 = req.Q1,  Q2 = req.Q2,  Q3 = req.Q3,  Q4 = req.Q4,  Q5 = req.Q5,
            Q6 = req.Q6,  Q7 = req.Q7,  Q8 = req.Q8,  Q9 = req.Q9,  Q10 = req.Q10,
            Q11 = req.Q11,Q12 = req.Q12,Q13 = req.Q13,Q14 = req.Q14,Q15 = req.Q15,
            Q16 = req.Q16,Q17 = req.Q17,Q18 = req.Q18,Q19 = req.Q19,Q20 = req.Q20,
            Q21 = req.Q21,Q22 = req.Q22,Q23 = req.Q23,Q24 = req.Q24,Q25 = req.Q25,
        };

        db.Submissions.Add(submission);
        await db.SaveChangesAsync();
        return new CreateResponse(true, submission.Id);
    }

    public async Task<bool> UpdateAsync(int id, UpdateSubmissionRequest req)
    {
        var submission = await db.Submissions.FindAsync(id);
        if (submission is null) return false;

        var eraParicial = submission.Status == "parcial";

        submission.Nome        = req.Nome.Trim();
        submission.Whatsapp    = req.Whatsapp?.Trim();
        submission.Status      = req.Status;
        submission.UltimoBloco = req.UltimoBloco;
        submission.UpdatedAt   = DateTime.Now;
        submission.Q1 = req.Q1;  submission.Q2 = req.Q2;  submission.Q3 = req.Q3;
        submission.Q4 = req.Q4;  submission.Q5 = req.Q5;  submission.Q6 = req.Q6;
        submission.Q7 = req.Q7;  submission.Q8 = req.Q8;  submission.Q9 = req.Q9;
        submission.Q10 = req.Q10;submission.Q11 = req.Q11;submission.Q12 = req.Q12;
        submission.Q13 = req.Q13;submission.Q14 = req.Q14;submission.Q15 = req.Q15;
        submission.Q16 = req.Q16;submission.Q17 = req.Q17;submission.Q18 = req.Q18;
        submission.Q19 = req.Q19;submission.Q20 = req.Q20;submission.Q21 = req.Q21;
        submission.Q22 = req.Q22;submission.Q23 = req.Q23;submission.Q24 = req.Q24;
        submission.Q25 = req.Q25;

        if (req.Status == "completo" && eraParicial)
            submission.ConcluidoEm = DateTime.Now;

        await db.SaveChangesAsync();

        // Envia mensagem WhatsApp quando finaliza
        if (req.Status == "completo" && eraParicial && !string.IsNullOrWhiteSpace(submission.Whatsapp))
        {
            var resultado = await EnviarMensagemConfirmacaoAsync(submission);
            if (resultado.Sucesso)
            {
                submission.WhatsappEnviado   = true;
                submission.WhatsappEnviadoEm = DateTime.Now;
                await db.SaveChangesAsync();
            }
        }

        return true;
    }

    public async Task<bool> UpdateBloco6Async(int id, Bloco6Request req)
    {
        var submission = await db.Submissions.FindAsync(id);
        if (submission is null) return false;

        submission.B6IdentidadeStatus   = req.B6IdentidadeStatus;
        submission.B6IdentidadeQuebra   = req.B6IdentidadeQuebra;
        submission.B6GovernoStatus      = req.B6GovernoStatus;
        submission.B6GovernoQuebra      = req.B6GovernoQuebra;
        submission.B6PreparacaoStatus   = req.B6PreparacaoStatus;
        submission.B6PreparacaoQuebra   = req.B6PreparacaoQuebra;
        submission.B6FeAcaoStatus       = req.B6FeAcaoStatus;
        submission.B6FeAcaoQuebra       = req.B6FeAcaoQuebra;
        submission.B6ProsperidadeStatus = req.B6ProsperidadeStatus;
        submission.B6ProsperidadeQuebra = req.B6ProsperidadeQuebra;
        submission.B6Gargalo            = req.B6Gargalo;
        submission.B6ErroInvisivel      = req.B6ErroInvisivel;
        submission.B6ProximoMovimento   = req.B6ProximoMovimento;
        submission.UpdatedAt            = DateTime.Now;

        await db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var submission = await db.Submissions.FindAsync(id);
        if (submission is null) return false;

        db.Submissions.Remove(submission);
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> EditarCadastroAsync(int id, EditarCadastroRequest req)
    {
        var submission = await db.Submissions.FindAsync(id);
        if (submission is null) return false;

        submission.Nome      = req.Nome.Trim();
        submission.Whatsapp  = req.Whatsapp?.Trim();
        submission.UpdatedAt = DateTime.Now;

        await db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UpdateMentorAsync(int id, MentorRequest req)
    {
        var submission = await db.Submissions.FindAsync(id);
        if (submission is null) return false;

        submission.MentorRevisado   = req.Revisado;
        submission.MentorObservacao = req.Observacao;
        submission.UpdatedAt        = DateTime.Now;

        await db.SaveChangesAsync();
        return true;
    }

    public async Task<EvolutionApiResult> ReenviarWhatsappAsync(int id)
    {
        var submission = await db.Submissions.FindAsync(id);
        if (submission is null)
            return new EvolutionApiResult { Sucesso = false, MensagemErro = "Não encontrado.", StatusCode = 404 };

        if (string.IsNullOrWhiteSpace(submission.Whatsapp))
            return new EvolutionApiResult { Sucesso = false, MensagemErro = "Sem número de WhatsApp.", StatusCode = 400 };

        var resultado = await EnviarMensagemConfirmacaoAsync(submission);

        if (resultado.Sucesso)
        {
            submission.WhatsappEnviado   = true;
            submission.WhatsappEnviadoEm = DateTime.Now;
            submission.UpdatedAt         = DateTime.Now;
            await db.SaveChangesAsync();
        }

        return resultado;
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private async Task<EvolutionApiResult> EnviarMensagemConfirmacaoAsync(Submission submission)
    {
        var primeiroNome = submission.Nome.Split(' ')[0];
        var mensagem =
            $"Olá, {primeiroNome}! 🙌\n\n" +
            $"Recebemos seu *Diagnóstico 5D* completo!\n\n" +
            $"Em breve a equipe do Ap. Sandro Lopez vai analisar suas respostas e entrar em contato com você com o diagnóstico personalizado.\n\n" +
            $"Fique atento ao WhatsApp. 😊";

        var resultado = await evolutionApi.EnviarMensagemTextoAsync(submission.Whatsapp!, mensagem);

        if (!resultado.Sucesso)
            logger.LogWarning("Falha ao enviar WhatsApp para {Whatsapp}: {Erro}", submission.Whatsapp, resultado.MensagemErro);

        return resultado;
    }

    private static SubmissionDto ToDto(Submission s) => new(
        s.Id, s.Nome, s.Whatsapp, s.Status, s.UltimoBloco, s.CreatedAt, s.UpdatedAt, s.ConcluidoEm,
        s.Q1,  s.Q2,  s.Q3,  s.Q4,  s.Q5,
        s.Q6,  s.Q7,  s.Q8,  s.Q9,  s.Q10,
        s.Q11, s.Q12, s.Q13, s.Q14, s.Q15,
        s.Q16, s.Q17, s.Q18, s.Q19, s.Q20,
        s.Q21, s.Q22, s.Q23, s.Q24, s.Q25,
        s.B6IdentidadeStatus,   s.B6IdentidadeQuebra,
        s.B6GovernoStatus,      s.B6GovernoQuebra,
        s.B6PreparacaoStatus,   s.B6PreparacaoQuebra,
        s.B6FeAcaoStatus,       s.B6FeAcaoQuebra,
        s.B6ProsperidadeStatus, s.B6ProsperidadeQuebra,
        s.B6Gargalo, s.B6ErroInvisivel, s.B6ProximoMovimento,
        s.WhatsappEnviado, s.WhatsappEnviadoEm,
        s.MentorRevisado, s.MentorObservacao
    );
}
