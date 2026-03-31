using QueryNest.Contract.Comments;

namespace QueryNest.Contract.Answers;

public class AnswerDto
{
    public int AnswerId { get; init; }
    public int AuthorUserId { get; init; }
    public string AuthorName { get; init; } = default!;
    public string Content { get; init; } = default!;
    public int Score { get; init; }
    public DateTime CreatedAt { get; init; }
    public IReadOnlyList<CommentDto> Comments { get; init; } = [];
}
