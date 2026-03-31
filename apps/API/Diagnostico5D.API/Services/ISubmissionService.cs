using Diagnostico5D.API.DTOs;

namespace Diagnostico5D.API.Services;

public interface ISubmissionService
{
    Task<IEnumerable<SubmissionDto>> GetAllAsync();
    Task<SubmissionDto?> GetByIdAsync(int id);
    Task<LookupResponse> LookupByWhatsappAsync(string whatsapp);
    Task<CreateResponse> CreateAsync(CreateSubmissionRequest request);
    Task<bool> UpdateAsync(int id, UpdateSubmissionRequest request);
    Task<bool> UpdateBloco6Async(int id, Bloco6Request request);
    Task<bool> DeleteAsync(int id);
    Task<bool> EditarCadastroAsync(int id, EditarCadastroRequest request);
    Task<bool> UpdateMentorAsync(int id, MentorRequest request);
    Task<EvolutionApiResult> ReenviarWhatsappAsync(int id);
}
