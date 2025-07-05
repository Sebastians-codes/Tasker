using System.Security.Cryptography;
using System.Text;
using Tasker.Core.Interfaces;
using Tasker.Domain.Models;

namespace Tasker.Cli.Services;

public class UserService(IUserRepository userRepository) : IUserService
{
    private readonly IUserRepository _userRepository = userRepository;

    public async Task<User?> AuthenticateAsync(string username, string password)
    {
        var user = await _userRepository.GetByUsernameAsync(username);
        if (user == null)
            return null;

        var hashedPassword = HashPassword(password);
        return user.PasswordHash == hashedPassword ? user : null;
    }

    public async Task<User> RegisterAsync(string username, string password)
    {
        if (await _userRepository.UsernameExistsAsync(username))
            throw new InvalidOperationException("Username already exists");

        var user = new User
        {
            Username = username,
            PasswordHash = HashPassword(password)
        };

        await _userRepository.AddAsync(user);
        await _userRepository.SaveChangesAsync();
        return user;
    }

    public async Task<bool> UsernameExistsAsync(string username)
    {
        return await _userRepository.UsernameExistsAsync(username);
    }

    public async Task<User?> GetByIdAsync(int id)
    {
        return await _userRepository.GetByIdAsync(id);
    }

    private static string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(hashedBytes);
    }
}