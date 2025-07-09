using Tasker.Domain.Models;
using System.Security;

namespace Tasker.Cli.Services;

public interface IUserService
{
    Task<User?> AuthenticateAsync(string username, SecureString password);
    Task<User> RegisterAsync(string username, SecureString password);
    Task<bool> UsernameExistsAsync(string username);
    Task<User?> GetByIdAsync(Guid id);
    Task<User?> GetByUsernameAsync(string username);
}