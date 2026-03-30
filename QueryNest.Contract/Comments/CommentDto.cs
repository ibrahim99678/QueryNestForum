namespace QueryNest.Contract.Comments;

public class CommentDto
{
    public int CommentId { get; init; }
    public int? ParentCommentId { get; init; }
    public int AuthorUserId { get; init; }
    public string AuthorName { get; init; } = default!;
    public string Content { get; init; } = default!;
    public DateTime CreatedAt { get; init; }
    public IReadOnlyList<CommentDto> Replies { get; init; } = [];
}
