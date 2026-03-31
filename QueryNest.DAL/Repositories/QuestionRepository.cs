using Microsoft.EntityFrameworkCore;
using QueryNest.DAL.Interfaces;
using QueryNest.Domain.Entities;

namespace QueryNest.DAL.Repositories;

public class QuestionRepository : GenericRepository<Question>, IQuestionRepository
{
    private readonly DbSet<Question> _questions;

    public QuestionRepository(DbContext dbContext) : base(dbContext)
    {
        _questions = dbContext.Set<Question>();
    }

    public async Task<Question?> GetDetailsAsync(int questionId, CancellationToken cancellationToken = default)
    {
        return await _questions
            .Include(q => q.User)
            .Include(q => q.Category)
            .Include(q => q.Votes)
            .Include(q => q.QuestionTags)
            .ThenInclude(qt => qt.Tag)
            .Include(q => q.Answers)
            .ThenInclude(a => a.User)
            .Include(q => q.Answers)
            .ThenInclude(a => a.Votes)
            .Include(q => q.Answers)
            .ThenInclude(a => a.Comments)
            .ThenInclude(c => c.User)
            .Include(q => q.Answers)
            .ThenInclude(a => a.Comments)
            .ThenInclude(c => c.Votes)
            .FirstOrDefaultAsync(q => q.QuestionId == questionId, cancellationToken);
    }
}
