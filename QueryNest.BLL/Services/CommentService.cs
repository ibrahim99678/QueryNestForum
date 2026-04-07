using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using QueryNest.BLL.Interfaces;
using QueryNest.Contract.Auth;
using QueryNest.Contract.Comments;
using QueryNest.DAL.Interfaces;
using QueryNest.Domain.Entities;
using QueryNest.Domain.Enums;

namespace QueryNest.BLL.Services;

public class CommentService : ICommentService
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationService _notificationService;

    public CommentService(UserManager<IdentityUser> userManager, IUnitOfWork unitOfWork, INotificationService notificationService)
    {
        _userManager = userManager;
        _unitOfWork = unitOfWork;
        _notificationService = notificationService;
    }

    public async Task<(AuthResultDto Result, int? QuestionId)> CreateAsync(string aspNetUserId, CommentCreateRequestDto request, CancellationToken cancellationToken = default)
    {
        var profile = await _unitOfWork.Users.Query()
            .FirstOrDefaultAsync(x => x.AspNetUserId == aspNetUserId, cancellationToken);

        if (profile is null)
        {
            return (AuthResultDto.Failed("User profile not found."), null);
        }

        var answer = await _unitOfWork.Answers.Query()
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.AnswerId == request.AnswerId, cancellationToken);

        if (answer is null)
        {
            return (AuthResultDto.Failed("Answer not found."), null);
        }

        int? parentCommentAuthorUserId = null;
        if (request.ParentCommentId is not null)
        {
            var parent = await _unitOfWork.Comments.Query()
                .AsNoTracking()
                .Select(c => new { c.CommentId, c.AnswerId, c.UserId })
                .FirstOrDefaultAsync(c => c.CommentId == request.ParentCommentId.Value, cancellationToken);

            if (parent is null || parent.AnswerId != request.AnswerId)
            {
                return (AuthResultDto.Failed("Invalid parent comment."), answer.QuestionId);
            }

            parentCommentAuthorUserId = parent.UserId;
        }

        var content = request.Content?.Trim() ?? string.Empty;
        if (content.Length < 2)
        {
            return (AuthResultDto.Failed("Comment is too short."), answer.QuestionId);
        }

        var comment = new Comment
        {
            AnswerId = request.AnswerId,
            UserId = profile.UserId,
            ParentCommentId = request.ParentCommentId,
            Content = content,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Comments.AddAsync(comment, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var questionTitle = await _unitOfWork.Questions.Query()
            .AsNoTracking()
            .Where(q => q.QuestionId == answer.QuestionId)
            .Select(q => q.Title)
            .FirstOrDefaultAsync(cancellationToken);

        if (request.ParentCommentId is not null && parentCommentAuthorUserId is not null)
        {
            await _notificationService.CreateAsync(
                recipientUserId: parentCommentAuthorUserId.Value,
                actorUserId: profile.UserId,
                type: NotificationType.ReplyPosted,
                message: $"New reply on: {questionTitle}",
                questionId: answer.QuestionId,
                answerId: answer.AnswerId,
                commentId: comment.CommentId,
                cancellationToken: cancellationToken);
        }
        else
        {
            await _notificationService.CreateAsync(
                recipientUserId: answer.UserId,
                actorUserId: profile.UserId,
                type: NotificationType.CommentPosted,
                message: $"New comment on your answer: {questionTitle}",
                questionId: answer.QuestionId,
                answerId: answer.AnswerId,
                commentId: comment.CommentId,
                cancellationToken: cancellationToken);
        }

        return (AuthResultDto.Success(), answer.QuestionId);
    }

    public async Task<CommentEditDto?> GetForEditAsync(int commentId, CancellationToken cancellationToken = default)
    {
        var comment = await _unitOfWork.Comments.Query()
            .AsNoTracking()
            .Include(c => c.Answer)
            .FirstOrDefaultAsync(c => c.CommentId == commentId, cancellationToken);

        if (comment is null)
        {
            return null;
        }

        return new CommentEditDto
        {
            CommentId = comment.CommentId,
            AnswerId = comment.AnswerId,
            QuestionId = comment.Answer.QuestionId,
            Content = comment.Content
        };
    }

    public async Task<(AuthResultDto Result, int? QuestionId)> UpdateAsync(string aspNetUserId, int commentId, CommentUpdateRequestDto request, CancellationToken cancellationToken = default)
    {
        var profile = await _unitOfWork.Users.Query()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.AspNetUserId == aspNetUserId, cancellationToken);

        if (profile is null)
        {
            return (AuthResultDto.Failed("User profile not found."), null);
        }

        var comment = await _unitOfWork.Comments.Query()
            .Include(c => c.Answer)
            .FirstOrDefaultAsync(c => c.CommentId == commentId, cancellationToken);

        if (comment is null)
        {
            return (AuthResultDto.Failed("Comment not found."), null);
        }

        if (comment.UserId != profile.UserId && !await IsModeratorAsync(aspNetUserId))
        {
            return (AuthResultDto.Failed("Not allowed."), comment.Answer.QuestionId);
        }

        var content = request.Content?.Trim() ?? string.Empty;
        if (content.Length < 2)
        {
            return (AuthResultDto.Failed("Comment is too short."), comment.Answer.QuestionId);
        }

        comment.Content = content;
        _unitOfWork.Comments.Update(comment);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return (AuthResultDto.Success(), comment.Answer.QuestionId);
    }

    public async Task<(AuthResultDto Result, int? QuestionId)> DeleteAsync(string aspNetUserId, int commentId, CancellationToken cancellationToken = default)
    {
        var profile = await _unitOfWork.Users.Query()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.AspNetUserId == aspNetUserId, cancellationToken);

        if (profile is null)
        {
            return (AuthResultDto.Failed("User profile not found."), null);
        }

        var comment = await _unitOfWork.Comments.Query()
            .AsNoTracking()
            .Include(c => c.Answer)
            .FirstOrDefaultAsync(c => c.CommentId == commentId, cancellationToken);

        if (comment is null)
        {
            return (AuthResultDto.Failed("Comment not found."), null);
        }

        if (comment.UserId != profile.UserId && !await IsModeratorAsync(aspNetUserId))
        {
            return (AuthResultDto.Failed("Not allowed."), comment.Answer.QuestionId);
        }

        var answerId = comment.AnswerId;
        var questionId = comment.Answer.QuestionId;

        var commentIds = await _unitOfWork.Comments.Query()
            .AsNoTracking()
            .Where(c => c.AnswerId == answerId)
            .Select(c => new { c.CommentId, c.ParentCommentId })
            .ToListAsync(cancellationToken);

        var toDelete = new HashSet<int> { commentId };
        var added = true;

        while (added)
        {
            added = false;
            foreach (var c in commentIds)
            {
                if (c.ParentCommentId is null)
                {
                    continue;
                }

                if (toDelete.Contains(c.ParentCommentId.Value) && toDelete.Add(c.CommentId))
                {
                    added = true;
                }
            }
        }

        var votes = await _unitOfWork.Votes.Query()
            .Where(v => v.CommentId != null && toDelete.Contains(v.CommentId.Value))
            .ToListAsync(cancellationToken);

        foreach (var vote in votes)
        {
            _unitOfWork.Votes.Remove(vote);
        }

        var notifications = await _unitOfWork.Notifications.Query()
            .Where(n => n.CommentId != null && toDelete.Contains(n.CommentId.Value))
            .ToListAsync(cancellationToken);

        foreach (var n in notifications)
        {
            _unitOfWork.Notifications.Remove(n);
        }

        var reports = await _unitOfWork.Reports.Query()
            .Where(r => r.CommentId != null && toDelete.Contains(r.CommentId.Value))
            .ToListAsync(cancellationToken);

        foreach (var r in reports)
        {
            _unitOfWork.Reports.Remove(r);
        }

        var comments = await _unitOfWork.Comments.Query()
            .Where(c => toDelete.Contains(c.CommentId))
            .ToListAsync(cancellationToken);

        foreach (var c in comments)
        {
            _unitOfWork.Comments.Remove(c);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return (AuthResultDto.Success(), questionId);
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
