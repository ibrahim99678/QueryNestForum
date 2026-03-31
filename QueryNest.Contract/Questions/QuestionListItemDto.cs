namespace QueryNest.Contract.Questions;

public class QuestionListItemDto
{
    public int QuestionId { get; init; }
    public string Title { get; init; } = default!;
    public string CategoryName { get; init; } = default!;
    public string AuthorName { get; init; } = default!;
    public int ViewCount { get; init; }
    public int Score { get; init; }
    public int AnswerCount { get; init; }
    public DateTime CreatedAt { get; init; }
}
