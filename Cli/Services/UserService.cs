using BCrypt.Net;
using Tasker.Core.Interfaces;
using Tasker.Domain.Models;
using System.Security;
using System.Runtime.InteropServices;

namespace Tasker.Cli.Services;

public class UserService(IUserRepository userRepository) : IUserService
{
    private readonly IUserRepository _userRepository = userRepository;

    public async Task<User?> AuthenticateAsync(string username, SecureString password)
    {
        var user = await _userRepository.GetByUsernameAsync(username);
        if (user == null)
        {
            password.Dispose();
            return null;
        }

        if (user.LockoutEndTime.HasValue && user.LockoutEndTime.Value > DateTime.UtcNow)
        {
            password.Dispose();
            throw new InvalidOperationException($"Account is locked out until {user.LockoutEndTime.Value.ToLocalTime():yyyy-MM-dd HH:mm:ss}");
        }

        var passwordString = SecureStringToString(password);
        password.Dispose();

        try
        {
            if (BCrypt.Net.BCrypt.Verify(passwordString, user.PasswordHash))
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
        finally
        {
            if (passwordString != null)
            {
                unsafe
                {
                    fixed (char* ptr = passwordString)
                    {
                        for (int i = 0; i < passwordString.Length; i++)
                        {
                            ptr[i] = '\0';
                        }
                    }
                }
            }
        }
    }

    public async Task<User> RegisterAsync(string username, SecureString password)
    {
        if (await _userRepository.UsernameExistsAsync(username))
        {
            password.Dispose();
            throw new InvalidOperationException("Username already exists");
        }

        var passwordString = SecureStringToString(password);
        password.Dispose();

        try
        {
            ValidatePassword(passwordString);

            var user = new User
            {
                Username = username,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(passwordString, 12)
            };

            await _userRepository.AddAsync(user);
            await _userRepository.SaveChangesAsync();
            return user;
        }
        finally
        {
            if (passwordString != null)
            {
                unsafe
                {
                    fixed (char* ptr = passwordString)
                    {
                        for (int i = 0; i < passwordString.Length; i++)
                        {
                            ptr[i] = '\0';
                        }
                    }
                }
            }
        }
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

    private static string SecureStringToString(SecureString secureString)
    {
        IntPtr ptr = IntPtr.Zero;
        try
        {
            ptr = Marshal.SecureStringToGlobalAllocUnicode(secureString);
            return Marshal.PtrToStringUni(ptr) ?? string.Empty;
        }
        finally
        {
            if (ptr != IntPtr.Zero)
            {
                Marshal.ZeroFreeGlobalAllocUnicode(ptr);
            }
        }
    }
}