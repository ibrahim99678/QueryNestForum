namespace QueryNest.Domain.Entities;

public class Answer
{
    public int AnswerId { get; set; }
    public int QuestionId { get; set; }
    public int UserId { get; set; }
    public string Content { get; set; } = default!;
    public DateTime CreatedAt { get; set; }

    public Question Question { get; set; } = default!;
    public UserProfile User { get; set; } = default!;

    public ICollection<Comment> Comments { get; set; } = new List<Comment>();
    public ICollection<Vote> Votes { get; set; } = new List<Vote>();
}
