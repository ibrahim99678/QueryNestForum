using QueryNest.DAL.Data;
using QueryNest.DAL.Interfaces;
using QueryNest.DAL.Repositories;
using QueryNest.Domain.Entities;

namespace QueryNest.DAL;

public class UnitOfWork : IUnitOfWork
{
    private readonly QueryNestDbContext _dbContext;

    private IGenericRepository<UserProfile>? _users;
    private IGenericRepository<Category>? _categories;
    private IGenericRepository<Tag>? _tags;
    private IQuestionRepository? _questions;
    private IGenericRepository<QuestionTag>? _questionTags;
    private IGenericRepository<Answer>? _answers;
    private IGenericRepository<Comment>? _comments;
    private IGenericRepository<Vote>? _votes;
    private IGenericRepository<Notification>? _notifications;
    private IGenericRepository<Report>? _reports;
    private IGenericRepository<TagFollow>? _tagFollows;

    public UnitOfWork(QueryNestDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public IGenericRepository<UserProfile> Users => _users ??= new GenericRepository<UserProfile>(_dbContext);
    public IGenericRepository<Category> Categories => _categories ??= new GenericRepository<Category>(_dbContext);
    public IGenericRepository<Tag> Tags => _tags ??= new GenericRepository<Tag>(_dbContext);
    public IQuestionRepository Questions => _questions ??= new QuestionRepository(_dbContext);
    public IGenericRepository<QuestionTag> QuestionTags => _questionTags ??= new GenericRepository<QuestionTag>(_dbContext);
    public IGenericRepository<Answer> Answers => _answers ??= new GenericRepository<Answer>(_dbContext);
    public IGenericRepository<Comment> Comments => _comments ??= new GenericRepository<Comment>(_dbContext);
    public IGenericRepository<Vote> Votes => _votes ??= new GenericRepository<Vote>(_dbContext);
    public IGenericRepository<Notification> Notifications => _notifications ??= new GenericRepository<Notification>(_dbContext);
    public IGenericRepository<Report> Reports => _reports ??= new GenericRepository<Report>(_dbContext);
    public IGenericRepository<TagFollow> TagFollows => _tagFollows ??= new GenericRepository<TagFollow>(_dbContext);

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}
