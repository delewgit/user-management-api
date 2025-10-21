using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using UserManagementApi.Models;
using UserManagementApi.DTOs;
using UserManagementApi.Repositories;

namespace UserManagementApi.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _hasher;

    public UserService(IUserRepository userRepository, IPasswordHasher hasher)
    {
        _userRepository = userRepository;
        _hasher = hasher;
    }

    public async Task<IEnumerable<UserDto>> GetAllUsersAsync(int page = 1, int pageSize = 20, string? query = null, CancellationToken ct = default)
    {
        return (await _userRepository.GetAllAsync(page, pageSize, query, ct)).Select(user => new UserDto(user));
    }

    public async Task<UserDto> GetUserByIdAsync(int id, CancellationToken ct = default)
    {
        var user = await _userRepository.GetByIdAsync(id, ct);
        if (user == null) return null;

        return new UserDto(user);
    }

    public async Task<UserDto> CreateUserAsync(UserCreateDto userDto, CancellationToken ct = default)
    {
        var user = new User
        {
            FirstName = userDto.FirstName,
            LastName = userDto.LastName,
            Email = userDto.Email,
            Password = _hasher.Hash(userDto.Password)
        };

        await _userRepository.AddAsync(user, ct);
        return new UserDto(user);
    }

    public async Task<bool> UpdateUserAsync(int id, UserUpdateDto userDto, CancellationToken ct = default)
    {
        var user = await _userRepository.GetByIdAsync(id, ct);
        if (user == null) return false;

        user.FirstName = userDto.FirstName;
        user.Email = userDto.Email;
        user.Password = _hasher.Hash(userDto.Password);

        await _userRepository.UpdateAsync(user, ct);
        return true;
    }

    public async Task<bool> DeleteUserAsync(int id, CancellationToken ct = default)
    {
        var user = await _userRepository.GetByIdAsync(id, ct);
        if (user == null) return false;

        await _userRepository.DeleteAsync(id, ct);
        return true;
    }
}
