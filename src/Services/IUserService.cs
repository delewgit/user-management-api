using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UserManagementApi.DTOs;

namespace UserManagementApi.Services;
public interface IUserService
{
    Task<IEnumerable<UserDto>> GetAllUsersAsync(int page = 1, int pageSize = 20, string? query = null, CancellationToken ct = default);
    Task<UserDto> GetUserByIdAsync(int id, CancellationToken ct = default);
    Task<UserDto> CreateUserAsync(UserCreateDto userDto, CancellationToken ct = default);
    Task<bool> UpdateUserAsync(int id, UserUpdateDto userDto, CancellationToken ct = default);
    Task<bool> DeleteUserAsync(int id, CancellationToken ct = default);
}