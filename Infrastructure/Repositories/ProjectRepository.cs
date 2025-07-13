using Tasker.Core.Interfaces;
using Tasker.Domain.Models;
using Tasker.Infrastructure.Data;

namespace Tasker.Infrastructure.Repositories;

public class ProjectRepository(DatabaseManager databaseManager) : IProjectRepository
{
    private readonly DatabaseManager _databaseManager = databaseManager;

    public async Task<IEnumerable<Project>> GetAllAsync()
    {
        var projects = await _databaseManager.GetAllProjectsWithTasksAsync();
        
        foreach (var project in projects)
        {
            project.Decrypt();
            foreach (var task in project.Tasks)
            {
                task.Decrypt();
            }
        }
        
        return projects;
    }

    public async Task<Project?> GetByIdAsync(Guid id)
    {
        var project = await _databaseManager.GetProjectWithTasksAsync(id);
        
        if (project != null)
        {
            project.Decrypt();
            foreach (var task in project.Tasks)
            {
                task.Decrypt();
            }
        }
        
        return project;
    }

    public async Task<Project> AddAsync(Project project)
    {
        project.Encrypt();
        
        var result = await _databaseManager.AddAsync(project);
        
        result.Decrypt();
        
        return result;
    }

    public async Task<Project> UpdateAsync(Project project)
    {
        project.UpdatedOn = DateTime.UtcNow;
        
        project.Encrypt();
        
        var result = await _databaseManager.UpdateAsync(project);
        
        result.Decrypt();
        
        return result;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        try
        {
            await _databaseManager.DeleteAsync<Project>(id);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> ExistsAsync(Guid id)
    {
        var project = await _databaseManager.GetAsync<Project>(id);
        return project != null;
    }

    public async Task<bool> ProjectNameExistsAsync(string name, Guid userId)
    {
        return await _databaseManager.ProjectNameExistsAsync(name, userId);
    }

    public async Task<int> SaveChangesAsync()
    {
        return await Task.FromResult(1);
    }
}