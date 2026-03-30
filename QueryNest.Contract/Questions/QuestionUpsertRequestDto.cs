namespace QueryNest.Contract.Questions;

public class QuestionUpsertRequestDto
{
    public string Title { get; init; } = default!;
    public string Description { get; init; } = default!;
    public int CategoryId { get; init; }
    public int[] TagIds { get; init; } = [];
    public string? NewTagsCsv { get; init; }
}
