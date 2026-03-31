namespace QueryNest.Contract.Categories;

public class CategoryUpsertRequestDto
{
    public string Name { get; init; } = default!;
    public string? Description { get; init; }
}
