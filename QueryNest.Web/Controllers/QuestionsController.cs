using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using QueryNest.BLL.Interfaces;
using QueryNest.Contract.Questions;
using QueryNest.Web.Models.Questions;

namespace QueryNest.Web.Controllers;

public class QuestionsController : Controller
{
    private readonly IQuestionService _questionService;
    private readonly IProfileService _profileService;

    public QuestionsController(IQuestionService questionService, IProfileService profileService)
    {
        _questionService = questionService;
        _profileService = profileService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? q, int? categoryId, int? tagId, string? sort, int page = 1, CancellationToken cancellationToken = default)
    {
        var queryResult = await _questionService.QueryAsync(
            new QueryNest.Contract.Questions.QuestionQueryRequestDto
            {
                Query = q,
                CategoryId = categoryId,
                TagId = tagId,
                Sort = sort ?? "latest",
                Page = page,
                PageSize = 20
            },
            cancellationToken);

        var data = await _questionService.GetUpsertDataAsync(cancellationToken);
        var categoryOptions = new List<SelectListItem>
        {
            new SelectListItem("All categories", string.Empty, categoryId is null)
        };
        categoryOptions.AddRange(data.Categories.Select(c => new SelectListItem(c.Name, c.CategoryId.ToString(), categoryId == c.CategoryId)));

        var tagOptions = new List<SelectListItem>
        {
            new SelectListItem("All tags", string.Empty, tagId is null)
        };
        tagOptions.AddRange(data.Tags.Select(t => new SelectListItem(t.Name, t.TagId.ToString(), tagId == t.TagId)));

        var model = new QuestionIndexViewModel
        {
            Items = queryResult.Items.Select(x => new QuestionListItemViewModel
            {
                QuestionId = x.QuestionId,
                Title = x.Title,
                CategoryName = x.CategoryName,
                AuthorName = x.AuthorName,
                ViewCount = x.ViewCount,
                Score = x.Score,
                AnswerCount = x.AnswerCount,
                CreatedAt = x.CreatedAt
            }).ToList(),
            Query = q,
            CategoryId = categoryId,
            TagId = tagId,
            Sort = (sort ?? "latest").Trim().ToLowerInvariant(),
            Page = queryResult.Page,
            TotalPages = queryResult.TotalPages,
            TotalCount = queryResult.TotalCount,
            CategoryOptions = categoryOptions,
            TagOptions = tagOptions,
            SortOptions = new List<SelectListItem>
            {
                new SelectListItem("Latest", "latest", string.Equals(sort, "latest", StringComparison.OrdinalIgnoreCase) || string.IsNullOrWhiteSpace(sort)),
                new SelectListItem("Trending", "trending", string.Equals(sort, "trending", StringComparison.OrdinalIgnoreCase))
            }
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        var dto = await _questionService.GetDetailsAsync(id, true, cancellationToken);
        if (dto is null)
        {
            return NotFound();
        }

        var canEdit = false;
        var isAuthenticated = User.Identity?.IsAuthenticated == true;
        var isAdmin = isAuthenticated && User.IsInRole("Admin");
        int? currentProfileUserId = null;
        if (User.Identity?.IsAuthenticated == true)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrWhiteSpace(userId))
            {
                var profile = await _profileService.GetProfileAsync(userId, cancellationToken);
                if (profile is not null)
                {
                    currentProfileUserId = profile.UserId;
                }

                canEdit = isAdmin || (currentProfileUserId is not null && currentProfileUserId.Value == dto.AuthorUserId);
            }
        }

        var model = new QuestionDetailsViewModel
        {
            QuestionId = dto.QuestionId,
            Title = dto.Title,
            Description = dto.Description,
            CategoryName = dto.CategoryName,
            AuthorName = dto.AuthorName,
            AuthorAvatarPath = dto.AuthorAvatarPath,
            ViewCount = dto.ViewCount,
            Score = dto.Score,
            CreatedAt = dto.CreatedAt,
            UpdatedAt = dto.UpdatedAt,
            Tags = dto.Tags.Select(t => t.Name).ToList(),
            CanEdit = canEdit,
            CanAnswer = isAuthenticated,
            Answers = dto.Answers.Select(a => new AnswerViewModel
            {
                AnswerId = a.AnswerId,
                AuthorName = a.AuthorName,
                AuthorAvatarPath = a.AuthorAvatarPath,
                Content = a.Content,
                Score = a.Score,
                CreatedAt = a.CreatedAt,
                CanComment = isAuthenticated,
                CanEdit = isAdmin || (currentProfileUserId is not null && currentProfileUserId.Value == a.AuthorUserId),
                CanDelete = isAdmin || (currentProfileUserId is not null && currentProfileUserId.Value == a.AuthorUserId),
                Comments = a.Comments.Select(c => MapComment(c, currentProfileUserId, isAdmin)).ToList()
            }).ToList()
        };

