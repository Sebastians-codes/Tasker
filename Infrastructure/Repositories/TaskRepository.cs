using Microsoft.EntityFrameworkCore;
using Tasker.Core.Interfaces;
using Tasker.Domain.Models;
using Tasker.Infrastructure.Data;

namespace Tasker.Infrastructure.Repositories;

public class TaskRepository(TaskerDbContext context) : ITaskRepository
{
    private readonly TaskerDbContext _context = context;

    public async Task<IEnumerable<Tasks>> GetAllAsync() =>
        await _context.Tasks.Include(t => t.Project).ToListAsync();

    public async Task<Tasks?> GetByIdAsync(int id) =>
        await _context.Tasks.Include(t => t.Project).FirstOrDefaultAsync(t => t.Id == id);

    public async Task<Tasks> AddAsync(Tasks task)
    {
        var entity = await _context.Tasks.AddAsync(task);
        return entity.Entity;
    }

    public async Task<Tasks> UpdateAsync(Tasks task)
    {
        task.UpdatedOn = DateTimeOffset.Now;
        _context.Tasks.Update(task);
        return await Task.FromResult(task);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var task = await _context.Tasks.FindAsync(id);
        if (task == null)
            return false;

        _context.Tasks.Remove(task);
        return true;
    }

    public async Task<bool> ExistsAsync(int id) =>
        await _context.Tasks.AnyAsync(t => t.Id == id);

    public async Task<int> SaveChangesAsync() =>
        await _context.SaveChangesAsync();
}
