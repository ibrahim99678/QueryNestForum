namespace QueryNest.Contract.Comments;

public class CommentEditDto
{
    public int CommentId { get; init; }
    public int AnswerId { get; init; }
    public int QuestionId { get; init; }
    public string Content { get; init; } = default!;
}
