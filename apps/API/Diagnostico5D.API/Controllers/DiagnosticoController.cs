using Diagnostico5D.API.DTOs;
using Diagnostico5D.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Diagnostico5D.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DiagnosticoController(ISubmissionService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var submissions = await service.GetAllAsync();
        return Ok(submissions);
    }

    [HttpGet("lookup")]
    public async Task<IActionResult> Lookup([FromQuery] string whatsapp)
    {
        if (string.IsNullOrWhiteSpace(whatsapp))
            return BadRequest(new { error = "WhatsApp obrigatório." });

        var result = await service.LookupByWhatsappAsync(whatsapp);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var submission = await service.GetByIdAsync(id);
        if (submission is null) return NotFound(new { error = "Não encontrado." });
        return Ok(submission);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateSubmissionRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Nome))
            return BadRequest(new { error = "Nome é obrigatório." });

        var result = await service.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateSubmissionRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Nome))
            return BadRequest(new { error = "Nome é obrigatório." });

        var updated = await service.UpdateAsync(id, request);
        if (!updated) return NotFound(new { error = "Não encontrado." });

        return Ok(new UpdateResponse(true));
    }

    [HttpPut("{id:int}/bloco6")]
    public async Task<IActionResult> UpdateBloco6(int id, [FromBody] Bloco6Request request)
    {
        var updated = await service.UpdateBloco6Async(id, request);
        if (!updated) return NotFound(new { error = "Não encontrado." });

        return Ok(new UpdateResponse(true));
    }

    [HttpDelete("{id:int}")]
    [Authorize]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await service.DeleteAsync(id);
        if (!deleted) return NotFound(new { error = "Não encontrado." });

        return Ok(new UpdateResponse(true));
    }

    [HttpPatch("{id:int}/cadastro")]
    [Authorize]
    public async Task<IActionResult> EditarCadastro(int id, [FromBody] EditarCadastroRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Nome))
            return BadRequest(new { error = "Nome é obrigatório." });

        var updated = await service.EditarCadastroAsync(id, request);
        if (!updated) return NotFound(new { error = "Não encontrado." });

        return Ok(new UpdateResponse(true));
    }

    [HttpPut("{id:int}/mentor")]
    [Authorize]
    public async Task<IActionResult> UpdateMentor(int id, [FromBody] MentorRequest request)
    {
        var updated = await service.UpdateMentorAsync(id, request);
        if (!updated) return NotFound(new { error = "Não encontrado." });

        return Ok(new UpdateResponse(true));
    }

    [HttpPost("{id:int}/whatsapp/reenviar")]
    [Authorize]
    public async Task<IActionResult> ReenviarWhatsapp(int id)
    {
        var resultado = await service.ReenviarWhatsappAsync(id);

        if (!resultado.Sucesso)
            return BadRequest(new { error = resultado.MensagemErro });

        return Ok(new { success = true, messageId = resultado.MessageId });
    }

    [HttpPatch("{id:int}/fase")]
    [Authorize]
    public async Task<IActionResult> UpdateFase(int id, [FromBody] FaseRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Fase))
            return BadRequest(new { error = "Fase obrigatória." });

        var updated = await service.UpdateFaseAsync(id, request.Fase);
        if (!updated) return NotFound(new { error = "Não encontrado." });

        return Ok(new UpdateResponse(true));
    }

    [HttpPost("convidado")]
    [Authorize]
    public async Task<IActionResult> CriarConvidado([FromBody] CriarConvidadoRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Nome))
            return BadRequest(new { error = "Nome é obrigatório." });

        var result = await service.CriarConvidadoAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPost("{id:int}/pdf")]
    [Authorize]
    public async Task<IActionResult> GerarPdf(int id)
    {
        var (path, error) = await service.GerarPdfAsync(id);
        if (path is null) return NotFound(new { error });
        return Ok(new { success = true });
    }

    [HttpGet("{id:int}/pdf/download")]
    [Authorize]
    public async Task<IActionResult> DownloadPdf(int id)
    {
        var submission = await service.GetByIdAsync(id);
        if (submission?.DevolutivaPdfPath is null || !System.IO.File.Exists(submission.DevolutivaPdfPath))
            return NotFound(new { error = "PDF não encontrado. Gere primeiro." });

        var bytes = await System.IO.File.ReadAllBytesAsync(submission.DevolutivaPdfPath);
        var nomeArquivo = $"Devolutiva-{string.Concat(submission.Nome.Split(' ').Take(2))}.pdf";
        return File(bytes, "application/pdf", nomeArquivo);
    }

    [HttpPost("{id:int}/enviar-devolutiva")]
    [Authorize]
    public async Task<IActionResult> EnviarDevolutiva(int id)
    {
        var resultado = await service.EnviarDevolutivaAsync(id);
        if (!resultado.Sucesso)
            return BadRequest(new { error = resultado.MensagemErro });
        return Ok(new { success = true });
    }
}
