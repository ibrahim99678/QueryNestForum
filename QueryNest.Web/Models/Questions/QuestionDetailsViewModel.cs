namespace QueryNest.Web.Models.Questions;

public class QuestionDetailsViewModel
{
    public int QuestionId { get; set; }
    public string Title { get; set; } = default!;
    public string Description { get; set; } = default!;
    public string CategoryName { get; set; } = default!;
    public string AuthorName { get; set; } = default!;
    public int ViewCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public List<string> Tags { get; set; } = [];
    public bool CanEdit { get; set; }
    public List<AnswerViewModel> Answers { get; set; } = [];
    public bool CanAnswer { get; set; }
}
