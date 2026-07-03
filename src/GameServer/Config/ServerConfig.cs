namespace GameServer.Config;

public sealed class ServerConfig
{
    public static readonly ServerConfig Instance = Load();

    public int TcpPort { get; init; }
    public string MySqlConnectionString { get; init; } = "";
    public string RedisConnectionString { get; init; } = "";
    public string OrleansClusterId { get; init; } = "";
    public string OrleansServiceId { get; init; } = "";
    public int OrleansSiloPort { get; init; }
    public int OrleansGatewayPort { get; init; }

    private static ServerConfig Load() => new()
    {
        TcpPort = int.TryParse(Env("TCP_PORT"), out var p) ? p : 9000,
        MySqlConnectionString = Env("MYSQL_CONN")
            ?? "Server=127.0.0.1;Port=3306;Database=game_server_cs;Uid=root;Pwd=password;" +
               "Pooling=true;MinimumPoolSize=5;MaximumPoolSize=100;CharacterSet=utf8mb4;",
        RedisConnectionString = Env("REDIS_CONN") ?? "127.0.0.1:6379",
        OrleansClusterId = Env("ORLEANS_CLUSTER_ID") ?? "game-server-cluster",
        OrleansServiceId = Env("ORLEANS_SERVICE_ID") ?? "GameServerCS",
        OrleansSiloPort = int.TryParse(Env("ORLEANS_SILO_PORT"), out var sp) ? sp : 11111,
        OrleansGatewayPort = int.TryParse(Env("ORLEANS_GATEWAY_PORT"), out var gp) ? gp : 30000,
    };

    private static string? Env(string key) => Environment.GetEnvironmentVariable(key);
}
