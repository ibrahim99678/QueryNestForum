using QueryNest.Contract.Auth;
using QueryNest.Contract.Questions;

namespace QueryNest.BLL.Interfaces;

public interface IQuestionService
{
    Task<List<QuestionListItemDto>> GetLatestAsync(int take = 50, CancellationToken cancellationToken = default);
    Task<QuestionDetailsDto?> GetDetailsAsync(int questionId, bool incrementViewCount = true, CancellationToken cancellationToken = default);
    Task<QuestionUpsertDataDto> GetUpsertDataAsync(CancellationToken cancellationToken = default);
    Task<QuestionUpsertRequestDto?> GetForEditAsync(int questionId, CancellationToken cancellationToken = default);
    Task<(AuthResultDto Result, int? QuestionId)> CreateAsync(string aspNetUserId, QuestionUpsertRequestDto request, CancellationToken cancellationToken = default);
    Task<AuthResultDto> UpdateAsync(string aspNetUserId, int questionId, QuestionUpsertRequestDto request, CancellationToken cancellationToken = default);
    Task<AuthResultDto> DeleteAsync(string aspNetUserId, int questionId, CancellationToken cancellationToken = default);
}
