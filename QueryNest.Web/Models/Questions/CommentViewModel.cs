namespace QueryNest.Web.Models.Questions;

public class CommentViewModel
{
    public int CommentId { get; set; }
    public int? ParentCommentId { get; set; }
    public string AuthorName { get; set; } = default!;
    public string? AuthorAvatarPath { get; set; }
    public string Content { get; set; } = default!;
    public int Score { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<CommentViewModel> Replies { get; set; } = [];
    public bool CanEdit { get; set; }
}
