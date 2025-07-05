using Microsoft.EntityFrameworkCore;
using Tasker.Core.Interfaces;
using Tasker.Domain.Models;
using Tasker.Infrastructure.Data;

namespace Tasker.Infrastructure.Repositories;

public class UserRepository(TaskerDbContext context) : IUserRepository
{
    private readonly TaskerDbContext _context = context;

    public async Task<IEnumerable<User>> GetAllAsync() =>
        await _context.Users.Include(u => u.Tasks).Include(u => u.Projects).ToListAsync();

    public async Task<User?> GetByIdAsync(int id) =>
        await _context.Users.Include(u => u.Tasks).Include(u => u.Projects).FirstOrDefaultAsync(u => u.Id == id);

    public async Task<User?> GetByUsernameAsync(string username) =>
        await _context.Users.Include(u => u.Tasks).Include(u => u.Projects).FirstOrDefaultAsync(u => u.Username == username);

    public async Task<User> AddAsync(User user)
    {
        var entity = await _context.Users.AddAsync(user);
        return entity.Entity;
    }

    public async Task<User> UpdateAsync(User user)
    {
        _context.Users.Update(user);
        return await Task.FromResult(user);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null)
            return false;

        _context.Users.Remove(user);
        return true;
    }

    public async Task<bool> ExistsAsync(int id) =>
        await _context.Users.AnyAsync(u => u.Id == id);

    public async Task<bool> UsernameExistsAsync(string username) =>
        await _context.Users.AnyAsync(u => u.Username == username);

    public async Task<int> SaveChangesAsync() =>
        await _context.SaveChangesAsync();
}