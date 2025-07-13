using Tasker.Core.Interfaces;
using Tasker.Domain.Models;
using Tasker.Infrastructure.Data;

namespace Tasker.Infrastructure.Repositories;

public class UserRepository(DatabaseManager databaseManager) : IUserRepository
{
    private readonly DatabaseManager _databaseManager = databaseManager;

    public async Task<IEnumerable<User>> GetAllAsync() =>
        await _databaseManager.GetAllAsync<User>();

    public async Task<User?> GetByIdAsync(Guid id) =>
        await _databaseManager.GetAsync<User>(id);

    public async Task<User?> GetByUsernameAsync(string username) =>
        await _databaseManager.GetUserByUsernameAsync(username);

    public async Task<User> AddAsync(User user)
    {
        return await _databaseManager.AddAsync(user);
    }

    public async Task<User> UpdateAsync(User user)
    {
        return await _databaseManager.UpdateAsync(user);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        try
        {
            await _databaseManager.DeleteAsync<User>(id);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> ExistsAsync(Guid id)
    {
        var user = await _databaseManager.GetAsync<User>(id);
        return user != null;
    }

    public async Task<bool> UsernameExistsAsync(string username) =>
        await _databaseManager.UsernameExistsAsync(username);

    public async Task<int> SaveChangesAsync()
    {
        return await Task.FromResult(1);
    }

    public async Task<UserSession> AddSessionAsync(UserSession session)
    {
        return await _databaseManager.AddAsync(session);
    }

    public async Task<UserSession?> GetSessionByTokenAsync(string token) =>
        await _databaseManager.GetSessionByTokenAsync(token);

    public async Task<UserSession> UpdateSessionAsync(UserSession session)
    {
        return await _databaseManager.UpdateAsync(session);
    }

    public async Task DeleteSessionAsync(UserSession session)
    {
        await _databaseManager.DeleteAsync<UserSession>(session.Id);
    }
}