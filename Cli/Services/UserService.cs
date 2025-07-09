using BCrypt.Net;
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

        if (user.LockoutEndTime.HasValue && user.LockoutEndTime.Value > DateTime.UtcNow)
        {
            throw new InvalidOperationException($"Account is locked out until {user.LockoutEndTime.Value.ToLocalTime():yyyy-MM-dd HH:mm:ss}");
        }

        if (BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
        {
            if (user.FailedLoginAttempts > 0)
            {
                user.FailedLoginAttempts = 0;
                user.LockoutEndTime = null;
                await _userRepository.UpdateAsync(user);
                await _userRepository.SaveChangesAsync();
            }
            return user;
        }
        else
        {
            user.FailedLoginAttempts++;

            if (user.FailedLoginAttempts >= 5)
                user.LockoutEndTime = DateTime.UtcNow.AddMinutes(15);

            await _userRepository.UpdateAsync(user);
            await _userRepository.SaveChangesAsync();

            return null;
        }
    }

    public async Task<User> RegisterAsync(string username, string password)
    {
        if (await _userRepository.UsernameExistsAsync(username))
            throw new InvalidOperationException("Username already exists");

        ValidatePassword(password);

        var user = new User
        {
            Username = username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password, 12)
        };

        await _userRepository.AddAsync(user);
        await _userRepository.SaveChangesAsync();
        return user;
    }

    public async Task<bool> UsernameExistsAsync(string username) =>
        await _userRepository.UsernameExistsAsync(username);

    public async Task<User?> GetByIdAsync(Guid id) =>
        await _userRepository.GetByIdAsync(id);

    public async Task<User?> GetByUsernameAsync(string username) =>
        await _userRepository.GetByUsernameAsync(username);

    private static void ValidatePassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
            throw new ArgumentException("Password cannot be empty");

        if (password.Length < 8)
            throw new ArgumentException("Password must be at least 8 characters long");

        if (!password.Any(char.IsUpper))
            throw new ArgumentException("Password must contain at least one uppercase letter");

        if (!password.Any(char.IsLower))
            throw new ArgumentException("Password must contain at least one lowercase letter");

        if (!password.Any(char.IsDigit))
            throw new ArgumentException("Password must contain at least one digit");

        if (!password.Any(ch => !char.IsLetterOrDigit(ch)))
            throw new ArgumentException("Password must contain at least one special character");
    }
}