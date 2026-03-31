using QueryNest.Contract.Auth;
using QueryNest.Contract.Reports;

namespace QueryNest.BLL.Interfaces;

public interface IReportService
{
    Task<AuthResultDto> CreateAsync(string aspNetUserId, ReportCreateRequestDto request, CancellationToken cancellationToken = default);
    Task<List<ReportListItemDto>> GetPendingAsync(CancellationToken cancellationToken = default);
    Task<AuthResultDto> ReviewAsync(string reviewerAspNetUserId, ReportReviewRequestDto request, CancellationToken cancellationToken = default);
    Task<AuthResultDto> BanUserAsync(string moderatorAspNetUserId, int userIdToBan, int days, string? reason, CancellationToken cancellationToken = default);
    Task<AuthResultDto> UnbanUserAsync(string moderatorAspNetUserId, int userIdToUnban, CancellationToken cancellationToken = default);
}
