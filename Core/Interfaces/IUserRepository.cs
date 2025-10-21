using Domain.Models;

namespace Tasker.Core.Interfaces;

public interface IUserRepository
{
    Task<IEnumerable<User>> GetAllAsync();
    Task<User?> GetByIdAsync(Guid id);
    Task<User?> GetByUsernameAsync(string username);
    Task<User> AddAsync(User user);
    Task<User> UpdateAsync(User user);
    Task<bool> DeleteAsync(Guid id);
    Task<bool> ExistsAsync(Guid id);
    Task<bool> UsernameExistsAsync(string username);
    Task<int> SaveChangesAsync();

    Task<UserSession> AddSessionAsync(UserSession session);
    Task<UserSession?> GetSessionByTokenAsync(string token);
    Task<UserSession> UpdateSessionAsync(UserSession session);
    Task DeleteSessionAsync(UserSession session);
}
