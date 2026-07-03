using Orleans;

namespace GameServer.Grains;

public interface IChannelGrain : IGrainWithIntegerKey
{
    Task JoinAsync(long playerId);
    Task LeaveAsync(long playerId);
    Task BroadcastAsync(long senderId, string message);
}
