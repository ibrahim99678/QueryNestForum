using QueryNest.Domain.Entities;

namespace QueryNest.DAL.Interfaces;

public interface IQuestionRepository : IGenericRepository<Question>
{
    Task<Question?> GetDetailsAsync(int questionId, CancellationToken cancellationToken = default);
}
