using GameServer.DB;

namespace GameServer.Network;

public sealed class HeartbeatManager
{
    private const int IntervalMs = 5_000;
    private const int TimeoutSeconds = 30;
    private const string SessionCountKey = "stats:session_count";
    private static readonly TimeSpan SessionCountTtl = TimeSpan.FromSeconds(15);

    private readonly CancellationToken _ct;

    public HeartbeatManager(CancellationToken ct) => _ct = ct;

    public async Task RunAsync()
    {
        while (!_ct.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(IntervalMs, _ct);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            var cutoff = DateTime.UtcNow.AddSeconds(-TimeoutSeconds);
            var timedOut = SessionManager.Instance.GetTimedOut(cutoff);
            foreach (var session in timedOut)
            {
                Console.WriteLine($"[Heartbeat] Timeout: {session.SessionId}");
                await session.DisconnectAsync();
            }

            await RedisClient.Instance.Db.StringSetAsync(
                SessionCountKey, SessionManager.Instance.Count, SessionCountTtl);
        }
    }
}
