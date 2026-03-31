namespace QueryNest.Domain.Entities;

public class Tag
{
    public int TagId { get; set; }
    public string Name { get; set; } = default!;
    public string Slug { get; set; } = default!;
    public DateTime CreatedAt { get; set; }

    public ICollection<QuestionTag> QuestionTags { get; set; } = new List<QuestionTag>();
    public ICollection<TagFollow> Followers { get; set; } = new List<TagFollow>();
}
