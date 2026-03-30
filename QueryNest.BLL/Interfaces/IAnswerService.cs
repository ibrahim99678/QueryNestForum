using QueryNest.Contract.Answers;
using QueryNest.Contract.Auth;

namespace QueryNest.BLL.Interfaces;

public interface IAnswerService
{
    Task<(AuthResultDto Result, int? AnswerId)> CreateAsync(string aspNetUserId, AnswerCreateRequestDto request, CancellationToken cancellationToken = default);
}
