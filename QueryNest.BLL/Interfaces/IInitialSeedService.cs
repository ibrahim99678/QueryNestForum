namespace QueryNest.BLL.Interfaces;

public interface IInitialSeedService
{
    Task SeedAdminAsync(string email, string password, CancellationToken cancellationToken = default);
}
