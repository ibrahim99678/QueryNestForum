namespace QueryNest.Domain.Entities;

public class Question
{
    public int QuestionId { get; set; }
    public int UserId { get; set; }
    public int CategoryId { get; set; }
    public string Title { get; set; } = default!;
    public string Description { get; set; } = default!;
    public int ViewCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public UserProfile User { get; set; } = default!;
    public Category Category { get; set; } = default!;

    public ICollection<QuestionTag> QuestionTags { get; set; } = new List<QuestionTag>();
    public ICollection<Answer> Answers { get; set; } = new List<Answer>();
    public ICollection<Vote> Votes { get; set; } = new List<Vote>();
}
