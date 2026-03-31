namespace QueryNest.Contract.Tags;

public class TagSummaryDto
{
    public int TagId { get; init; }
    public string Name { get; init; } = default!;
    public string Slug { get; init; } = default!;
    public int QuestionCount { get; init; }
    public int FollowerCount { get; init; }
    public bool IsFollowedByCurrentUser { get; init; }
}
