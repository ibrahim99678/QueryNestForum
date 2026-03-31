using Microsoft.EntityFrameworkCore;
using QueryNest.BLL.Interfaces;
using QueryNest.Contract.Dashboard;
using QueryNest.Contract.Tags;
using QueryNest.DAL.Interfaces;
using QueryNest.Domain.Enums;

namespace QueryNest.BLL.Services;

public class DashboardService : IDashboardService
{
    private readonly IUnitOfWork _unitOfWork;

    public DashboardService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<UserDashboardDto?> GetUserDashboardAsync(string aspNetUserId, CancellationToken cancellationToken = default)
    {
        var profile = await _unitOfWork.Users.Query()
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.AspNetUserId == aspNetUserId, cancellationToken);

        if (profile is null)
        {
            return null;
        }

        var userId = profile.UserId;

        var followedTags = await _unitOfWork.TagFollows.Query()
            .AsNoTracking()
            .CountAsync(tf => tf.UserId == userId, cancellationToken);

        var unreadNotifications = await _unitOfWork.Notifications.Query()
            .AsNoTracking()
            .CountAsync(n => n.UserId == userId && !n.IsRead, cancellationToken);

        var myQuestions = await _unitOfWork.Questions.Query()
            .AsNoTracking()
            .CountAsync(q => q.UserId == userId, cancellationToken);

        var myAnswers = await _unitOfWork.Answers.Query()
            .AsNoTracking()
            .CountAsync(a => a.UserId == userId, cancellationToken);

        var myComments = await _unitOfWork.Comments.Query()
            .AsNoTracking()
            .CountAsync(c => c.UserId == userId, cancellationToken);

        var questionVoteSum = await _unitOfWork.Votes.Query()
            .AsNoTracking()
            .Where(v => v.QuestionId != null)
            .Join(_unitOfWork.Questions.Query().AsNoTracking().Where(q => q.UserId == userId),
                v => v.QuestionId!.Value,
                q => q.QuestionId,
                (v, q) => (int?)v.VoteType)
            .SumAsync(cancellationToken) ?? 0;

        var answerVoteSum = await _unitOfWork.Votes.Query()
            .AsNoTracking()
            .Where(v => v.AnswerId != null)
            .Join(_unitOfWork.Answers.Query().AsNoTracking().Where(a => a.UserId == userId),
                v => v.AnswerId!.Value,
                a => a.AnswerId,
                (v, a) => (int?)v.VoteType)
            .SumAsync(cancellationToken) ?? 0;

        var commentVoteSum = await _unitOfWork.Votes.Query()
            .AsNoTracking()
            .Where(v => v.CommentId != null)
            .Join(_unitOfWork.Comments.Query().AsNoTracking().Where(c => c.UserId == userId),
                v => v.CommentId!.Value,
                c => c.CommentId,
                (v, c) => (int?)v.VoteType)
            .SumAsync(cancellationToken) ?? 0;

        var start = DateTime.UtcNow.Date.AddDays(-13);

        var myQuestionsSeries = await BuildDaySeriesAsync(
            start,
            _unitOfWork.Questions.Query().AsNoTracking().Where(q => q.UserId == userId),
            q => q.CreatedAt,
            cancellationToken);

        var myAnswersSeries = await BuildDaySeriesAsync(
            start,
            _unitOfWork.Answers.Query().AsNoTracking().Where(a => a.UserId == userId),
            a => a.CreatedAt,
            cancellationToken);

