using GameServer.Grains;
using GameServer.Network;

namespace GameServer.Packet.Handler;

public static class CharacterStatHandler
{
    // payload: [4B level][4B hpMax][4B hp][4B mpMax][4B mp][1B isAlive][4B lastMapId]
    private const int PayloadSize = 25;

    public static Task HandleAsync(ClientSession session, Memory<byte> packet)
    {
        if (packet.Length < PacketHeader.HeaderSize + PayloadSize) return Task.CompletedTask;
        var p = packet.Span[PacketHeader.HeaderSize..];

        var stat = new PlayerState
        {
            PlayerId = session.PlayerId,
            Level = BitConverter.ToInt32(p),
            HpMax = BitConverter.ToInt32(p[4..]),
            Hp = BitConverter.ToInt32(p[8..]),
            MpMax = BitConverter.ToInt32(p[12..]),
            Mp = BitConverter.ToInt32(p[16..]),
            IsAlive = p[20] == 1,
            LastMapId = BitConverter.ToInt32(p[21..]),
        };

        return OrleansClient.Instance.Factory.GetGrain<IPlayerGrain>(session.PlayerId).UpdateStatAsync(stat);
    }
}
