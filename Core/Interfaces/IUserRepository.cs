using Tasker.Domain.Models;

namespace Tasker.Core.Interfaces;

public interface IUserRepository
{
    Task<IEnumerable<User>> GetAllAsync();
    Task<User?> GetByIdAsync(int id);
    Task<User?> GetByUsernameAsync(string username);
    Task<User> AddAsync(User user);
    Task<User> UpdateAsync(User user);
    Task<bool> DeleteAsync(int id);
    Task<bool> ExistsAsync(int id);
    Task<bool> UsernameExistsAsync(string username);
    Task<int> SaveChangesAsync();
    
    Task<UserSession> AddSessionAsync(UserSession session);
    Task<UserSession?> GetSessionByTokenAsync(string token);
    Task<UserSession> UpdateSessionAsync(UserSession session);
    Task DeleteSessionAsync(UserSession session);
}