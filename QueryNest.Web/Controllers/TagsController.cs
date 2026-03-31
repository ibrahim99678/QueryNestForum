using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QueryNest.BLL.Interfaces;
using QueryNest.Contract.Tags;
using QueryNest.Web.Models.Tags;

namespace QueryNest.Web.Controllers;

public class TagsController : Controller
{
    private readonly ITagService _tagService;
    private readonly IQuestionService _questionService;

    public TagsController(ITagService tagService, IQuestionService questionService)
    {
        _tagService = tagService;
        _questionService = questionService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? q, CancellationToken cancellationToken)
    {
        var aspNetUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var tags = await _tagService.GetAllAsync(aspNetUserId, q, cancellationToken);
        var followed = new List<TagSummaryDto>();
        if (User.Identity?.IsAuthenticated == true && !string.IsNullOrWhiteSpace(aspNetUserId))
        {
            followed = await _tagService.GetFollowedAsync(aspNetUserId, cancellationToken);
        }

        var model = new TagIndexViewModel
        {
            Query = q,
            IsAuthenticated = User.Identity?.IsAuthenticated == true,
            CanManage = User.IsInRole("Admin") || User.IsInRole("Moderator"),
            FollowedTags = followed.Select(MapTag).ToList(),
            Tags = tags.Select(MapTag).ToList()
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id, int page = 1, CancellationToken cancellationToken = default)
    {
        var aspNetUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var tag = await _tagService.GetByIdAsync(id, aspNetUserId, cancellationToken);
        if (tag is null)
        {
            return NotFound();
        }

        var result = await _questionService.QueryAsync(
            new QueryNest.Contract.Questions.QuestionQueryRequestDto
            {
                TagId = id,
                Sort = "latest",
                Page = page,
                PageSize = 20
            },
            cancellationToken);

        var model = new TagDetailsViewModel
        {
            Tag = MapTag(tag),
            Questions = result.Items.Select(x => new QueryNest.Web.Models.Questions.QuestionListItemViewModel
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
            Page = result.Page,
            TotalPages = result.TotalPages,
            TotalCount = result.TotalCount
        };

        return View(model);
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleFollow(int id, string? returnUrl, CancellationToken cancellationToken)
    {
        var aspNetUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(aspNetUserId))
        {
            return RedirectToAction("Login", "Account");
        }

        await _tagService.ToggleFollowAsync(aspNetUserId, id, cancellationToken);

        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    [Authorize(Roles = "Admin,Moderator")]
    [HttpGet]
    public async Task<IActionResult> Manage(CancellationToken cancellationToken)
    {
        var tags = await _tagService.GetAllAsync(null, null, cancellationToken);
        var model = tags.Select(MapTag).ToList();
        return View(model);
    }

    [Authorize(Roles = "Admin,Moderator")]
    [HttpGet]
    public IActionResult Create()
    {
        return View(new TagUpsertViewModel());
    }

    [Authorize(Roles = "Admin,Moderator")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(TagUpsertViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await _tagService.CreateAsync(new TagUpsertRequestDto { Name = model.Name }, cancellationToken);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error);
            }

            return View(model);
        }

        return RedirectToAction(nameof(Manage));
    }

    [Authorize(Roles = "Admin,Moderator")]
    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var tag = await _tagService.GetByIdAsync(id, null, cancellationToken);
        if (tag is null)
        {
            return NotFound();
        }

        return View(new TagUpsertViewModel { TagId = id, Name = tag.Name });
    }

    [Authorize(Roles = "Admin,Moderator")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(TagUpsertViewModel model, CancellationToken cancellationToken)
    {
        if (model.TagId is null)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await _tagService.UpdateAsync(model.TagId.Value, new TagUpsertRequestDto { Name = model.Name }, cancellationToken);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error);
            }

            return View(model);
        }

        return RedirectToAction(nameof(Manage));
    }

    [Authorize(Roles = "Admin,Moderator")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await _tagService.DeleteAsync(id, cancellationToken);
        return RedirectToAction(nameof(Manage));
    }

    private static TagListItemViewModel MapTag(TagSummaryDto t)
    {
        return new TagListItemViewModel
        {
            TagId = t.TagId,
            Name = t.Name,
            Slug = t.Slug,
            QuestionCount = t.QuestionCount,
            FollowerCount = t.FollowerCount,
            IsFollowed = t.IsFollowedByCurrentUser
        };
    }
}

