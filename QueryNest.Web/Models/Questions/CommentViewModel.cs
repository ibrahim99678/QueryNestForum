namespace QueryNest.Web.Models.Questions;

public class CommentViewModel
{
    public int CommentId { get; set; }
    public int? ParentCommentId { get; set; }
    public string AuthorName { get; set; } = default!;
    public string Content { get; set; } = default!;
    public DateTime CreatedAt { get; set; }
    public List<CommentViewModel> Replies { get; set; } = [];
    public bool CanEdit { get; set; }
}
