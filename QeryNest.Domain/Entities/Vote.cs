using QueryNest.Domain.Enums;

namespace QueryNest.Domain.Entities;

public class Vote
{
    public int VoteId { get; set; }
    public int UserId { get; set; }
    public int? QuestionId { get; set; }
    public int? AnswerId { get; set; }
    public int? CommentId { get; set; }
    public VoteType VoteType { get; set; }
    public DateTime CreatedAt { get; set; }

    public UserProfile User { get; set; } = default!;
    public Question? Question { get; set; }
    public Answer? Answer { get; set; }
    public Comment? Comment { get; set; }
}
