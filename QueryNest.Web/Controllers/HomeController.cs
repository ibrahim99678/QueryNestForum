using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QueryNest.BLL.Interfaces;
using QueryNest.DAL.Interfaces;
using QueryNest.Web.Models;
using QueryNest.Web.Models.Home;
using QueryNest.Web.Models.Questions;
using QueryNest.Web.Models.Tags;

namespace QueryNest.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IQuestionService _questionService;
        private readonly ITagService _tagService;
        private readonly ICacheService _cacheService;

        public HomeController(ILogger<HomeController> logger, IUnitOfWork unitOfWork, IQuestionService questionService, ITagService tagService, ICacheService cacheService)
        {
            _logger = logger;
            _unitOfWork = unitOfWork;
            _questionService = questionService;
            _tagService = tagService;
            _cacheService = cacheService;
        }

        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            var aspNetUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var totals = await _cacheService.GetOrSetAsync(
                "home:totals",
                TimeSpan.FromSeconds(30),
                async ct =>
                {
                    var totalUsers = await _unitOfWork.Users.Query().AsNoTracking().CountAsync(ct);
                    var totalQuestions = await _unitOfWork.Questions.Query().AsNoTracking().CountAsync(ct);
                    var totalAnswers = await _unitOfWork.Answers.Query().AsNoTracking().CountAsync(ct);
                    var totalTags = await _unitOfWork.Tags.Query().AsNoTracking().CountAsync(ct);
                    return new HomeTotals
                    {
                        TotalUsers = totalUsers,
                        TotalQuestions = totalQuestions,
                        TotalAnswers = totalAnswers,
                        TotalTags = totalTags
                    };
                },
                cancellationToken);

            var questions = await _questionService.QueryAsync(
                new QueryNest.Contract.Questions.QuestionQueryRequestDto
                {
                    Sort = "latest",
                    Page = 1,
                    PageSize = 6
                },
                cancellationToken);

            var tags = await _tagService.GetTrendingAsync(aspNetUserId, 10, cancellationToken);
            var trendingTags = tags.Select(t => new TagListItemViewModel
            {
                TagId = t.TagId,
                Name = t.Name,
                Slug = t.Slug,
                QuestionCount = t.QuestionCount,
                FollowerCount = t.FollowerCount,
                IsFollowed = t.IsFollowedByCurrentUser
            }).ToList();

            var popularCategories = await _cacheService.GetOrSetAsync(
                "home:popularCategories:8",
                TimeSpan.FromSeconds(60),
                async ct =>
                {
                    return await _unitOfWork.Categories.Query()
                        .AsNoTracking()
                        .OrderByDescending(c => c.Questions.Count)
                        .ThenBy(c => c.Name)
                        .Take(8)
                        .Select(c => new QueryNest.Web.Models.Categories.CategoryListItemViewModel
                        {
                            CategoryId = c.CategoryId,
                            Name = c.Name,
                            Description = c.Description,
                            QuestionCount = c.Questions.Count
                        })
                        .ToListAsync(ct);
                },
                cancellationToken);

            var model = new HomeIndexViewModel
            {
                IsAuthenticated = User.Identity?.IsAuthenticated == true,
                TotalUsers = totals.TotalUsers,
                TotalQuestions = totals.TotalQuestions,
                TotalAnswers = totals.TotalAnswers,
                TotalTags = totals.TotalTags,
                TrendingTags = trendingTags,
                PopularCategories = popularCategories,
                LatestQuestions = questions.Items.Select(q => new QuestionListItemViewModel
                {
                    QuestionId = q.QuestionId,
                    Title = q.Title,
                    CategoryName = q.CategoryName,
                    AuthorName = q.AuthorName,
                    ViewCount = q.ViewCount,
                    Score = q.Score,
                    AnswerCount = q.AnswerCount,
                    CreatedAt = q.CreatedAt
                }).ToList()
            };

            return View(model);
        }

        private sealed class HomeTotals
        {
            public int TotalUsers { get; init; }
            public int TotalQuestions { get; init; }
            public int TotalAnswers { get; init; }
            public int TotalTags { get; init; }
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
