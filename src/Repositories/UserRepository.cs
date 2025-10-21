using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using UserManagementApi.Data;
using UserManagementApi.Models;

namespace UserManagementApi.Repositories;

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _context;

    public UserRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<User>> GetAllAsync(int page = 1, int pageSize = 20, string? query = null, CancellationToken ct = default)
    {
        // Validate paging bounds
        page = Math.Max(1, page);

        if (!string.IsNullOrWhiteSpace(query))
        {
            query = query.Trim();
            if (query.Length > 100) query = query.Substring(0, 100); // protect from very long queries
        }

        var users = _context.Users.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(query))
            users = users.Where(u => u.FirstName.Contains(query) || u.LastName.Contains(query) || u.Email.Contains(query));
        if (pageSize == 0)
            return await users.ToListAsync(ct);

        pageSize = Math.Clamp(pageSize, 1, 100);

        var total = await users.CountAsync(ct);
        var items = await users
            .OrderBy(u => u.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => u)
            .ToListAsync(ct);

        return items;
    }

    public async Task<User> GetByIdAsync(int id, CancellationToken ct = default)
    {
        return await _context.Users.FindAsync(id, ct);
    }

    public async Task AddAsync(User user, CancellationToken ct = default)
    {
        await _context.Users.AddAsync(user, ct);
        await _context.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(User user, CancellationToken ct = default)
    {
        _context.Users.Update(user);
        await _context.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var user = await GetByIdAsync(id, ct);
        if (user != null)
        {
            _context.Users.Remove(user);
            await _context.SaveChangesAsync(ct);
        }
    }
}