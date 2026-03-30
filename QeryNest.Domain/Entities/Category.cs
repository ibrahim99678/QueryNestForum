namespace QueryNest.Domain.Entities;

public class Category
{
    public int CategoryId { get; set; }
    public string Name { get; set; } = default!;
    public string Slug { get; set; } = default!;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }

    public ICollection<Question> Questions { get; set; } = new List<Question>();
}
