namespace GameServer.Config;

public sealed class ServerConfig
{
    public static readonly ServerConfig Instance = Load();

    public int TcpPort { get; init; }
    public string PostgresConnectionString { get; init; } = "";
    public string RedisConnectionString { get; init; } = "";
    public string OrleansClusterId { get; init; } = "";
    public string OrleansServiceId { get; init; } = "";
    public int OrleansSiloPort { get; init; }
    public int OrleansGatewayPort { get; init; }
    public string LmStudioUrl { get; init; } = "";
    public string? DiscordWebhookUrl { get; init; }

    private static ServerConfig Load() => new()
    {
        TcpPort = int.TryParse(Env("TCP_PORT"), out var p) ? p : 9000,
        PostgresConnectionString = Env("POSTGRES_CONN")
            ?? "Host=127.0.0.1;Port=5432;Database=game_server_cs;Username=postgres;Password=password;" +
               "Pooling=true;Minimum Pool Size=5;Maximum Pool Size=120;",
        RedisConnectionString = Env("REDIS_CONN") ?? "127.0.0.1:6379",
        OrleansClusterId = Env("ORLEANS_CLUSTER_ID") ?? "game-server-cluster",
        OrleansServiceId = Env("ORLEANS_SERVICE_ID") ?? "GameServerCS",
        OrleansSiloPort = int.TryParse(Env("ORLEANS_SILO_PORT"), out var sp) ? sp : 11111,
        OrleansGatewayPort = int.TryParse(Env("ORLEANS_GATEWAY_PORT"), out var gp) ? gp : 30000,
        LmStudioUrl = Env("LM_STUDIO_URL") ?? "http://localhost:1234/v1/chat/completions",
        DiscordWebhookUrl = Env("DISCORD_WEBHOOK_URL"),
    };

    private static string? Env(string key) => Environment.GetEnvironmentVariable(key);
}
