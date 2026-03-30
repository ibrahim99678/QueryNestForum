namespace QueryNest.Web.Models.Questions;

public class QuestionListItemViewModel
{
    public int QuestionId { get; set; }
    public string Title { get; set; } = default!;
    public string CategoryName { get; set; } = default!;
    public string AuthorName { get; set; } = default!;
    public int ViewCount { get; set; }
    public DateTime CreatedAt { get; set; }
}
