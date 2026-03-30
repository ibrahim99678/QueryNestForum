namespace QueryNest.Domain.Entities;

public class UserProfile
{
    public int UserId { get; set; }
    public string AspNetUserId { get; set; } = default!;
    public string Name { get; set; } = default!;
    public int Reputation { get; set; }
    public DateTime CreatedAt { get; set; }

    public ICollection<Question> Questions { get; set; } = new List<Question>();
    public ICollection<Answer> Answers { get; set; } = new List<Answer>();
    public ICollection<Comment> Comments { get; set; } = new List<Comment>();
    public ICollection<Vote> Votes { get; set; } = new List<Vote>();
}
