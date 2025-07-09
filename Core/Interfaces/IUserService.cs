using Tasker.Domain.Models;

namespace Tasker.Cli.Services;

public interface IUserService
{
    Task<User?> AuthenticateAsync(string username, string password);
    Task<User> RegisterAsync(string username, string password);
    Task<bool> UsernameExistsAsync(string username);
    Task<User?> GetByIdAsync(Guid id);
    Task<User?> GetByUsernameAsync(string username);
}