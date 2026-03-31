using QueryNest.Contract.Auth;
using QueryNest.Contract.Votes;

namespace QueryNest.BLL.Interfaces;

public interface IVoteService
{
    Task<(AuthResultDto Result, int? QuestionId)> CastAsync(string aspNetUserId, CastVoteRequestDto request, CancellationToken cancellationToken = default);
}
