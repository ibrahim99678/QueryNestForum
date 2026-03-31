using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using QueryNest.BLL.Interfaces;
using QueryNest.Contract.Auth;
using QueryNest.Contract.Reports;
using QueryNest.DAL.Interfaces;
using QueryNest.Domain.Entities;
using QueryNest.Domain.Enums;

namespace QueryNest.BLL.Services;

public class ReportService : IReportService
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly IUnitOfWork _unitOfWork;

    public ReportService(UserManager<IdentityUser> userManager, IUnitOfWork unitOfWork)
    {
        _userManager = userManager;
        _unitOfWork = unitOfWork;
    }

    public async Task<AuthResultDto> CreateAsync(string aspNetUserId, ReportCreateRequestDto request, CancellationToken cancellationToken = default)
    {
        var reporter = await _unitOfWork.Users.Query()
            .FirstOrDefaultAsync(u => u.AspNetUserId == aspNetUserId, cancellationToken);

        if (reporter is null)
        {
            return AuthResultDto.Failed("User profile not found.");
        }

        var reason = (request.Reason ?? string.Empty).Trim();
        if (reason.Length == 0)
        {
            return AuthResultDto.Failed("Reason is required.");
        }

        if (reason.Length > 200)
        {
            reason = reason[..200];
        }

        var details = string.IsNullOrWhiteSpace(request.Details) ? null : request.Details.Trim();
        if (details is not null && details.Length > 1000)
        {
            details = details[..1000];
        }

        var targetType = (ReportTargetType)request.TargetType;
        var targetId = request.TargetId;
        if (targetId <= 0)
        {
            return AuthResultDto.Failed("Invalid target.");
        }

        var (questionId, ownerUserId, preview) = await ResolveTargetAsync(targetType, targetId, cancellationToken);
        if (ownerUserId is null)
        {
            return AuthResultDto.Failed("Content not found.");
        }

        preview = string.IsNullOrWhiteSpace(preview) ? null : preview.Trim();
        if (preview is not null && preview.Length > 200)
        {
            preview = preview[..200];
        }

        try
        {
            var report = new Report
            {
                ReporterUserId = reporter.UserId,
                TargetType = targetType,
                QuestionId = targetType == ReportTargetType.Question ? targetId : null,
                AnswerId = targetType == ReportTargetType.Answer ? targetId : null,
                CommentId = targetType == ReportTargetType.Comment ? targetId : null,
                Reason = reason,
                Details = details,
                Status = ReportStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Reports.AddAsync(report, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            return AuthResultDto.Failed("You already reported this content.");
        }

        return AuthResultDto.Success();
    }

    public async Task<List<ReportListItemDto>> GetPendingAsync(CancellationToken cancellationToken = default)
    {
        var baseQuery = _unitOfWork.Reports.Query()
            .AsNoTracking()
            .Where(r => r.Status == ReportStatus.Pending)
            .OrderByDescending(r => r.CreatedAt);

        var items = await baseQuery
            .Select(r => new
            {
                r.ReportId,
                r.TargetType,
                r.QuestionId,
                r.AnswerId,
                r.CommentId,
                r.Reason,
                r.Details,
                r.Status,
                r.CreatedAt,
                ReporterUserId = r.ReporterUserId,
                ReporterName = r.Reporter.Name
            })
            .ToListAsync(cancellationToken);

        var results = new List<ReportListItemDto>(items.Count);
        foreach (var r in items)
        {
            var targetType = r.TargetType;
            var targetId = targetType == ReportTargetType.Question ? r.QuestionId!.Value :
                targetType == ReportTargetType.Answer ? r.AnswerId!.Value :
                r.CommentId!.Value;

            var (questionId, ownerUserId, preview) = await ResolveTargetAsync(targetType, targetId, cancellationToken);
            if (ownerUserId is null)
            {
                continue;
            }

            var ownerName = await _unitOfWork.Users.Query()
                .AsNoTracking()
                .Where(u => u.UserId == ownerUserId.Value)
                .Select(u => u.Name)
                .FirstOrDefaultAsync(cancellationToken) ?? string.Empty;

            results.Add(new ReportListItemDto
            {
                ReportId = r.ReportId,
                TargetType = (ReportTargetTypeDto)targetType,
                TargetId = targetId,
                Reason = r.Reason,
                Details = r.Details,
                Status = (ReportStatusDto)r.Status,
                CreatedAt = r.CreatedAt,
                ReporterUserId = r.ReporterUserId,
                ReporterName = r.ReporterName,
                ContentOwnerUserId = ownerUserId.Value,
                ContentOwnerName = ownerName,
                QuestionId = questionId,
                ContentPreview = preview
            });
        }

        return results;
    }

    public async Task<AuthResultDto> ReviewAsync(string reviewerAspNetUserId, ReportReviewRequestDto request, CancellationToken cancellationToken = default)
    {
        var reviewerProfile = await _unitOfWork.Users.Query()
            .FirstOrDefaultAsync(u => u.AspNetUserId == reviewerAspNetUserId, cancellationToken);

        if (reviewerProfile is null)
        {
            return AuthResultDto.Failed("User profile not found.");
        }

        var report = await _unitOfWork.Reports.Query()
            .FirstOrDefaultAsync(r => r.ReportId == request.ReportId, cancellationToken);

        if (report is null)
        {
            return AuthResultDto.Failed("Report not found.");
        }

        if (report.Status != ReportStatus.Pending)
        {
            return AuthResultDto.Failed("Report already reviewed.");
        }

        report.Status = request.Approve ? ReportStatus.Approved : ReportStatus.Rejected;
        report.ReviewedByUserId = reviewerProfile.UserId;
        report.ReviewNote = string.IsNullOrWhiteSpace(request.ReviewNote) ? null : request.ReviewNote.Trim();
        report.ReviewedAt = DateTime.UtcNow;

        _unitOfWork.Reports.Update(report);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return AuthResultDto.Success();
    }

    public async Task<AuthResultDto> BanUserAsync(string moderatorAspNetUserId, int userIdToBan, int days, string? reason, CancellationToken cancellationToken = default)
    {
        if (days <= 0)
        {
            days = 7;
        }

        days = Math.Min(days, 3650);

        var moderator = await _userManager.FindByIdAsync(moderatorAspNetUserId);
        if (moderator is null)
        {
            return AuthResultDto.Failed("User not found.");
        }

        var isModerator = await _userManager.IsInRoleAsync(moderator, "Admin") || await _userManager.IsInRoleAsync(moderator, "Moderator");
        if (!isModerator)
        {
            return AuthResultDto.Failed("Not allowed.");
        }

        var profile = await _unitOfWork.Users.Query()
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.UserId == userIdToBan, cancellationToken);

        if (profile is null)
        {
            return AuthResultDto.Failed("User profile not found.");
        }

        var identityUser = await _userManager.FindByIdAsync(profile.AspNetUserId);
        if (identityUser is null)
        {
            return AuthResultDto.Failed("User not found.");
        }

        var until = DateTimeOffset.UtcNow.AddDays(days);
        await _userManager.SetLockoutEnabledAsync(identityUser, true);
        await _userManager.SetLockoutEndDateAsync(identityUser, until);

        return AuthResultDto.Success();
    }

    public async Task<AuthResultDto> UnbanUserAsync(string moderatorAspNetUserId, int userIdToUnban, CancellationToken cancellationToken = default)
    {
        var moderator = await _userManager.FindByIdAsync(moderatorAspNetUserId);
        if (moderator is null)
        {
            return AuthResultDto.Failed("User not found.");
        }

        var isModerator = await _userManager.IsInRoleAsync(moderator, "Admin") || await _userManager.IsInRoleAsync(moderator, "Moderator");
        if (!isModerator)
        {
            return AuthResultDto.Failed("Not allowed.");
        }

        var profile = await _unitOfWork.Users.Query()
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.UserId == userIdToUnban, cancellationToken);

        if (profile is null)
        {
            return AuthResultDto.Failed("User profile not found.");
        }

        var identityUser = await _userManager.FindByIdAsync(profile.AspNetUserId);
        if (identityUser is null)
        {
            return AuthResultDto.Failed("User not found.");
        }

        await _userManager.SetLockoutEndDateAsync(identityUser, null);

        return AuthResultDto.Success();
    }

    private async Task<(int? questionId, int? ownerUserId, string? preview)> ResolveTargetAsync(ReportTargetType targetType, int targetId, CancellationToken cancellationToken)
    {
        TargetResolution? resolution = targetType switch
        {
            ReportTargetType.Question => await _unitOfWork.Questions.Query()
                .AsNoTracking()
                .Where(q => q.QuestionId == targetId)
                .Select(q => new TargetResolution
                {
                    QuestionId = q.QuestionId,
                    OwnerUserId = q.UserId,
                    Preview = q.Title
                })
                .FirstOrDefaultAsync(cancellationToken),

            ReportTargetType.Answer => await _unitOfWork.Answers.Query()
                .AsNoTracking()
                .Where(a => a.AnswerId == targetId)
                .Select(a => new TargetResolution
                {
                    QuestionId = a.QuestionId,
                    OwnerUserId = a.UserId,
                    Preview = a.Content
                })
                .FirstOrDefaultAsync(cancellationToken),

            ReportTargetType.Comment => await _unitOfWork.Comments.Query()
                .AsNoTracking()
                .Include(c => c.Answer)
                .Where(c => c.CommentId == targetId)
                .Select(c => new TargetResolution
                {
                    QuestionId = c.Answer.QuestionId,
                    OwnerUserId = c.UserId,
                    Preview = c.Content
                })
                .FirstOrDefaultAsync(cancellationToken),

            _ => null
        };

        return (resolution?.QuestionId, resolution?.OwnerUserId, resolution?.Preview);
    }

    private sealed class TargetResolution
    {
        public int? QuestionId { get; init; }
        public int? OwnerUserId { get; init; }
        public string? Preview { get; init; }
    }
}
