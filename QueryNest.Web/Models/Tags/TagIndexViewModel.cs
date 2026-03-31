namespace QueryNest.Web.Models.Tags;

public class TagIndexViewModel
{
    public string? Query { get; set; }
    public List<TagListItemViewModel> Tags { get; set; } = [];
    public List<TagListItemViewModel> FollowedTags { get; set; } = [];
    public bool IsAuthenticated { get; set; }
    public bool CanManage { get; set; }
}
