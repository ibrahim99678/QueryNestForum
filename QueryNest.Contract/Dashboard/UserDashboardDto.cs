namespace QueryNest.Contract.Dashboard;

public class UserDashboardDto
{
    public int UserId { get; init; }
    public string Name { get; init; } = default!;
    public int Reputation { get; init; }
    public int FollowedTags { get; init; }
    public int UnreadNotifications { get; init; }

    public int MyQuestions { get; init; }
    public int MyAnswers { get; init; }
    public int MyComments { get; init; }

    public int VotesReceived { get; init; }

    public IReadOnlyList<DashboardDayCountDto> MyQuestionsLast14Days { get; init; } = [];
    public IReadOnlyList<DashboardDayCountDto> MyAnswersLast14Days { get; init; } = [];
}
