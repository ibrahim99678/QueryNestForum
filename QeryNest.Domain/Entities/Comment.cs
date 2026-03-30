namespace QueryNest.Domain.Entities;

public class Comment
{
    public int CommentId { get; set; }
    public int AnswerId { get; set; }
    public int UserId { get; set; }
    public int? ParentCommentId { get; set; }
    public string Content { get; set; } = default!;
    public DateTime CreatedAt { get; set; }

    public Answer Answer { get; set; } = default!;
    public UserProfile User { get; set; } = default!;

    public Comment? ParentComment { get; set; }
    public ICollection<Comment> Replies { get; set; } = new List<Comment>();
    public ICollection<Vote> Votes { get; set; } = new List<Vote>();
}
