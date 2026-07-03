using Orleans;
using GameServer.DB.Model;

namespace GameServer.Grains;

[GenerateSerializer]
public sealed class PlayerState
{
    [Id(0)] public long PlayerId { get; set; }
    [Id(1)] public int Level { get; set; }
    [Id(2)] public int HpMax { get; set; }
    [Id(3)] public int Hp { get; set; }
    [Id(4)] public int MpMax { get; set; }
    [Id(5)] public int Mp { get; set; }
    [Id(6)] public bool IsAlive { get; set; }
    [Id(7)] public int LastMapId { get; set; }

    public static PlayerState FromModel(CharacterStatModel model) => new()
    {
        PlayerId = model.PlayerId,
        Level = model.Level,
        HpMax = model.HpMax,
        Hp = model.Hp,
        MpMax = model.MpMax,
        Mp = model.Mp,
        IsAlive = model.IsAlive,
        LastMapId = model.LastMapId,
    };

    public CharacterStatModel ToModel() => new()
    {
        PlayerId = PlayerId,
        Level = Level,
        HpMax = HpMax,
        Hp = Hp,
        MpMax = MpMax,
        Mp = Mp,
        IsAlive = IsAlive,
        LastMapId = LastMapId,
    };
}
