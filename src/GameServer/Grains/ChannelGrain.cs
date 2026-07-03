using System.Text;
using Orleans;
using GameServer.Packet;

namespace GameServer.Grains;

public sealed class ChannelGrain : Grain, IChannelGrain
{
    private readonly HashSet<long> _members = new();

    public Task JoinAsync(long playerId)
    {
        _members.Add(playerId);
        return Task.CompletedTask;
    }

    public Task LeaveAsync(long playerId)
    {
        _members.Remove(playerId);
        return Task.CompletedTask;
    }

    public async Task BroadcastAsync(long senderId, string message)
    {
        int channelId = (int)this.GetPrimaryKeyLong();
        var msgBytes = Encoding.UTF8.GetBytes(message);

        // payload: [4B channelId][8B senderId][2B msgLen][msgBytes]
        var payload = new byte[14 + msgBytes.Length];
        BitConverter.TryWriteBytes(payload.AsSpan(0), channelId);
        BitConverter.TryWriteBytes(payload.AsSpan(4), senderId);
        BitConverter.TryWriteBytes(payload.AsSpan(12), (ushort)msgBytes.Length);
        msgBytes.CopyTo(payload.AsSpan(14));

        var packet = PacketWriter.Build(PacketId.ChatMessage, payload);

        await Task.WhenAll(_members.Select(id =>
            GrainFactory.GetGrain<IPlayerGrain>(id).SendMessageAsync(packet)));
    }
}
