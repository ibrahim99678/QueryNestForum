using Microsoft.EntityFrameworkCore;
using QueryNest.BLL.Interfaces;
using QueryNest.Contract.Auth;
using QueryNest.Contract.Tags;
using QueryNest.DAL.Interfaces;
using QueryNest.Domain.Entities;

namespace QueryNest.BLL.Services;

public class TagService : ITagService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICacheService _cacheService;

    public TagService(IUnitOfWork unitOfWork, ICacheService cacheService)
    {
        _unitOfWork = unitOfWork;
        _cacheService = cacheService;
    }

    public async Task<List<TagSummaryDto>> GetAllAsync(string? aspNetUserId, string? query = null, CancellationToken cancellationToken = default)
    {
        var normalized = string.IsNullOrWhiteSpace(query) ? null : query.Trim();
        var userProfileId = await GetUserProfileIdAsync(aspNetUserId, cancellationToken);

        var tagsQuery = _unitOfWork.Tags.Query().AsNoTracking();
        if (!string.IsNullOrWhiteSpace(normalized))
        {
            var pattern = $"%{normalized}%";
            tagsQuery = tagsQuery.Where(t => EF.Functions.Like(t.Name, pattern));
        }

        var tags = await tagsQuery
            .OrderBy(t => t.Name)
            .Select(t => new
            {
                t.TagId,
                t.Name,
                t.Slug,
                QuestionCount = t.QuestionTags.Count,
                FollowerCount = t.Followers.Count
            })
            .ToListAsync(cancellationToken);

        HashSet<int> followed = [];
        if (userProfileId is not null)
        {
            var followedIds = await _unitOfWork.TagFollows.Query()
                .AsNoTracking()
                .Where(tf => tf.UserId == userProfileId.Value)
                .Select(tf => tf.TagId)
                .ToListAsync(cancellationToken);

            followed = followedIds.ToHashSet();
        }

        return tags.Select(t => new TagSummaryDto
        {
            TagId = t.TagId,
            Name = t.Name,
            Slug = t.Slug,
            QuestionCount = t.QuestionCount,
            FollowerCount = t.FollowerCount,
            IsFollowedByCurrentUser = followed.Contains(t.TagId)
        }).ToList();
    }

    public async Task<List<TagSummaryDto>> GetTrendingAsync(string? aspNetUserId, int take = 10, CancellationToken cancellationToken = default)
    {
        if (take <= 0)
        {
            take = 10;
        }

        take = Math.Min(take, 20);
        var key = $"tags:trending:{take}";

        var tags = await _cacheService.GetOrSetAsync(
            key,
            TimeSpan.FromSeconds(60),
            async ct =>
            {
                return await _unitOfWork.Tags.Query()
                    .AsNoTracking()
                    .OrderByDescending(t => t.Followers.Count)
                    .ThenByDescending(t => t.QuestionTags.Count)
                    .Take(take)
                    .Select(t => new TagSummaryDto
                    {
                        TagId = t.TagId,
                        Name = t.Name,
                        Slug = t.Slug,
                        QuestionCount = t.QuestionTags.Count,
                        FollowerCount = t.Followers.Count,
                        IsFollowedByCurrentUser = false
                    })
                    .ToListAsync(ct);
            },
            cancellationToken);

        if (string.IsNullOrWhiteSpace(aspNetUserId))
        {
            return tags;
        }

        var userProfileId = await GetUserProfileIdAsync(aspNetUserId, cancellationToken);
        if (userProfileId is null || tags.Count == 0)
        {
            return tags;
        }

        var tagIds = tags.Select(t => t.TagId).ToList();
        var followedIds = await _unitOfWork.TagFollows.Query()
            .AsNoTracking()
            .Where(tf => tf.UserId == userProfileId.Value && tagIds.Contains(tf.TagId))
            .Select(tf => tf.TagId)
            .ToListAsync(cancellationToken);

        var followed = followedIds.ToHashSet();
        return tags.Select(t => new TagSummaryDto
        {
            TagId = t.TagId,
            Name = t.Name,
            Slug = t.Slug,
            QuestionCount = t.QuestionCount,
            FollowerCount = t.FollowerCount,
            IsFollowedByCurrentUser = followed.Contains(t.TagId)
        }).ToList();
    }

    public async Task<List<TagSummaryDto>> SuggestAsync(string? query, int take = 8, CancellationToken cancellationToken = default)
    {
        if (take <= 0)
        {
            take = 8;
        }

        take = Math.Min(take, 12);
        var normalized = string.IsNullOrWhiteSpace(query) ? string.Empty : query.Trim();
        var key = $"tags:suggest:{normalized.ToLowerInvariant()}:{take}";

        return await _cacheService.GetOrSetAsync(
            key,
            TimeSpan.FromSeconds(30),
            async ct =>
            {
                var tagsQuery = _unitOfWork.Tags.Query().AsNoTracking();
                if (!string.IsNullOrWhiteSpace(normalized))
                {
                    var pattern = $"%{normalized}%";
                    tagsQuery = tagsQuery.Where(t => EF.Functions.Like(t.Name, pattern));
                }

                return await tagsQuery
                    .OrderByDescending(t => t.Followers.Count)
                    .ThenByDescending(t => t.QuestionTags.Count)
                    .Take(take)
                    .Select(t => new TagSummaryDto
                    {
                        TagId = t.TagId,
                        Name = t.Name,
                        Slug = t.Slug,
                        QuestionCount = t.QuestionTags.Count,
                        FollowerCount = t.Followers.Count,
                        IsFollowedByCurrentUser = false
                    })
                    .ToListAsync(ct);
            },
            cancellationToken);
    }

    public async Task<TagSummaryDto?> GetByIdAsync(int tagId, string? aspNetUserId, CancellationToken cancellationToken = default)
    {
        var userProfileId = await GetUserProfileIdAsync(aspNetUserId, cancellationToken);

        var tag = await _unitOfWork.Tags.Query()
            .AsNoTracking()
            .Where(t => t.TagId == tagId)
            .Select(t => new
            {
                t.TagId,
                t.Name,
                t.Slug,
                QuestionCount = t.QuestionTags.Count,
                FollowerCount = t.Followers.Count
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (tag is null)
        {
            return null;
        }

        var isFollowed = false;
        if (userProfileId is not null)
        {
            isFollowed = await _unitOfWork.TagFollows.Query()
                .AsNoTracking()
                .AnyAsync(tf => tf.UserId == userProfileId.Value && tf.TagId == tagId, cancellationToken);
        }

        return new TagSummaryDto
        {
            TagId = tag.TagId,
            Name = tag.Name,
            Slug = tag.Slug,
            QuestionCount = tag.QuestionCount,
            FollowerCount = tag.FollowerCount,
            IsFollowedByCurrentUser = isFollowed
        };
    }

    public async Task<List<TagSummaryDto>> GetFollowedAsync(string aspNetUserId, CancellationToken cancellationToken = default)
    {
        var userProfileId = await GetUserProfileIdAsync(aspNetUserId, cancellationToken);
        if (userProfileId is null)
        {
            return [];
        }

        return await _unitOfWork.TagFollows.Query()
            .AsNoTracking()
            .Where(tf => tf.UserId == userProfileId.Value)
            .OrderByDescending(tf => tf.CreatedAt)
            .Select(tf => new TagSummaryDto
            {
                TagId = tf.TagId,
                Name = tf.Tag.Name,
                Slug = tf.Tag.Slug,
                QuestionCount = tf.Tag.QuestionTags.Count,
                FollowerCount = tf.Tag.Followers.Count,
                IsFollowedByCurrentUser = true
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<AuthResultDto> ToggleFollowAsync(string aspNetUserId, int tagId, CancellationToken cancellationToken = default)
    {
        var userProfileId = await GetUserProfileIdAsync(aspNetUserId, cancellationToken);
        if (userProfileId is null)
        {
            return AuthResultDto.Failed("User profile not found.");
        }

        var tagExists = await _unitOfWork.Tags.Query().AsNoTracking().AnyAsync(t => t.TagId == tagId, cancellationToken);
        if (!tagExists)
        {
            return AuthResultDto.Failed("Tag not found.");
        }

        var existing = await _unitOfWork.TagFollows.Query()
            .FirstOrDefaultAsync(tf => tf.UserId == userProfileId.Value && tf.TagId == tagId, cancellationToken);

        if (existing is null)
        {
            await _unitOfWork.TagFollows.AddAsync(
                new TagFollow
                {
                    UserId = userProfileId.Value,
                    TagId = tagId,
                    CreatedAt = DateTime.UtcNow
                },
                cancellationToken);
        }
        else
        {
            _unitOfWork.TagFollows.Remove(existing);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return AuthResultDto.Success();
    }

    public async Task<AuthResultDto> CreateAsync(TagUpsertRequestDto request, CancellationToken cancellationToken = default)
    {
        var name = NormalizeName(request.Name);
        if (name is null)
        {
            return AuthResultDto.Failed("Invalid tag name.");
        }

        var slug = ToSlug(name);
        var exists = await _unitOfWork.Tags.Query()
            .AsNoTracking()
            .AnyAsync(t => t.Slug == slug, cancellationToken);

        if (exists)
        {
            return AuthResultDto.Failed("Tag already exists.");
        }

        await _unitOfWork.Tags.AddAsync(
            new Tag
            {
                Name = name,
                Slug = slug,
                CreatedAt = DateTime.UtcNow
            },
            cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return AuthResultDto.Success();
    }

    public async Task<AuthResultDto> UpdateAsync(int tagId, TagUpsertRequestDto request, CancellationToken cancellationToken = default)
    {
        var tag = await _unitOfWork.Tags.Query().FirstOrDefaultAsync(t => t.TagId == tagId, cancellationToken);
        if (tag is null)
        {
            return AuthResultDto.Failed("Tag not found.");
        }

        var name = NormalizeName(request.Name);
        if (name is null)
        {
            return AuthResultDto.Failed("Invalid tag name.");
        }

        var slug = ToSlug(name);
        var exists = await _unitOfWork.Tags.Query()
            .AsNoTracking()
            .AnyAsync(t => t.TagId != tagId && t.Slug == slug, cancellationToken);

        if (exists)
        {
            return AuthResultDto.Failed("Another tag already uses this name.");
        }

        tag.Name = name;
        tag.Slug = slug;
        _unitOfWork.Tags.Update(tag);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return AuthResultDto.Success();
    }

    public async Task<AuthResultDto> DeleteAsync(int tagId, CancellationToken cancellationToken = default)
    {
        var tag = await _unitOfWork.Tags.Query().FirstOrDefaultAsync(t => t.TagId == tagId, cancellationToken);
        if (tag is null)
        {
            return AuthResultDto.Failed("Tag not found.");
        }

        var links = await _unitOfWork.QuestionTags.Query()
            .Where(qt => qt.TagId == tagId)
            .ToListAsync(cancellationToken);

        foreach (var link in links)
        {
            _unitOfWork.QuestionTags.Remove(link);
        }

        var follows = await _unitOfWork.TagFollows.Query()
            .Where(tf => tf.TagId == tagId)
            .ToListAsync(cancellationToken);

        foreach (var follow in follows)
        {
            _unitOfWork.TagFollows.Remove(follow);
        }

        _unitOfWork.Tags.Remove(tag);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return AuthResultDto.Success();
    }

    private async Task<int?> GetUserProfileIdAsync(string? aspNetUserId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(aspNetUserId))
        {
            return null;
        }

        return await _unitOfWork.Users.Query()
            .AsNoTracking()
            .Where(u => u.AspNetUserId == aspNetUserId)
            .Select(u => (int?)u.UserId)
            .FirstOrDefaultAsync(cancellationToken);
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
