using Microsoft.EntityFrameworkCore;
using QueryNest.BLL.Interfaces;
using QueryNest.Contract.Auth;
using QueryNest.Contract.Votes;
using QueryNest.DAL.Interfaces;
using QueryNest.Domain.Entities;
using QueryNest.Domain.Enums;

namespace QueryNest.BLL.Services;

public class VoteService : IVoteService
{
    private readonly IUnitOfWork _unitOfWork;

    public VoteService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<(AuthResultDto Result, int? QuestionId)> CastAsync(string aspNetUserId, CastVoteRequestDto request, CancellationToken cancellationToken = default)
    {
        if (request.VoteType is not (-1 or 1))
        {
            return (AuthResultDto.Failed("Invalid vote type."), null);
        }

        var voterProfile = await _unitOfWork.Users.Query()
            .FirstOrDefaultAsync(x => x.AspNetUserId == aspNetUserId, cancellationToken);

        if (voterProfile is null)
        {
            return (AuthResultDto.Failed("User profile not found."), null);
        }

        var voteType = (VoteType)request.VoteType;

        return request.TargetType switch
        {
            VoteTargetTypeDto.Question => await VoteOnQuestionAsync(voterProfile.UserId, aspNetUserId, request.TargetId, voteType, cancellationToken),
            VoteTargetTypeDto.Answer => await VoteOnAnswerAsync(voterProfile.UserId, aspNetUserId, request.TargetId, voteType, cancellationToken),
            VoteTargetTypeDto.Comment => await VoteOnCommentAsync(voterProfile.UserId, aspNetUserId, request.TargetId, voteType, cancellationToken),
            _ => (AuthResultDto.Failed("Invalid vote target."), null)
        };
    }

