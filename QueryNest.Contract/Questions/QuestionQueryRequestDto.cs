namespace QueryNest.Contract.Questions;

public class QuestionQueryRequestDto
{
    public string? Query { get; init; }
    public int? CategoryId { get; init; }
    public int? TagId { get; init; }
    public string Sort { get; init; } = "latest";
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}
