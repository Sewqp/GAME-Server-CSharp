using Orleans;

namespace GameServer.Grains;

public interface IPlayerGrain : IGrainWithIntegerKey
{
    Task<PlayerState> GetStateAsync();
    Task UpdateStatAsync(PlayerState stat);
    Task SendMessageAsync(byte[] packet);
    Task SubscribeAsync(IPlayerSessionObserver observer);
    Task UnsubscribeAsync();
    Task OnDisconnectAsync();
}
