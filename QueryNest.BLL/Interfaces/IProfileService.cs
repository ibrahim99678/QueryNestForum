using QueryNest.Contract.Auth;
using QueryNest.Contract.Users;

namespace QueryNest.BLL.Interfaces;

public interface IProfileService
{
    Task<UserProfileDto?> GetProfileAsync(string aspNetUserId, CancellationToken cancellationToken = default);
    Task<AuthResultDto> UpdateProfileAsync(string aspNetUserId, UpdateProfileRequestDto request, CancellationToken cancellationToken = default);
    Task<AuthResultDto> UpdateAvatarAsync(string aspNetUserId, string avatarPath, CancellationToken cancellationToken = default);
}
