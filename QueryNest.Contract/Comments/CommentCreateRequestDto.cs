namespace QueryNest.Contract.Comments;

public class CommentCreateRequestDto
{
    public int AnswerId { get; init; }
    public int? ParentCommentId { get; init; }
    public string Content { get; init; } = default!;
}
