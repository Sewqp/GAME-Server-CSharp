using System.Text;
using GameServer.DB;
using GameServer.DB.Repository;
using GameServer.Network;

namespace GameServer.Packet.Handler;

public static class LoginHandler
{
    private const int LoginLogInterval = 1000;
    private static readonly TimeSpan ReconnectTokenTtl = TimeSpan.FromSeconds(300);
    private static long _loginCount;

    public static async Task HandleAsync(ClientSession session, Memory<byte> packet)
    {
        if (packet.Length < PacketHeader.HeaderSize + 2) return;
        var payload = packet.Span[PacketHeader.HeaderSize..];

        ushort nameLen = BitConverter.ToUInt16(payload);
        if (payload.Length < 2 + nameLen) return;
        var name = Encoding.UTF8.GetString(payload.Slice(2, nameLen));

        long playerId = await PlayerRepository.Instance.GetOrCreateByNameAsync(name);
        await session.AttachPlayerAsync(playerId);

        var token = Guid.NewGuid().ToString();
        await RedisClient.Instance.Db.StringSetAsync($"reconnect:{token}", playerId, ReconnectTokenTtl);

        // payload: [1B success][8B playerId][36B token]
        var tokenBytes = Encoding.UTF8.GetBytes(token);
        var result = new byte[45];
        result[0] = 1;
        BitConverter.TryWriteBytes(result.AsSpan(1), playerId);
        tokenBytes.CopyTo(result.AsSpan(9));

        await session.SendAsync(PacketWriter.Build(PacketId.LoginResult, result));

        long loginCount = Interlocked.Increment(ref _loginCount);
        if (loginCount % LoginLogInterval == 0)
            Console.WriteLine($"[Login] Total logins so far: {loginCount}");
    }
}