    private async Task<(AuthResultDto Result, int? QuestionId)> VoteOnQuestionAsync(int voterUserId, string aspNetUserId, int questionId, VoteType voteType, CancellationToken cancellationToken)
    {
        var question = await _unitOfWork.Questions.Query()
            .FirstOrDefaultAsync(q => q.QuestionId == questionId, cancellationToken);

        if (question is null)
        {
            return (AuthResultDto.Failed("Question not found."), null);
        }

        if (question.UserId == voterUserId)
        {
            return (AuthResultDto.Failed("You cannot vote on your own content."), question.QuestionId);
        }

        var existing = await _unitOfWork.Votes.Query()
            .FirstOrDefaultAsync(v => v.UserId == voterUserId && v.QuestionId == questionId, cancellationToken);

        var (delta, action) = ComputeDelta(existing?.VoteType, voteType);

        if (action == VoteAction.None)
        {
            return (AuthResultDto.Success(), question.QuestionId);
        }

        if (action == VoteAction.Remove && existing is not null)
        {
            _unitOfWork.Votes.Remove(existing);
        }
        else if (action == VoteAction.Update && existing is not null)
        {
            existing.VoteType = voteType;
            _unitOfWork.Votes.Update(existing);
        }
        else if (action == VoteAction.Add)
        {
            await _unitOfWork.Votes.AddAsync(
                new Vote
                {
                    UserId = voterUserId,
                    QuestionId = questionId,
                    VoteType = voteType,
                    CreatedAt = DateTime.UtcNow
                },
                cancellationToken);
        }

        await ApplyReputationDeltaAsync(question.UserId, delta, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return (AuthResultDto.Success(), question.QuestionId);
    }

    private async Task<(AuthResultDto Result, int? QuestionId)> VoteOnAnswerAsync(int voterUserId, string aspNetUserId, int answerId, VoteType voteType, CancellationToken cancellationToken)
    {
        var answer = await _unitOfWork.Answers.Query()
            .FirstOrDefaultAsync(a => a.AnswerId == answerId, cancellationToken);

        if (answer is null)
        {
            return (AuthResultDto.Failed("Answer not found."), null);
        }

        if (answer.UserId == voterUserId)
        {
            return (AuthResultDto.Failed("You cannot vote on your own content."), answer.QuestionId);
        }

        var existing = await _unitOfWork.Votes.Query()
            .FirstOrDefaultAsync(v => v.UserId == voterUserId && v.AnswerId == answerId, cancellationToken);

        var (delta, action) = ComputeDelta(existing?.VoteType, voteType);

        if (action == VoteAction.None)
        {
            return (AuthResultDto.Success(), answer.QuestionId);
        }

        if (action == VoteAction.Remove && existing is not null)
        {
            _unitOfWork.Votes.Remove(existing);
        }
        else if (action == VoteAction.Update && existing is not null)
        {
            existing.VoteType = voteType;
            _unitOfWork.Votes.Update(existing);
        }
        else if (action == VoteAction.Add)
        {
            await _unitOfWork.Votes.AddAsync(
                new Vote
                {
                    UserId = voterUserId,
                    AnswerId = answerId,
                    VoteType = voteType,
                    CreatedAt = DateTime.UtcNow
                },
                cancellationToken);
        }

        await ApplyReputationDeltaAsync(answer.UserId, delta, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return (AuthResultDto.Success(), answer.QuestionId);
    }

    private async Task<(AuthResultDto Result, int? QuestionId)> VoteOnCommentAsync(int voterUserId, string aspNetUserId, int commentId, VoteType voteType, CancellationToken cancellationToken)
    {
        var comment = await _unitOfWork.Comments.Query()
            .Include(c => c.Answer)
            .FirstOrDefaultAsync(c => c.CommentId == commentId, cancellationToken);

        if (comment is null)
        {
            return (AuthResultDto.Failed("Comment not found."), null);
        }

        if (comment.UserId == voterUserId)
        {
            return (AuthResultDto.Failed("You cannot vote on your own content."), comment.Answer.QuestionId);
        }

        var existing = await _unitOfWork.Votes.Query()
            .FirstOrDefaultAsync(v => v.UserId == voterUserId && v.CommentId == commentId, cancellationToken);

        var (delta, action) = ComputeDelta(existing?.VoteType, voteType);

        if (action == VoteAction.None)
        {
            return (AuthResultDto.Success(), comment.Answer.QuestionId);
        }

        if (action == VoteAction.Remove && existing is not null)
        {
            _unitOfWork.Votes.Remove(existing);
        }
        else if (action == VoteAction.Update && existing is not null)
        {
            existing.VoteType = voteType;
            _unitOfWork.Votes.Update(existing);
        }
        else if (action == VoteAction.Add)
        {
            await _unitOfWork.Votes.AddAsync(
                new Vote
                {
                    UserId = voterUserId,
                    CommentId = commentId,
                    VoteType = voteType,
                    CreatedAt = DateTime.UtcNow
                },
                cancellationToken);
        }

        await ApplyReputationDeltaAsync(comment.UserId, delta, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return (AuthResultDto.Success(), comment.Answer.QuestionId);
    }

    private async Task ApplyReputationDeltaAsync(int contentOwnerUserId, int delta, CancellationToken cancellationToken)
    {
        if (delta == 0)
        {
            return;
        }

        var ownerProfile = await _unitOfWork.Users.Query()
            .FirstOrDefaultAsync(u => u.UserId == contentOwnerUserId, cancellationToken);

        if (ownerProfile is null)
        {
            return;
        }

        ownerProfile.Reputation += delta;
        ownerProfile.UpdatedAt = DateTime.UtcNow;
        _unitOfWork.Users.Update(ownerProfile);
    }

    private static (int delta, VoteAction action) ComputeDelta(VoteType? existing, VoteType incoming)
    {
        if (existing is null)
        {
            return ((int)incoming, VoteAction.Add);
        }

        if (existing.Value == incoming)
        {
            return (-(int)existing.Value, VoteAction.Remove);
        }

        return ((int)incoming - (int)existing.Value, VoteAction.Update);
    }

    private enum VoteAction
    {
        None = 0,
        Add = 1,
        Update = 2,
        Remove = 3
    }
}
