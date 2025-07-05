using Microsoft.EntityFrameworkCore;
using Tasker.Core.Interfaces;
using Tasker.Domain.Models;
using Tasker.Infrastructure.Data;

namespace Tasker.Infrastructure.Repositories;

public class ProjectRepository(TaskerDbContext context) : IProjectRepository
{
    private readonly TaskerDbContext _context = context;

    public async Task<IEnumerable<Project>> GetAllAsync() =>
        await _context.Projects.Include(p => p.Tasks).ToListAsync();

    public async Task<Project?> GetByIdAsync(int id) =>
        await _context.Projects.Include(p => p.Tasks).FirstOrDefaultAsync(p => p.Id == id);

    public async Task<Project> AddAsync(Project project)
    {
        var entity = await _context.Projects.AddAsync(project);
        return entity.Entity;
    }

    public async Task<Project> UpdateAsync(Project project)
    {
        project.UpdatedOn = DateTimeOffset.Now;
        _context.Projects.Update(project);
        return await Task.FromResult(project);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var project = await _context.Projects.FindAsync(id);
        if (project == null)
            return false;

        _context.Projects.Remove(project);
        return true;
    }

    public async Task<bool> ExistsAsync(int id) =>
        await _context.Projects.AnyAsync(p => p.Id == id);

    public async Task<int> SaveChangesAsync() =>
        await _context.SaveChangesAsync();
}