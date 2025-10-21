using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UserManagementApi.Models;

namespace UserManagementApi.Repositories;

public interface IUserRepository
{
    Task<IEnumerable<User>> GetAllAsync(int page = 1, int pageSize = 20, string? query = null, CancellationToken ct = default);
    Task<User> GetByIdAsync(int id, CancellationToken ct = default);
    Task AddAsync(User user, CancellationToken ct = default);
    Task UpdateAsync(User user, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
}
