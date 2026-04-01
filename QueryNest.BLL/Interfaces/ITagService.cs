using QueryNest.Contract.Auth;
using QueryNest.Contract.Tags;

namespace QueryNest.BLL.Interfaces;

public interface ITagService
{
    Task<List<TagSummaryDto>> GetAllAsync(string? aspNetUserId, string? query = null, CancellationToken cancellationToken = default);
    Task<List<TagSummaryDto>> GetTrendingAsync(string? aspNetUserId, int take = 10, CancellationToken cancellationToken = default);
    Task<List<TagSummaryDto>> SuggestAsync(string? query, int take = 8, CancellationToken cancellationToken = default);
    Task<TagSummaryDto?> GetByIdAsync(int tagId, string? aspNetUserId, CancellationToken cancellationToken = default);
    Task<List<TagSummaryDto>> GetFollowedAsync(string aspNetUserId, CancellationToken cancellationToken = default);
    Task<AuthResultDto> ToggleFollowAsync(string aspNetUserId, int tagId, CancellationToken cancellationToken = default);

    Task<AuthResultDto> CreateAsync(TagUpsertRequestDto request, CancellationToken cancellationToken = default);
    Task<AuthResultDto> UpdateAsync(int tagId, TagUpsertRequestDto request, CancellationToken cancellationToken = default);
    Task<AuthResultDto> DeleteAsync(int tagId, CancellationToken cancellationToken = default);
}
