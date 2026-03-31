using QueryNest.Domain.Entities;

namespace QueryNest.DAL.Interfaces;

public interface IUnitOfWork
{
    IGenericRepository<UserProfile> Users { get; }
    IGenericRepository<Category> Categories { get; }
    IGenericRepository<Tag> Tags { get; }
    IQuestionRepository Questions { get; }
    IGenericRepository<QuestionTag> QuestionTags { get; }
    IGenericRepository<Answer> Answers { get; }
    IGenericRepository<Comment> Comments { get; }
    IGenericRepository<Vote> Votes { get; }
    IGenericRepository<Notification> Notifications { get; }
    IGenericRepository<Report> Reports { get; }
    IGenericRepository<TagFollow> TagFollows { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
