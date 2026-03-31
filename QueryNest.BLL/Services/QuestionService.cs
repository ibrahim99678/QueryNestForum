using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using QueryNest.BLL.Interfaces;
using QueryNest.Contract.Answers;
using QueryNest.Contract.Auth;
using QueryNest.Contract.Categories;
using QueryNest.Contract.Comments;
using QueryNest.Contract.Common;
using QueryNest.Contract.Questions;
using QueryNest.Contract.Tags;
using QueryNest.DAL.Interfaces;
using QueryNest.Domain.Entities;

namespace QueryNest.BLL.Services;

public class QuestionService : IQuestionService
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly IUnitOfWork _unitOfWork;

    public QuestionService(UserManager<IdentityUser> userManager, IUnitOfWork unitOfWork)
    {
        _userManager = userManager;
        _unitOfWork = unitOfWork;
    }

    public async Task<List<QuestionListItemDto>> GetLatestAsync(int take = 50, CancellationToken cancellationToken = default)
    {
        if (take <= 0)
        {
            take = 50;
        }

        return await _unitOfWork.Questions.Query()
            .AsNoTracking()
            .OrderByDescending(q => q.CreatedAt)
            .Take(take)
            .Select(q => new QuestionListItemDto
            {
                QuestionId = q.QuestionId,
                Title = q.Title,
                CategoryName = q.Category.Name,
                AuthorName = q.User.Name,
                ViewCount = q.ViewCount,
                Score = _unitOfWork.Votes.Query()
                    .Where(v => v.QuestionId == q.QuestionId)
                    .Sum(v => (int?)v.VoteType) ?? 0,
                AnswerCount = _unitOfWork.Answers.Query().Count(a => a.QuestionId == q.QuestionId),
                CreatedAt = q.CreatedAt
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<PagedResultDto<QuestionListItemDto>> QueryAsync(QuestionQueryRequestDto request, CancellationToken cancellationToken = default)
    {
        var pageSize = request.PageSize <= 0 ? 20 : Math.Min(request.PageSize, 50);
        var page = request.Page <= 0 ? 1 : request.Page;
        var sort = string.IsNullOrWhiteSpace(request.Sort) ? "latest" : request.Sort.Trim().ToLowerInvariant();
        var tagId = request.TagId;
        var queryText = string.IsNullOrWhiteSpace(request.Query) ? null : request.Query.Trim();

        var query = _unitOfWork.Questions.Query()
            .AsNoTracking()
            .AsQueryable();

        if (tagId is not null && tagId.Value > 0)
        {
            query = query.Where(q => q.QuestionTags.Any(qt => qt.TagId == tagId.Value));
        }

        if (!string.IsNullOrWhiteSpace(queryText))
        {
            var terms = queryText
                .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(t => t.Length > 0)
                .Take(5)
                .ToArray();

            foreach (var term in terms)
            {
                var pattern = $"%{term}%";
                query = query.Where(q =>
                    EF.Functions.Like(q.Title, pattern) ||
                    EF.Functions.Like(q.Description, pattern));
            }
        }

        query = sort switch
        {
            "trending" => query
                .OrderByDescending(q => _unitOfWork.Votes.Query()
                    .Where(v => v.QuestionId == q.QuestionId)
                    .Sum(v => (int?)v.VoteType) ?? 0)
                .ThenByDescending(q => q.CreatedAt),
            _ => query.OrderByDescending(q => q.CreatedAt)
        };

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(q => new QuestionListItemDto
            {
                QuestionId = q.QuestionId,
                Title = q.Title,
                CategoryName = q.Category.Name,
                AuthorName = q.User.Name,
                ViewCount = q.ViewCount,
                Score = _unitOfWork.Votes.Query()
                    .Where(v => v.QuestionId == q.QuestionId)
                    .Sum(v => (int?)v.VoteType) ?? 0,
                AnswerCount = _unitOfWork.Answers.Query().Count(a => a.QuestionId == q.QuestionId),
                CreatedAt = q.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return new PagedResultDto<QuestionListItemDto>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task<QuestionDetailsDto?> GetDetailsAsync(int questionId, bool incrementViewCount = true, CancellationToken cancellationToken = default)
    {
        Question? question;
        if (incrementViewCount)
        {
            question = await _unitOfWork.Questions.GetDetailsAsync(questionId, cancellationToken);
            if (question is null)
            {
                return null;
            }

            question.ViewCount += 1;
            _unitOfWork.Questions.Update(question);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        else
        {
            question = await _unitOfWork.Questions.Query()
                .AsNoTracking()
                .Include(q => q.User)
                .Include(q => q.Category)
                .Include(q => q.Votes)
                .Include(q => q.QuestionTags)
                .ThenInclude(qt => qt.Tag)
                .FirstOrDefaultAsync(q => q.QuestionId == questionId, cancellationToken);

            if (question is null)
            {
                return null;
            }
        }

        return new QuestionDetailsDto
        {
            QuestionId = question.QuestionId,
            Title = question.Title,
            Description = question.Description,
            CategoryName = question.Category.Name,
            AuthorName = question.User.Name,
            AuthorAvatarPath = question.User.AvatarPath,
            AuthorUserId = question.UserId,
            ViewCount = question.ViewCount,
            Score = question.Votes.Select(v => (int)v.VoteType).DefaultIfEmpty(0).Sum(),
            CreatedAt = question.CreatedAt,
            UpdatedAt = question.UpdatedAt,
            Tags = question.QuestionTags
                .Select(qt => new TagDto { TagId = qt.TagId, Name = qt.Tag.Name })
                .ToList(),
            Answers = question.Answers
                .OrderBy(a => a.CreatedAt)
                .Select(a => new AnswerDto
                {
                    AnswerId = a.AnswerId,
                    AuthorUserId = a.UserId,
                    AuthorName = a.User.Name,
                    AuthorAvatarPath = a.User.AvatarPath,
                    Content = a.Content,
                    Score = a.Votes.Select(v => (int)v.VoteType).DefaultIfEmpty(0).Sum(),
                    CreatedAt = a.CreatedAt,
                    Comments = BuildCommentTree(a.Comments)
                })
                .ToList()
        };
    }

    public async Task<QuestionUpsertDataDto> GetUpsertDataAsync(CancellationToken cancellationToken = default)
    {
        var categories = await _unitOfWork.Categories.Query()
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .Select(c => new CategoryDto { CategoryId = c.CategoryId, Name = c.Name })
            .ToListAsync(cancellationToken);

        var tags = await _unitOfWork.Tags.Query()
            .AsNoTracking()
            .OrderBy(t => t.Name)
            .Select(t => new TagDto { TagId = t.TagId, Name = t.Name })
            .ToListAsync(cancellationToken);

        return new QuestionUpsertDataDto
        {
            Categories = categories,
            Tags = tags
        };
    }

    public async Task<QuestionUpsertRequestDto?> GetForEditAsync(int questionId, CancellationToken cancellationToken = default)
    {
        var question = await _unitOfWork.Questions.Query()
            .AsNoTracking()
            .Include(q => q.QuestionTags)
            .FirstOrDefaultAsync(q => q.QuestionId == questionId, cancellationToken);

        if (question is null)
        {
            return null;
        }

        return new QuestionUpsertRequestDto
        {
            Title = question.Title,
            Description = question.Description,
            CategoryId = question.CategoryId,
            TagIds = question.QuestionTags.Select(qt => qt.TagId).ToArray()
        };
    }

    public async Task<(AuthResultDto Result, int? QuestionId)> CreateAsync(string aspNetUserId, QuestionUpsertRequestDto request, CancellationToken cancellationToken = default)
    {
        var userProfile = await GetUserProfileAsync(aspNetUserId, cancellationToken);
        if (userProfile is null)
        {
            return (AuthResultDto.Failed("User profile not found."), null);
        }

        if (!await _unitOfWork.Categories.Query().AnyAsync(c => c.CategoryId == request.CategoryId, cancellationToken))
        {
            return (AuthResultDto.Failed("Invalid category."), null);
        }

        var question = new Question
        {
            UserId = userProfile.UserId,
            CategoryId = request.CategoryId,
            Title = request.Title.Trim(),
            Description = request.Description.Trim(),
            ViewCount = 0,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Questions.AddAsync(question, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await ReplaceTagsAsync(question.QuestionId, request.TagIds, request.NewTagsCsv, cancellationToken);

        return (AuthResultDto.Success(), question.QuestionId);
    }

    public async Task<AuthResultDto> UpdateAsync(string aspNetUserId, int questionId, QuestionUpsertRequestDto request, CancellationToken cancellationToken = default)
    {
        var userProfile = await GetUserProfileAsync(aspNetUserId, cancellationToken);
        if (userProfile is null)
        {
            return AuthResultDto.Failed("User profile not found.");
        }

        var question = await _unitOfWork.Questions.Query()
            .FirstOrDefaultAsync(q => q.QuestionId == questionId, cancellationToken);

        if (question is null)
        {
            return AuthResultDto.Failed("Question not found.");
        }

        if (question.UserId != userProfile.UserId && !await IsInRoleAsync(aspNetUserId, "Admin"))
        {
            return AuthResultDto.Failed("Not allowed.");
        }

        if (!await _unitOfWork.Categories.Query().AnyAsync(c => c.CategoryId == request.CategoryId, cancellationToken))
        {
            return AuthResultDto.Failed("Invalid category.");
        }

        question.Title = request.Title.Trim();
        question.Description = request.Description.Trim();
        question.CategoryId = request.CategoryId;
        question.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.Questions.Update(question);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await ReplaceTagsAsync(question.QuestionId, request.TagIds, request.NewTagsCsv, cancellationToken);

        return AuthResultDto.Success();
    }

    public async Task<AuthResultDto> DeleteAsync(string aspNetUserId, int questionId, CancellationToken cancellationToken = default)
    {
        var userProfile = await GetUserProfileAsync(aspNetUserId, cancellationToken);
        if (userProfile is null)
        {
            return AuthResultDto.Failed("User profile not found.");
        }

        var question = await _unitOfWork.Questions.Query()
            .FirstOrDefaultAsync(q => q.QuestionId == questionId, cancellationToken);

        if (question is null)
        {
            return AuthResultDto.Failed("Question not found.");
        }

        if (question.UserId != userProfile.UserId && !await IsInRoleAsync(aspNetUserId, "Admin"))
        {
            return AuthResultDto.Failed("Not allowed.");
        }

        _unitOfWork.Questions.Remove(question);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return AuthResultDto.Success();
    }

    private async Task<UserProfile?> GetUserProfileAsync(string aspNetUserId, CancellationToken cancellationToken)
    {
        return await _unitOfWork.Users.Query()
            .FirstOrDefaultAsync(x => x.AspNetUserId == aspNetUserId, cancellationToken);
    }

    private async Task<bool> IsInRoleAsync(string aspNetUserId, string role)
    {
        var user = await _userManager.FindByIdAsync(aspNetUserId);
        if (user is null)
        {
            return false;
        }

        return await _userManager.IsInRoleAsync(user, role);
    }

    private async Task ReplaceTagsAsync(int questionId, int[] tagIds, string? newTagsCsv, CancellationToken cancellationToken)
    {
        var normalizedExistingIds = (tagIds ?? []).Distinct().Where(x => x > 0).ToHashSet();
        var newTagNames = ParseCsv(newTagsCsv);

        foreach (var name in newTagNames)
        {
            var slug = ToSlug(name);
            var existing = await _unitOfWork.Tags.Query()
                .FirstOrDefaultAsync(t => t.Slug == slug, cancellationToken);

            if (existing is not null)
            {
                normalizedExistingIds.Add(existing.TagId);
                continue;
            }

            var tag = new Tag
            {
                Name = name,
                Slug = slug,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Tags.AddAsync(tag, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            normalizedExistingIds.Add(tag.TagId);
        }

        var existingLinks = await _unitOfWork.QuestionTags.Query()
            .Where(qt => qt.QuestionId == questionId)
            .ToListAsync(cancellationToken);

        foreach (var link in existingLinks)
        {
            _unitOfWork.QuestionTags.Remove(link);
        }

        foreach (var tagId in normalizedExistingIds)
        {
            await _unitOfWork.QuestionTags.AddAsync(
                new QuestionTag
                {
                    QuestionId = questionId,
                    TagId = tagId
                },
                cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private static List<string> ParseCsv(string? csv)
    {
        if (string.IsNullOrWhiteSpace(csv))
        {
            return [];
        }

        return csv
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(x => x.Trim())
            .Where(x => x.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(x => x.Length > 50 ? x[..50] : x)
            .ToList();
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

    private static IReadOnlyList<CommentDto> BuildCommentTree(IEnumerable<Comment> comments)
    {
        var nodes = comments
            .OrderBy(c => c.CreatedAt)
            .Select(c => new CommentNode(
                new CommentDto
                {
                    CommentId = c.CommentId,
                    ParentCommentId = c.ParentCommentId,
                    AuthorUserId = c.UserId,
                    AuthorName = c.User.Name,
                    AuthorAvatarPath = c.User.AvatarPath,
                    Content = c.Content,
                    Score = c.Votes.Select(v => (int)v.VoteType).DefaultIfEmpty(0).Sum(),
                    CreatedAt = c.CreatedAt
                }))
            .ToDictionary(n => n.Dto.CommentId);

        var roots = new List<CommentNode>();

        foreach (var node in nodes.Values.OrderBy(n => n.Dto.CreatedAt))
        {
            if (node.Dto.ParentCommentId is null)
            {
                roots.Add(node);
                continue;
            }

            if (nodes.TryGetValue(node.Dto.ParentCommentId.Value, out var parent))
            {
                parent.Replies.Add(node);
            }
            else
            {
                roots.Add(node);
            }
        }

        return roots
            .OrderBy(r => r.Dto.CreatedAt)
            .Select(ToDto)
            .ToList();
    }

    private static CommentDto ToDto(CommentNode node)
    {
        return new CommentDto
        {
            CommentId = node.Dto.CommentId,
            ParentCommentId = node.Dto.ParentCommentId,
            AuthorUserId = node.Dto.AuthorUserId,
            AuthorName = node.Dto.AuthorName,
            Content = node.Dto.Content,
            CreatedAt = node.Dto.CreatedAt,
            Replies = node.Replies
                .OrderBy(r => r.Dto.CreatedAt)
                .Select(ToDto)
                .ToList()
        };
    }

    private sealed class CommentNode
    {
        public CommentNode(CommentDto dto)
        {
            Dto = dto;
        }

        public CommentDto Dto { get; }
        public List<CommentNode> Replies { get; } = [];
    }
}
