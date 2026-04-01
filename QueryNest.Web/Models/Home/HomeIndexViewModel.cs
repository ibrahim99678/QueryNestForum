using QueryNest.Web.Models.Questions;
using QueryNest.Web.Models.Tags;
using QueryNest.Web.Models.Categories;

namespace QueryNest.Web.Models.Home;

public class HomeIndexViewModel
{
    public bool IsAuthenticated { get; set; }

    public int TotalUsers { get; set; }
    public int TotalQuestions { get; set; }
    public int TotalAnswers { get; set; }
    public int TotalTags { get; set; }

    public List<TagListItemViewModel> TrendingTags { get; set; } = [];
    public List<CategoryListItemViewModel> PopularCategories { get; set; } = [];
    public List<QuestionListItemViewModel> LatestQuestions { get; set; } = [];
}
