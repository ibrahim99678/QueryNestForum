namespace QueryNest.Web.Models.Dashboard;

public class UserDashboardViewModel
{
    public string Name { get; set; } = default!;
    public int Reputation { get; set; }
    public int FollowedTags { get; set; }
    public int UnreadNotifications { get; set; }

    public int MyQuestions { get; set; }
    public int MyAnswers { get; set; }
    public int MyComments { get; set; }
    public int VotesReceived { get; set; }

    public List<DashboardDayCountViewModel> MyQuestionsLast14Days { get; set; } = [];
    public List<DashboardDayCountViewModel> MyAnswersLast14Days { get; set; } = [];
    public bool CanViewAdmin { get; set; }
}
