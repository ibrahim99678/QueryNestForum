using QueryNest.Contract.Tags;

namespace QueryNest.Contract.Dashboard;

public class AdminDashboardDto
{
    public int TotalUsers { get; init; }
    public int TotalQuestions { get; init; }
    public int TotalAnswers { get; init; }
    public int TotalComments { get; init; }
    public int TotalTags { get; init; }
    public int PendingReports { get; init; }

    public IReadOnlyList<DashboardDayCountDto> QuestionsLast14Days { get; init; } = [];
    public IReadOnlyList<DashboardDayCountDto> AnswersLast14Days { get; init; } = [];
    public IReadOnlyList<DashboardDayCountDto> CommentsLast14Days { get; init; } = [];

    public IReadOnlyList<TagSummaryDto> TopTags { get; init; } = [];
    public IReadOnlyList<TopUserDto> TopUsers { get; init; } = [];
}
