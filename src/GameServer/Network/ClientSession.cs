using System.Net.Sockets;
using GameServer.Grains;
using GameServer.Packet;

namespace GameServer.Network;

public sealed class ClientSession
{
    private readonly TcpClient _client;
    private readonly NetworkStream _stream;
    private readonly PacketBuffer _recvBuffer = new();
    private readonly SemaphoreSlim _sendLock = new(1, 1);
    private readonly CancellationToken _ct;
    private readonly HashSet<int> _joinedChannels = new();
    private IPlayerSessionObserver? _observerRef;

    public Guid SessionId { get; } = Guid.NewGuid();
    public long PlayerId { get; set; }
    public DateTime LastReceivedAt { get; private set; } = DateTime.UtcNow;

    public void UpdateLastReceived() => LastReceivedAt = DateTime.UtcNow;

    public ClientSession(TcpClient client, CancellationToken ct)
    {
        _client = client;
        _stream = client.GetStream();
        _ct = ct;
    }

    public void JoinChannel(int channelId) => _joinedChannels.Add(channelId);

    public void LeaveChannel(int channelId) => _joinedChannels.Remove(channelId);

    public async Task AttachPlayerAsync(long playerId)
    {
        PlayerId = playerId;
        SessionManager.Instance.RegisterPlayerId(playerId, SessionId);
        _observerRef ??= OrleansClient.Instance.Factory.CreateObjectReference<IPlayerSessionObserver>(new PlayerSessionObserver(this));
        await OrleansClient.Instance.Factory.GetGrain<IPlayerGrain>(playerId).SubscribeAsync(_observerRef);
    }

    public async Task StartAsync()
    {
        try
        {
            await RecvLoopAsync();
        }
        finally
        {
            await DisconnectAsync();
        }
    }

    public async Task SendAsync(byte[] data)
    {
        await _sendLock.WaitAsync(_ct);
        try
        {
            await _stream.WriteAsync(data, _ct);
        }
        finally
        {
            _sendLock.Release();
        }
    }

    public async Task DisconnectAsync()
    {
        if (PlayerId != 0)
        {
            foreach (var channelId in _joinedChannels)
                await OrleansClient.Instance.Factory.GetGrain<IChannelGrain>(channelId).LeaveAsync(PlayerId);

            await OrleansClient.Instance.Factory.GetGrain<IPlayerGrain>(PlayerId).OnDisconnectAsync();
        }

        SessionManager.Instance.UnregisterPlayerId(PlayerId);
        SessionManager.Instance.Remove(SessionId);
        _stream.Close();
        _client.Close();
    }

    private async Task RecvLoopAsync()
    {
        var buffer = new byte[PacketBuffer.MaxPacketSize];

        while (!_ct.IsCancellationRequested)
        {
            int read = await _stream.ReadAsync(buffer, _ct);
            if (read == 0) break;

            UpdateLastReceived();
            if (!_recvBuffer.Write(buffer.AsSpan(0, read))) break;

            Memory<byte>? packet;
            while ((packet = _recvBuffer.TryAssemble()) != null)
                await PacketDispatcher.Instance.DispatchAsync(this, packet.Value);
        }
    }
}
