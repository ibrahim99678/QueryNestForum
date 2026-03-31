using QueryNest.Contract.Answers;
using QueryNest.Contract.Tags;

namespace QueryNest.Contract.Questions;

public class QuestionDetailsDto
{
    public int QuestionId { get; init; }
    public string Title { get; init; } = default!;
    public string Description { get; init; } = default!;
    public string CategoryName { get; init; } = default!;
    public string AuthorName { get; init; } = default!;
    public string? AuthorAvatarPath { get; init; }
    public int AuthorUserId { get; init; }
    public int ViewCount { get; init; }
    public int Score { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public IReadOnlyList<TagDto> Tags { get; init; } = [];
    public IReadOnlyList<AnswerDto> Answers { get; init; } = [];
}
