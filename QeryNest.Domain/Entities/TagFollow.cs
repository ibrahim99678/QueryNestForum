namespace QueryNest.Domain.Entities;

public class TagFollow
{
    public int UserId { get; set; }
    public int TagId { get; set; }
    public DateTime CreatedAt { get; set; }

    public UserProfile User { get; set; } = default!;
    public Tag Tag { get; set; } = default!;
}
