using BellwoodGlobal.Mobile.Models;

namespace BellwoodGlobal.Mobile.Services;

/// <summary>
/// Service for monitoring ride status updates via polling.
/// Can be extended to support push notifications (FCM/SignalR) in the future.
/// </summary>
public sealed class RideStatusService : IRideStatusService, IDisposable
{
    private readonly IAdminApi _adminApi;
    private CancellationTokenSource? _pollingCts;
    private Task? _pollingTask;
    private string? _currentRideId;
    private string? _lastKnownStatus;

    public event EventHandler<RideStatusChangedEventArgs>? StatusChanged;

    public RideStatusService(IAdminApi adminApi)
    {
        _adminApi = adminApi;
    }

    public async Task StartMonitoringAsync(string rideId, int pollingIntervalMs = 30000)
    {
        StopMonitoring();

        _currentRideId = rideId;
        _lastKnownStatus = null;

        _pollingCts = new CancellationTokenSource();
        _pollingTask = PollStatusAsync(rideId, pollingIntervalMs, _pollingCts.Token);

        await Task.CompletedTask;
    }

    public void StopMonitoring()
    {
        if (_pollingCts != null)
        {
            _pollingCts.Cancel();
            _pollingCts.Dispose();
            _pollingCts = null;
        }

        _pollingTask = null;
        _currentRideId = null;
        _lastKnownStatus = null;

#if DEBUG
        System.Diagnostics.Debug.WriteLine("[RideStatusService] Monitoring stopped");
#endif
    }

    public async Task<string?> GetCurrentStatusAsync(string rideId)
    {
        try
        {
            var booking = await _adminApi.GetBookingAsync(rideId);
            return booking?.Status;
        }
        catch (Exception ex)
        {
#if DEBUG
            System.Diagnostics.Debug.WriteLine($"[RideStatusService] Error fetching status: {ex.Message}");
#endif
            return null;
        }
    }

    private async Task PollStatusAsync(string rideId, int intervalMs, CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                var currentStatus = await GetCurrentStatusAsync(rideId);

                if (currentStatus != null && currentStatus != _lastKnownStatus)
                {
                    var oldStatus = _lastKnownStatus ?? "";
                    _lastKnownStatus = currentStatus;

                    // Fire status changed event
                    StatusChanged?.Invoke(this, new RideStatusChangedEventArgs
                    {
                        RideId = rideId,
                        OldStatus = oldStatus,
                        NewStatus = currentStatus
                    });

#if DEBUG
                    System.Diagnostics.Debug.WriteLine(
                        $"[RideStatusService] Status changed: {oldStatus} -> {currentStatus}");
#endif
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
#if DEBUG
                System.Diagnostics.Debug.WriteLine($"[RideStatusService] Poll error: {ex.Message}");
#endif
            }

            try
            {
                await Task.Delay(intervalMs, ct);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

#if DEBUG
        System.Diagnostics.Debug.WriteLine("[RideStatusService] Polling loop ended");
#endif
    }

    public void Dispose()
    {
        StopMonitoring();
    }
}
