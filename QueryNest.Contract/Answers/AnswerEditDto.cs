namespace QueryNest.Contract.Answers;

public class AnswerEditDto
{
    public int AnswerId { get; init; }
    public int QuestionId { get; init; }
    public string Content { get; init; } = default!;
}
