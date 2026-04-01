using Microsoft.AspNetCore.Mvc.Rendering;

namespace QueryNest.Web.Models.Questions;

public class QuestionIndexViewModel
{
    public List<QuestionListItemViewModel> Items { get; set; } = [];

    public string? Query { get; set; }
    public int? CategoryId { get; set; }
    public int? TagId { get; set; }
    public string Sort { get; set; } = "latest";

    public int Page { get; set; }
    public int TotalPages { get; set; }
    public int TotalCount { get; set; }

    public List<SelectListItem> CategoryOptions { get; set; } = [];
    public List<SelectListItem> TagOptions { get; set; } = [];
    public List<SelectListItem> SortOptions { get; set; } =
    [
        new SelectListItem("Latest", "latest"),
        new SelectListItem("Trending", "trending")
    ];
}
