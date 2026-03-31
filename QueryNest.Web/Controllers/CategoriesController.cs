using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QueryNest.BLL.Interfaces;
using QueryNest.Contract.Categories;
using QueryNest.Web.Models.Categories;
using QueryNest.DAL.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace QueryNest.Web.Controllers;

[Authorize(Roles = "Admin,Moderator")]
public class CategoriesController : Controller
{
    private readonly ICategoryService _categoryService;
    private readonly IUnitOfWork _unitOfWork;

    public CategoriesController(ICategoryService categoryService, IUnitOfWork unitOfWork)
    {
        _categoryService = categoryService;
        _unitOfWork = unitOfWork;
    }

    [HttpGet]
    public async Task<IActionResult> Manage(CancellationToken cancellationToken)
    {
        var categories = await _unitOfWork.Categories.Query()
            .OrderBy(c => c.Name)
            .Select(c => new CategoryListItemViewModel
            {
                CategoryId = c.CategoryId,
                Name = c.Name,
                Description = c.Description,
                QuestionCount = c.Questions.Count
            })
            .ToListAsync(cancellationToken);

        return View(categories);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new CategoryUpsertViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CategoryUpsertViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await _categoryService.CreateAsync(
            new CategoryUpsertRequestDto { Name = model.Name, Description = model.Description },
            cancellationToken);

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

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var category = await _unitOfWork.Categories.Query().FirstOrDefaultAsync(c => c.CategoryId == id, cancellationToken);
        if (category is null)
        {
            return NotFound();
        }

        return View(new CategoryUpsertViewModel
        {
            CategoryId = category.CategoryId,
            Name = category.Name,
            Description = category.Description
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(CategoryUpsertViewModel model, CancellationToken cancellationToken)
    {
        if (model.CategoryId is null)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await _categoryService.UpdateAsync(
            model.CategoryId.Value,
            new CategoryUpsertRequestDto { Name = model.Name, Description = model.Description },
            cancellationToken);

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

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var result = await _categoryService.DeleteAsync(id, cancellationToken);
        if (!result.Succeeded)
        {
            TempData["Error"] = string.Join(" ", result.Errors);
        }
        return RedirectToAction(nameof(Manage));
    }
}
