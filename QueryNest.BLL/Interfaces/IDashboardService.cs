using QueryNest.Contract.Dashboard;

namespace QueryNest.BLL.Interfaces;

public interface IDashboardService
{
    Task<UserDashboardDto?> GetUserDashboardAsync(string aspNetUserId, CancellationToken cancellationToken = default);
    Task<AdminDashboardDto> GetAdminDashboardAsync(CancellationToken cancellationToken = default);
}
