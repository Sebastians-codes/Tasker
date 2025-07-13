namespace Tasker.Infrastructure.Data;

public interface IConnectionMonitor
{
    Task<bool> IsPostgresAvailableAsync();
    event EventHandler<bool> ConnectionStatusChanged;
}