        return View(model);
    }

    private static CommentViewModel MapComment(QueryNest.Contract.Comments.CommentDto dto, int? currentProfileUserId, bool isAdmin)
    {
        var canEdit = isAdmin || (currentProfileUserId is not null && currentProfileUserId.Value == dto.AuthorUserId);

        return new CommentViewModel
        {
            CommentId = dto.CommentId,
            ParentCommentId = dto.ParentCommentId,
            AuthorName = dto.AuthorName,
            AuthorAvatarPath = dto.AuthorAvatarPath,
            Content = dto.Content,
            Score = dto.Score,
            CreatedAt = dto.CreatedAt,
            CanEdit = canEdit,
            Replies = dto.Replies.Select(r => MapComment(r, currentProfileUserId, isAdmin)).ToList()
        };
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        var data = await _questionService.GetUpsertDataAsync(cancellationToken);
        var model = new QuestionUpsertViewModel
        {
            Categories = data.Categories.Select(c => new SelectListItem(c.Name, c.CategoryId.ToString())).ToList(),
            Tags = data.Tags.Select(t => new SelectListItem(t.Name, t.TagId.ToString())).ToList()
        };

        return View(model);
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(QuestionUpsertViewModel model, CancellationToken cancellationToken)
    {
        var data = await _questionService.GetUpsertDataAsync(cancellationToken);
        model.Categories = data.Categories.Select(c => new SelectListItem(c.Name, c.CategoryId.ToString())).ToList();
        model.Tags = data.Tags.Select(t => new SelectListItem(t.Name, t.TagId.ToString())).ToList();

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return RedirectToAction("Login", "Account");
        }

        var (result, questionId) = await _questionService.CreateAsync(
            userId,
            new QuestionUpsertRequestDto
            {
                Title = model.Title,
                Description = model.Description,
                CategoryId = model.CategoryId,
                TagIds = model.SelectedTagIds,
                NewTagsCsv = model.NewTagsCsv
            },
            cancellationToken);

        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error);
            }

            return View(model);
        }

        return RedirectToAction(nameof(Details), new { id = questionId });
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var dto = await _questionService.GetForEditAsync(id, cancellationToken);
        if (dto is null)
        {
            return NotFound();
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return RedirectToAction("Login", "Account");
        }

        var details = await _questionService.GetDetailsAsync(id, false, cancellationToken);
        if (details is null)
        {
            return NotFound();
        }

        var canEdit = User.IsInRole("Admin");
        if (!canEdit)
        {
            var profile = await _profileService.GetProfileAsync(userId, cancellationToken);
            canEdit = profile is not null && profile.UserId == details.AuthorUserId;
        }

        if (!canEdit)
        {
            return Forbid();
        }

        var data = await _questionService.GetUpsertDataAsync(cancellationToken);
        var model = new QuestionUpsertViewModel
        {
            QuestionId = id,
            Title = dto.Title,
            Description = dto.Description,
            CategoryId = dto.CategoryId,
            SelectedTagIds = dto.TagIds,
            Categories = data.Categories.Select(c => new SelectListItem(c.Name, c.CategoryId.ToString(), c.CategoryId == dto.CategoryId)).ToList(),
            Tags = data.Tags.Select(t => new SelectListItem(t.Name, t.TagId.ToString(), dto.TagIds.Contains(t.TagId))).ToList()
        };

        return View(model);
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(QuestionUpsertViewModel model, CancellationToken cancellationToken)
    {
        if (model.QuestionId is null)
        {
            return BadRequest();
        }

        var data = await _questionService.GetUpsertDataAsync(cancellationToken);
        model.Categories = data.Categories.Select(c => new SelectListItem(c.Name, c.CategoryId.ToString(), c.CategoryId == model.CategoryId)).ToList();
        model.Tags = data.Tags.Select(t => new SelectListItem(t.Name, t.TagId.ToString(), model.SelectedTagIds.Contains(t.TagId))).ToList();

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return RedirectToAction("Login", "Account");
        }

        var result = await _questionService.UpdateAsync(
            userId,
            model.QuestionId.Value,
            new QuestionUpsertRequestDto
            {
                Title = model.Title,
                Description = model.Description,
                CategoryId = model.CategoryId,
                TagIds = model.SelectedTagIds,
                NewTagsCsv = model.NewTagsCsv
            },
            cancellationToken);

        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error);
            }

            return View(model);
        }

        return RedirectToAction(nameof(Details), new { id = model.QuestionId.Value });
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var dto = await _questionService.GetDetailsAsync(id, false, cancellationToken);
        if (dto is null)
        {
            return NotFound();
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return RedirectToAction("Login", "Account");
        }

        var canEdit = User.IsInRole("Admin");
        if (!canEdit)
        {
            var profile = await _profileService.GetProfileAsync(userId, cancellationToken);
            canEdit = profile is not null && profile.UserId == dto.AuthorUserId;
        }

        if (!canEdit)
        {
            return Forbid();
        }

        var model = new QuestionDetailsViewModel
        {
            QuestionId = dto.QuestionId,
            Title = dto.Title,
            Description = dto.Description,
            CategoryName = dto.CategoryName,
            AuthorName = dto.AuthorName,
            ViewCount = dto.ViewCount,
            CreatedAt = dto.CreatedAt,
            UpdatedAt = dto.UpdatedAt,
            Tags = dto.Tags.Select(t => t.Name).ToList(),
            CanEdit = true
        };

        return View(model);
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return RedirectToAction("Login", "Account");
        }

        var result = await _questionService.DeleteAsync(userId, id, cancellationToken);
        if (!result.Succeeded)
        {
            return RedirectToAction(nameof(Details), new { id });
        }

        return RedirectToAction(nameof(Index));
    }
}
