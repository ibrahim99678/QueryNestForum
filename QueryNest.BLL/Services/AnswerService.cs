using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using QueryNest.BLL.Interfaces;
using QueryNest.Contract.Answers;
using QueryNest.Contract.Auth;
using QueryNest.DAL.Interfaces;
using QueryNest.Domain.Entities;
using QueryNest.Domain.Enums;

namespace QueryNest.BLL.Services;

public class AnswerService : IAnswerService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationService _notificationService;
    private readonly UserManager<IdentityUser> _userManager;

    public AnswerService(IUnitOfWork unitOfWork, INotificationService notificationService, UserManager<IdentityUser> userManager)
    {
        _unitOfWork = unitOfWork;
        _notificationService = notificationService;
        _userManager = userManager;
    }

    public async Task<(AuthResultDto Result, int? AnswerId)> CreateAsync(string aspNetUserId, AnswerCreateRequestDto request, CancellationToken cancellationToken = default)
    {
        var profile = await _unitOfWork.Users.Query()
            .FirstOrDefaultAsync(x => x.AspNetUserId == aspNetUserId, cancellationToken);

        if (profile is null)
        {
            return (AuthResultDto.Failed("User profile not found."), null);
        }

        var question = await _unitOfWork.Questions.Query()
            .AsNoTracking()
            .Where(q => q.QuestionId == request.QuestionId)
            .Select(q => new { q.QuestionId, q.UserId, q.Title })
            .FirstOrDefaultAsync(cancellationToken);

        if (question is null)
        {
            return (AuthResultDto.Failed("Question not found."), null);
        }

        var content = request.Content?.Trim() ?? string.Empty;
        if (content.Length < 10)
        {
            return (AuthResultDto.Failed("Answer is too short."), null);
        }

        var answer = new Answer
        {
            QuestionId = request.QuestionId,
            UserId = profile.UserId,
            Content = content,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Answers.AddAsync(answer, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _notificationService.CreateAsync(
            recipientUserId: question.UserId,
            actorUserId: profile.UserId,
            type: NotificationType.AnswerPosted,
            message: $"New answer on your question: {question.Title}",
            questionId: question.QuestionId,
            answerId: answer.AnswerId,
            commentId: null,
            cancellationToken: cancellationToken);

        return (AuthResultDto.Success(), answer.AnswerId);
    }

    public async Task<(AuthResultDto Result, AnswerEditDto? Answer)> GetForEditAsync(string aspNetUserId, int answerId, CancellationToken cancellationToken = default)
    {
        var profile = await _unitOfWork.Users.Query()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.AspNetUserId == aspNetUserId, cancellationToken);

        if (profile is null)
        {
            return (AuthResultDto.Failed("User profile not found."), null);
        }

        var answer = await _unitOfWork.Answers.Query()
            .AsNoTracking()
            .Where(a => a.AnswerId == answerId)
            .Select(a => new { a.AnswerId, a.QuestionId, a.UserId, a.Content })
            .FirstOrDefaultAsync(cancellationToken);

        if (answer is null)
        {
            return (AuthResultDto.Failed("Answer not found."), null);
        }

        if (answer.UserId != profile.UserId && !await IsModeratorAsync(aspNetUserId))
        {
            return (AuthResultDto.Failed("Not allowed."), null);
        }

        return (AuthResultDto.Success(), new AnswerEditDto
        {
            AnswerId = answer.AnswerId,
            QuestionId = answer.QuestionId,
            Content = answer.Content
        });
    }

    public async Task<(AuthResultDto Result, int? QuestionId)> UpdateAsync(string aspNetUserId, int answerId, AnswerUpdateRequestDto request, CancellationToken cancellationToken = default)
    {
        var profile = await _unitOfWork.Users.Query()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.AspNetUserId == aspNetUserId, cancellationToken);

        if (profile is null)
        {
            return (AuthResultDto.Failed("User profile not found."), null);
        }

        var answer = await _unitOfWork.Answers.Query()
            .FirstOrDefaultAsync(a => a.AnswerId == answerId, cancellationToken);

        if (answer is null)
        {
            return (AuthResultDto.Failed("Answer not found."), null);
        }

        if (answer.UserId != profile.UserId && !await IsModeratorAsync(aspNetUserId))
        {
            return (AuthResultDto.Failed("Not allowed."), answer.QuestionId);
        }

        var content = request.Content?.Trim() ?? string.Empty;
        if (content.Length < 10)
        {
            return (AuthResultDto.Failed("Answer is too short."), answer.QuestionId);
        }

        if (content.Length > 4000)
        {
            return (AuthResultDto.Failed("Answer is too long."), answer.QuestionId);
        }

        answer.Content = content;
        _unitOfWork.Answers.Update(answer);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return (AuthResultDto.Success(), answer.QuestionId);
    }

    public async Task<(AuthResultDto Result, int? QuestionId)> DeleteAsync(string aspNetUserId, int answerId, CancellationToken cancellationToken = default)
    {
        var profile = await _unitOfWork.Users.Query()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.AspNetUserId == aspNetUserId, cancellationToken);

        if (profile is null)
        {
            return (AuthResultDto.Failed("User profile not found."), null);
        }

        var answer = await _unitOfWork.Answers.Query()
            .FirstOrDefaultAsync(a => a.AnswerId == answerId, cancellationToken);

        if (answer is null)
        {
            return (AuthResultDto.Failed("Answer not found."), null);
        }

        if (answer.UserId != profile.UserId && !await IsModeratorAsync(aspNetUserId))
        {
            return (AuthResultDto.Failed("Not allowed."), answer.QuestionId);
        }

        var questionId = answer.QuestionId;

        var commentIds = await _unitOfWork.Comments.Query()
            .AsNoTracking()
            .Where(c => c.AnswerId == answerId)
            .Select(c => c.CommentId)
            .ToListAsync(cancellationToken);

        var votes = await _unitOfWork.Votes.Query()
            .Where(v => v.AnswerId == answerId || (v.CommentId != null && commentIds.Contains(v.CommentId.Value)))
            .ToListAsync(cancellationToken);

        foreach (var v in votes)
        {
            _unitOfWork.Votes.Remove(v);
        }

        var notifications = await _unitOfWork.Notifications.Query()
            .Where(n => n.AnswerId == answerId || (n.CommentId != null && commentIds.Contains(n.CommentId.Value)))
            .ToListAsync(cancellationToken);

        foreach (var n in notifications)
        {
            _unitOfWork.Notifications.Remove(n);
        }

        var reports = await _unitOfWork.Reports.Query()
            .Where(r => r.AnswerId == answerId || (r.CommentId != null && commentIds.Contains(r.CommentId.Value)))
            .ToListAsync(cancellationToken);

        foreach (var r in reports)
        {
            _unitOfWork.Reports.Remove(r);
        }

        _unitOfWork.Answers.Remove(answer);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return (AuthResultDto.Success(), questionId);
    }

    private async Task<bool> IsModeratorAsync(string aspNetUserId)
    {
        var user = await _userManager.FindByIdAsync(aspNetUserId);
        if (user is null)
        {
            return false;
        }

        return await _userManager.IsInRoleAsync(user, "Admin") || await _userManager.IsInRoleAsync(user, "Moderator");
    }
}
