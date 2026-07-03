using Orleans;
using GameServer.DB.Repository;

namespace GameServer.Grains;

public sealed class PlayerGrain : Grain, IPlayerGrain
{
    private PlayerState _state = new();
    private IPlayerSessionObserver? _observer;

    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        var model = await CharacterStatRepository.Instance.GetByIdAsync(this.GetPrimaryKeyLong());
        if (model != null)
            _state = PlayerState.FromModel(model);

        await base.OnActivateAsync(cancellationToken);
    }

    public Task<PlayerState> GetStateAsync() => Task.FromResult(_state);

    public async Task UpdateStatAsync(PlayerState stat)
    {
        _state = stat;
        await CharacterStatRepository.Instance.UpdateCacheAndMarkDirtyAsync(stat.ToModel());
    }

    public Task SendMessageAsync(byte[] packet) => _observer?.DeliverAsync(packet) ?? Task.CompletedTask;

    public Task SubscribeAsync(IPlayerSessionObserver observer)
    {
        _observer = observer;
        return Task.CompletedTask;
    }

    public Task UnsubscribeAsync()
    {
        _observer = null;
        return Task.CompletedTask;
    }

    public Task OnDisconnectAsync()
    {
        _observer = null;
        return Task.CompletedTask;
    }
}
