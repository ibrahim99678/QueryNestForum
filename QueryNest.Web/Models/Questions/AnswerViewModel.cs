namespace QueryNest.Web.Models.Questions;

public class AnswerViewModel
{
    public int AnswerId { get; set; }
    public string AuthorName { get; set; } = default!;
    public string? AuthorAvatarPath { get; set; }
    public string Content { get; set; } = default!;
    public int Score { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<CommentViewModel> Comments { get; set; } = [];
    public bool CanComment { get; set; }
    public bool CanEdit { get; set; }
    public bool CanDelete { get; set; }
}
