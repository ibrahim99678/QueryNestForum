namespace QueryNest.Contract.Answers;

public class AnswerCreateRequestDto
{
    public int QuestionId { get; init; }
    public string Content { get; init; } = default!;
}