        return new UserDashboardDto
        {
            UserId = userId,
            Name = profile.Name,
            Reputation = profile.Reputation,
            FollowedTags = followedTags,
            UnreadNotifications = unreadNotifications,
            MyQuestions = myQuestions,
            MyAnswers = myAnswers,
            MyComments = myComments,
            VotesReceived = questionVoteSum + answerVoteSum + commentVoteSum,
            MyQuestionsLast14Days = myQuestionsSeries,
            MyAnswersLast14Days = myAnswersSeries
        };
    }

    public async Task<AdminDashboardDto> GetAdminDashboardAsync(CancellationToken cancellationToken = default)
    {
        var totalUsers = await _unitOfWork.Users.Query().AsNoTracking().CountAsync(cancellationToken);
        var totalQuestions = await _unitOfWork.Questions.Query().AsNoTracking().CountAsync(cancellationToken);
        var totalAnswers = await _unitOfWork.Answers.Query().AsNoTracking().CountAsync(cancellationToken);
        var totalComments = await _unitOfWork.Comments.Query().AsNoTracking().CountAsync(cancellationToken);
        var totalTags = await _unitOfWork.Tags.Query().AsNoTracking().CountAsync(cancellationToken);
        var pendingReports = await _unitOfWork.Reports.Query().AsNoTracking().CountAsync(r => r.Status == ReportStatus.Pending, cancellationToken);

        var start = DateTime.UtcNow.Date.AddDays(-13);

        var questionsSeries = await BuildDaySeriesAsync(start, _unitOfWork.Questions.Query().AsNoTracking(), q => q.CreatedAt, cancellationToken);
        var answersSeries = await BuildDaySeriesAsync(start, _unitOfWork.Answers.Query().AsNoTracking(), a => a.CreatedAt, cancellationToken);
        var commentsSeries = await BuildDaySeriesAsync(start, _unitOfWork.Comments.Query().AsNoTracking(), c => c.CreatedAt, cancellationToken);

        var topTagsRaw = await _unitOfWork.Tags.Query()
            .AsNoTracking()
            .OrderByDescending(t => t.Followers.Count)
            .ThenByDescending(t => t.QuestionTags.Count)
            .Take(10)
            .Select(t => new TagSummaryDto
            {
                TagId = t.TagId,
                Name = t.Name,
                Slug = t.Slug,
                QuestionCount = t.QuestionTags.Count,
                FollowerCount = t.Followers.Count,
                IsFollowedByCurrentUser = false
            })
            .ToListAsync(cancellationToken);

        var topUsers = await _unitOfWork.Users.Query()
            .AsNoTracking()
            .OrderByDescending(u => u.Reputation)
            .Take(10)
            .Select(u => new TopUserDto
            {
                UserId = u.UserId,
                Name = u.Name,
                Reputation = u.Reputation,
                Questions = u.Questions.Count,
                Answers = u.Answers.Count
            })
            .ToListAsync(cancellationToken);

        return new AdminDashboardDto
        {
            TotalUsers = totalUsers,
            TotalQuestions = totalQuestions,
            TotalAnswers = totalAnswers,
            TotalComments = totalComments,
            TotalTags = totalTags,
            PendingReports = pendingReports,
            QuestionsLast14Days = questionsSeries,
            AnswersLast14Days = answersSeries,
            CommentsLast14Days = commentsSeries,
            TopTags = topTagsRaw,
            TopUsers = topUsers
        };
    }

    private static async Task<List<DashboardDayCountDto>> BuildDaySeriesAsync<T>(
        DateTime startDateUtc,
        IQueryable<T> query,
        System.Linq.Expressions.Expression<Func<T, DateTime>> dateSelector,
        CancellationToken cancellationToken)
        where T : class
    {
        var start = startDateUtc.Date;
        var endExclusive = start.AddDays(14);

        var data = await query
            .Where(x => EF.Property<DateTime>(x, ((System.Linq.Expressions.MemberExpression)dateSelector.Body).Member.Name) >= start &&
                        EF.Property<DateTime>(x, ((System.Linq.Expressions.MemberExpression)dateSelector.Body).Member.Name) < endExclusive)
            .Select(x => EF.Functions.DateDiffDay(start, EF.Property<DateTime>(x, ((System.Linq.Expressions.MemberExpression)dateSelector.Body).Member.Name)))
            .GroupBy(x => x)
            .Select(g => new { DayOffset = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var map = data.ToDictionary(x => x.DayOffset, x => x.Count);
        var result = new List<DashboardDayCountDto>(14);
        for (var i = 0; i < 14; i++)
        {
            map.TryGetValue(i, out var count);
            result.Add(new DashboardDayCountDto { Date = start.AddDays(i), Count = count });
        }

        return result;
    }
}
