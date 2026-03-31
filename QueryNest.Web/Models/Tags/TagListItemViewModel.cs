namespace QueryNest.Web.Models.Tags;

public class TagListItemViewModel
{
    public int TagId { get; set; }
    public string Name { get; set; } = default!;
    public string Slug { get; set; } = default!;
    public int QuestionCount { get; set; }
    public int FollowerCount { get; set; }
    public bool IsFollowed { get; set; }
}
