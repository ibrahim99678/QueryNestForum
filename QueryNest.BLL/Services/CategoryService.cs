using Microsoft.EntityFrameworkCore;
using QueryNest.BLL.Interfaces;
using QueryNest.Contract.Auth;
using QueryNest.Contract.Categories;
using QueryNest.DAL.Interfaces;
using QueryNest.Domain.Entities;

namespace QueryNest.BLL.Services;

public class CategoryService : ICategoryService
{
    private readonly IUnitOfWork _unitOfWork;

    public CategoryService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<List<CategoryDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _unitOfWork.Categories.Query()
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .Select(c => new CategoryDto { CategoryId = c.CategoryId, Name = c.Name })
            .ToListAsync(cancellationToken);
    }

    public async Task<CategoryDto?> GetByIdAsync(int categoryId, CancellationToken cancellationToken = default)
    {
        return await _unitOfWork.Categories.Query()
            .AsNoTracking()
            .Where(c => c.CategoryId == categoryId)
            .Select(c => new CategoryDto { CategoryId = c.CategoryId, Name = c.Name })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<AuthResultDto> CreateAsync(CategoryUpsertRequestDto request, CancellationToken cancellationToken = default)
    {
        var name = NormalizeName(request.Name);
        if (name is null)
        {
            return AuthResultDto.Failed("Invalid category name.");
        }

        var slug = ToSlug(name);
        var exists = await _unitOfWork.Categories.Query()
            .AsNoTracking()
            .AnyAsync(c => c.Slug == slug, cancellationToken);

        if (exists)
        {
            return AuthResultDto.Failed("Category already exists.");
        }

        await _unitOfWork.Categories.AddAsync(
            new Category
            {
                Name = name,
                Slug = slug,
                Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
                CreatedAt = DateTime.UtcNow
            },
            cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return AuthResultDto.Success();
    }

    public async Task<AuthResultDto> UpdateAsync(int categoryId, CategoryUpsertRequestDto request, CancellationToken cancellationToken = default)
    {
        var category = await _unitOfWork.Categories.Query().FirstOrDefaultAsync(c => c.CategoryId == categoryId, cancellationToken);
        if (category is null)
        {
            return AuthResultDto.Failed("Category not found.");
        }

        var name = NormalizeName(request.Name);
        if (name is null)
        {
            return AuthResultDto.Failed("Invalid category name.");
        }

        var slug = ToSlug(name);
        var exists = await _unitOfWork.Categories.Query()
            .AsNoTracking()
            .AnyAsync(c => c.CategoryId != categoryId && c.Slug == slug, cancellationToken);

        if (exists)
        {
            return AuthResultDto.Failed("Another category already uses this name.");
        }

        category.Name = name;
        category.Slug = slug;
        category.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        _unitOfWork.Categories.Update(category);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return AuthResultDto.Success();
    }

    public async Task<AuthResultDto> DeleteAsync(int categoryId, CancellationToken cancellationToken = default)
    {
        var category = await _unitOfWork.Categories.Query().FirstOrDefaultAsync(c => c.CategoryId == categoryId, cancellationToken);
        if (category is null)
        {
            return AuthResultDto.Failed("Category not found.");
        }

        var hasQuestions = await _unitOfWork.Questions.Query()
            .AsNoTracking()
            .AnyAsync(q => q.CategoryId == categoryId, cancellationToken);

        if (hasQuestions)
        {
            return AuthResultDto.Failed("Cannot delete a category with questions.");
        }

        _unitOfWork.Categories.Remove(category);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return AuthResultDto.Success();
    }

    private static string? NormalizeName(string? name)
    {
        var trimmed = (name ?? string.Empty).Trim();
        if (trimmed.Length < 2)
        {
            return null;
        }

        if (trimmed.Length > 50)
        {
            trimmed = trimmed[..50];
        }

        return trimmed;
    }

    private static string ToSlug(string value)
    {
        var chars = value.Trim().ToLowerInvariant().ToCharArray();
        var result = new List<char>(chars.Length);
        var lastWasDash = false;

        foreach (var c in chars)
        {
            if (char.IsLetterOrDigit(c))
            {
                result.Add(c);
                lastWasDash = false;
                continue;
            }

            if (c == ' ' || c == '-' || c == '_')
            {
                if (!lastWasDash && result.Count > 0)
                {
                    result.Add('-');
                    lastWasDash = true;
                }
            }
        }

        while (result.Count > 0 && result[^1] == '-')
        {
            result.RemoveAt(result.Count - 1);
        }

        if (result.Count == 0)
        {
            return Guid.NewGuid().ToString("N");
        }

        var slug = new string(result.ToArray());
        return slug.Length > 50 ? slug[..50] : slug;
    }
}
