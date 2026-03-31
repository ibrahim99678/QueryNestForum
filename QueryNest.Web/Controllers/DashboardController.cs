using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QueryNest.BLL.Interfaces;
using QueryNest.Web.Models.Dashboard;
using QueryNest.Web.Models.Tags;

namespace QueryNest.Web.Controllers;

[Authorize]
public class DashboardController : Controller
{
    private readonly IDashboardService _dashboardService;

    public DashboardController(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var aspNetUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(aspNetUserId))
        {
            return RedirectToAction("Login", "Account");
        }

        var dto = await _dashboardService.GetUserDashboardAsync(aspNetUserId, cancellationToken);
        if (dto is null)
        {
            return RedirectToAction("Login", "Account");
        }

        var model = new UserDashboardViewModel
        {
            Name = dto.Name,
            Reputation = dto.Reputation,
            FollowedTags = dto.FollowedTags,
            UnreadNotifications = dto.UnreadNotifications,
            MyQuestions = dto.MyQuestions,
            MyAnswers = dto.MyAnswers,
            MyComments = dto.MyComments,
            VotesReceived = dto.VotesReceived,
            MyQuestionsLast14Days = dto.MyQuestionsLast14Days.Select(x => new DashboardDayCountViewModel { Date = x.Date, Count = x.Count }).ToList(),
            MyAnswersLast14Days = dto.MyAnswersLast14Days.Select(x => new DashboardDayCountViewModel { Date = x.Date, Count = x.Count }).ToList(),
            CanViewAdmin = User.IsInRole("Admin") || User.IsInRole("Moderator")
        };

        return View(model);
    }

    [Authorize(Roles = "Admin,Moderator")]
    [HttpGet]
    public async Task<IActionResult> Admin(CancellationToken cancellationToken)
    {
        var dto = await _dashboardService.GetAdminDashboardAsync(cancellationToken);

        var model = new AdminDashboardViewModel
        {
            TotalUsers = dto.TotalUsers,
            TotalQuestions = dto.TotalQuestions,
            TotalAnswers = dto.TotalAnswers,
            TotalComments = dto.TotalComments,
            TotalTags = dto.TotalTags,
            PendingReports = dto.PendingReports,
            QuestionsLast14Days = dto.QuestionsLast14Days.Select(x => new DashboardDayCountViewModel { Date = x.Date, Count = x.Count }).ToList(),
            AnswersLast14Days = dto.AnswersLast14Days.Select(x => new DashboardDayCountViewModel { Date = x.Date, Count = x.Count }).ToList(),
            CommentsLast14Days = dto.CommentsLast14Days.Select(x => new DashboardDayCountViewModel { Date = x.Date, Count = x.Count }).ToList(),
            TopTags = dto.TopTags.Select(t => new TagListItemViewModel
            {
                TagId = t.TagId,
                Name = t.Name,
                Slug = t.Slug,
                QuestionCount = t.QuestionCount,
                FollowerCount = t.FollowerCount,
                IsFollowed = false
            }).ToList(),
            TopUsers = dto.TopUsers.Select(u => new TopUserViewModel
            {
                UserId = u.UserId,
                Name = u.Name,
                Reputation = u.Reputation,
                Questions = u.Questions,
                Answers = u.Answers
            }).ToList()
        };

        return View(model);
    }
}

