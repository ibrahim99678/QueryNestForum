using QueryNest.Web.Models.Tags;

namespace QueryNest.Web.Models.Dashboard;

public class AdminDashboardViewModel
{
    public int TotalUsers { get; set; }
    public int TotalQuestions { get; set; }
    public int TotalAnswers { get; set; }
    public int TotalComments { get; set; }
    public int TotalTags { get; set; }
    public int PendingReports { get; set; }

    public List<DashboardDayCountViewModel> QuestionsLast14Days { get; set; } = [];
    public List<DashboardDayCountViewModel> AnswersLast14Days { get; set; } = [];
    public List<DashboardDayCountViewModel> CommentsLast14Days { get; set; } = [];

    public List<TagListItemViewModel> TopTags { get; set; } = [];
    public List<TopUserViewModel> TopUsers { get; set; } = [];
}
