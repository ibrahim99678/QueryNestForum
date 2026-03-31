using Microsoft.EntityFrameworkCore;
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

    public AnswerService(IUnitOfWork unitOfWork, INotificationService notificationService)
    {
        _unitOfWork = unitOfWork;
        _notificationService = notificationService;
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
}
