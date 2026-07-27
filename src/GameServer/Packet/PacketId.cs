namespace GameServer.Packet;

public enum PacketId : ushort
{
    LoginRequest       = 1000,
    LoginResult        = 1001,
    CharacterInfo      = 1002,
    CharacterStat      = 1003,
    EnterRoom          = 1008,
    LeaveRoom          = 1009,
    ChatMessage        = 2000,
    WhisperMessage     = 2001,
    MatchRequest       = 3000,
    MatchResult        = 3001,
    ItemAcquireRequest = 4000,
    ItemAcquireResult  = 4001,
    InventoryRequest   = 4002,
    InventoryResult    = 4003,
    ItemUseRequest     = 4004,
    ItemUseResult      = 4005,
    Heartbeat          = 9000,
    ReconnectRequest   = 9001,
    ReconnectResult    = 9002,
}
