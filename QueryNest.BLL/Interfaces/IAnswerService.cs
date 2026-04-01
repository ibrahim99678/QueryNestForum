using QueryNest.Contract.Answers;
using QueryNest.Contract.Auth;

namespace QueryNest.BLL.Interfaces;

public interface IAnswerService
{
    Task<(AuthResultDto Result, int? AnswerId)> CreateAsync(string aspNetUserId, AnswerCreateRequestDto request, CancellationToken cancellationToken = default);
    Task<(AuthResultDto Result, AnswerEditDto? Answer)> GetForEditAsync(string aspNetUserId, int answerId, CancellationToken cancellationToken = default);
    Task<(AuthResultDto Result, int? QuestionId)> UpdateAsync(string aspNetUserId, int answerId, AnswerUpdateRequestDto request, CancellationToken cancellationToken = default);
    Task<(AuthResultDto Result, int? QuestionId)> DeleteAsync(string aspNetUserId, int answerId, CancellationToken cancellationToken = default);
}
