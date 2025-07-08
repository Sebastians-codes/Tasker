using Microsoft.EntityFrameworkCore;

namespace Tasker.Infrastructure.Data;

public class ConnectionMonitor : IConnectionMonitor
{
    private readonly PostgresDbContext _postgresContext;
    private bool _lastKnownStatus = true;
    private readonly Timer _connectionTimer;

    public event EventHandler<bool>? ConnectionStatusChanged;

    public ConnectionMonitor(PostgresDbContext postgresContext)
    {
        _postgresContext = postgresContext;
        
        // Check connection every 30 seconds
        _connectionTimer = new Timer(CheckConnectionStatus, null, TimeSpan.Zero, TimeSpan.FromSeconds(30));
    }

    public async Task<bool> IsPostgresAvailableAsync()
    {
        try
        {
            await _postgresContext.Database.OpenConnectionAsync();
            await _postgresContext.Database.CloseConnectionAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    private async void CheckConnectionStatus(object? state)
    {
        var currentStatus = await IsPostgresAvailableAsync();
        
        if (currentStatus != _lastKnownStatus)
        {
            _lastKnownStatus = currentStatus;
            ConnectionStatusChanged?.Invoke(this, currentStatus);
        }
    }

    public void Dispose()
    {
        _connectionTimer?.Dispose();
    }
}