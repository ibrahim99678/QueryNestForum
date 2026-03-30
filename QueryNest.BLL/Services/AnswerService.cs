using Microsoft.EntityFrameworkCore;
using QueryNest.BLL.Interfaces;
using QueryNest.Contract.Answers;
using QueryNest.Contract.Auth;
using QueryNest.DAL.Interfaces;
using QueryNest.Domain.Entities;

namespace QueryNest.BLL.Services;

public class AnswerService : IAnswerService
{
    private readonly IUnitOfWork _unitOfWork;

    public AnswerService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<(AuthResultDto Result, int? AnswerId)> CreateAsync(string aspNetUserId, AnswerCreateRequestDto request, CancellationToken cancellationToken = default)
    {
        var profile = await _unitOfWork.Users.Query()
            .FirstOrDefaultAsync(x => x.AspNetUserId == aspNetUserId, cancellationToken);

        if (profile is null)
        {
            return (AuthResultDto.Failed("User profile not found."), null);
        }

        var questionExists = await _unitOfWork.Questions.Query()
            .AnyAsync(q => q.QuestionId == request.QuestionId, cancellationToken);

        if (!questionExists)
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

        return (AuthResultDto.Success(), answer.AnswerId);
    }
}
