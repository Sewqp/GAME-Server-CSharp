using System.Text;
using GameServer.Grains;
using GameServer.Network;

namespace GameServer.Packet.Handler;

public static class ChatHandler
{
    public static async Task HandleEnterRoomAsync(ClientSession session, Memory<byte> packet)
    {
        if (packet.Length < PacketHeader.HeaderSize + 4) return;
        int channelId = BitConverter.ToInt32(packet.Span[PacketHeader.HeaderSize..]);
        session.JoinChannel(channelId);
        await OrleansClient.Instance.Factory.GetGrain<IChannelGrain>(channelId).JoinAsync(session.PlayerId);
    }

    public static async Task HandleLeaveRoomAsync(ClientSession session, Memory<byte> packet)
    {
        if (packet.Length < PacketHeader.HeaderSize + 4) return;
        int channelId = BitConverter.ToInt32(packet.Span[PacketHeader.HeaderSize..]);
        session.LeaveChannel(channelId);
        await OrleansClient.Instance.Factory.GetGrain<IChannelGrain>(channelId).LeaveAsync(session.PlayerId);
    }

    public static async Task HandleChatAsync(ClientSession session, Memory<byte> packet)
    {
        // payload: [4B channelId][2B msgLen][msgBytes]
        if (packet.Length < PacketHeader.HeaderSize + 6) return;
        var payload = packet.Span[PacketHeader.HeaderSize..];
        int channelId = BitConverter.ToInt32(payload);
        ushort msgLen = BitConverter.ToUInt16(payload[4..]);
        if (payload.Length < 6 + msgLen) return;
        var message = Encoding.UTF8.GetString(payload.Slice(6, msgLen));
        await OrleansClient.Instance.Factory.GetGrain<IChannelGrain>(channelId).BroadcastAsync(session.PlayerId, message);
    }

    public static async Task HandleWhisperAsync(ClientSession session, Memory<byte> packet)
    {
        // payload: [8B targetPlayerId][2B msgLen][msgBytes]
        if (packet.Length < PacketHeader.HeaderSize + 10) return;
        var payload = packet.Span[PacketHeader.HeaderSize..];
        long targetPlayerId = BitConverter.ToInt64(payload);
        ushort msgLen = BitConverter.ToUInt16(payload[8..]);
        if (payload.Length < 10 + msgLen) return;
        var message = Encoding.UTF8.GetString(payload.Slice(10, msgLen));

        // payload: [8B senderId][2B msgLen][msgBytes]
        var msgBytes = Encoding.UTF8.GetBytes(message);
        var outPayload = new byte[10 + msgBytes.Length];
        BitConverter.TryWriteBytes(outPayload.AsSpan(0), session.PlayerId);
        BitConverter.TryWriteBytes(outPayload.AsSpan(8), (ushort)msgBytes.Length);
        msgBytes.CopyTo(outPayload.AsSpan(10));
        var outPacket = PacketWriter.Build(PacketId.WhisperMessage, outPayload);

        await OrleansClient.Instance.Factory.GetGrain<IPlayerGrain>(targetPlayerId).SendMessageAsync(outPacket);
    }
}
