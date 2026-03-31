using QueryNest.Contract.Auth;
using QueryNest.Contract.Categories;

namespace QueryNest.BLL.Interfaces;

public interface ICategoryService
{
    Task<List<CategoryDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<CategoryDto?> GetByIdAsync(int categoryId, CancellationToken cancellationToken = default);
    Task<AuthResultDto> CreateAsync(CategoryUpsertRequestDto request, CancellationToken cancellationToken = default);
    Task<AuthResultDto> UpdateAsync(int categoryId, CategoryUpsertRequestDto request, CancellationToken cancellationToken = default);
    Task<AuthResultDto> DeleteAsync(int categoryId, CancellationToken cancellationToken = default);
}
