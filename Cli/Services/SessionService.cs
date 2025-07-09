using System.Security.Cryptography;
using Tasker.Core.Interfaces;
using Tasker.Domain.Models;

namespace Tasker.Cli.Services;

public class SessionService(IUserRepository userRepository)
{
    private readonly IUserRepository _userRepository = userRepository;

    public async Task<UserSession> CreateSessionAsync(User user, int durationDays, bool autoLoginEnabled)
    {
        var token = GenerateSecureToken();
        var machineId = MachineIdService.GetMachineId();

        var session = new UserSession
        {
            UserId = user.Id,
            Token = token,
            ExpiresAt = DateTime.UtcNow.AddDays(durationDays),
            DurationDays = durationDays,
            AutoLoginEnabled = autoLoginEnabled,
            MachineId = machineId
        };

        await _userRepository.AddSessionAsync(session);
        await _userRepository.SaveChangesAsync();

        return session;
    }

    public async Task<UserSession?> ValidateSessionAsync(string token)
    {
        var session = await _userRepository.GetSessionByTokenAsync(token);
        var currentMachineId = MachineIdService.GetMachineId();

        if (session == null ||
            session.ExpiresAt <= DateTime.UtcNow ||
            session.MachineId != currentMachineId)
        {
            if (session != null)
            {
                await _userRepository.DeleteSessionAsync(session);
                await _userRepository.SaveChangesAsync();
            }
            return null;
        }

        return session;
    }

    public async Task InvalidateSessionAsync(string token)
    {
        var session = await _userRepository.GetSessionByTokenAsync(token);
        if (session != null)
        {
            await _userRepository.DeleteSessionAsync(session);
            await _userRepository.SaveChangesAsync();
        }
    }

    public async Task UpdateSessionSettingsAsync(string token, int? durationDays = null, bool? autoLoginEnabled = null)
    {
        var session = await _userRepository.GetSessionByTokenAsync(token);
        if (session != null)
        {
            if (durationDays.HasValue)
            {
                session.DurationDays = durationDays.Value;
                session.ExpiresAt = DateTime.UtcNow.AddDays(durationDays.Value);
            }

            if (autoLoginEnabled.HasValue)
            {
                session.AutoLoginEnabled = autoLoginEnabled.Value;
            }

            await _userRepository.UpdateSessionAsync(session);
            await _userRepository.SaveChangesAsync();
        }
    }

    private static string GenerateSecureToken()
    {
        using var rng = RandomNumberGenerator.Create();
        var bytes = new byte[32];
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes);
    }
}