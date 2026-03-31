namespace QueryNest.Web.Models.Tags;

public class TagDetailsViewModel
{
    public TagListItemViewModel Tag { get; set; } = default!;
    public List<QueryNest.Web.Models.Questions.QuestionListItemViewModel> Questions { get; set; } = [];
    public int Page { get; set; }
    public int TotalPages { get; set; }
    public int TotalCount { get; set; }
}
