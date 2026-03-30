using QueryNest.Contract.Auth;
using QueryNest.Contract.Comments;

namespace QueryNest.BLL.Interfaces;

public interface ICommentService
{
    Task<(AuthResultDto Result, int? QuestionId)> CreateAsync(string aspNetUserId, CommentCreateRequestDto request, CancellationToken cancellationToken = default);
    Task<CommentEditDto?> GetForEditAsync(int commentId, CancellationToken cancellationToken = default);
    Task<(AuthResultDto Result, int? QuestionId)> UpdateAsync(string aspNetUserId, int commentId, CommentUpdateRequestDto request, CancellationToken cancellationToken = default);
    Task<(AuthResultDto Result, int? QuestionId)> DeleteAsync(string aspNetUserId, int commentId, CancellationToken cancellationToken = default);
}
