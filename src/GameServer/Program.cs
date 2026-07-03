using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Orleans.Configuration;
using StackExchange.Redis;
using GameServer.Config;
using GameServer.DB;
using GameServer.Grains;
using GameServer.Network;
using GameServer.Packet;
using GameServer.Packet.Handler;

var config = ServerConfig.Instance;

DbConnectionPool.Instance.Init(config.MySqlConnectionString);
Console.WriteLine("[DB] MySQL connection pool ready.");

RedisClient.Instance.Init(config.RedisConnectionString);
Console.WriteLine("[DB] Redis connected.");

var hostBuilder = Host.CreateApplicationBuilder(args);
hostBuilder.UseOrleans(silo =>
{
    silo.Configure<ClusterOptions>(o =>
    {
        o.ClusterId = config.OrleansClusterId;
        o.ServiceId = config.OrleansServiceId;
    });
    silo.UseRedisClustering(o =>
    {
        o.ConfigurationOptions = ConfigurationOptions.Parse(config.RedisConnectionString);
        o.ConfigurationOptions.AbortOnConnectFail = false;
    });
    silo.ConfigureEndpoints(siloPort: config.OrleansSiloPort, gatewayPort: config.OrleansGatewayPort);
});

using var host = hostBuilder.Build();
await host.StartAsync();
OrleansClient.Instance.Init(host.Services.GetRequiredService<IGrainFactory>());
Console.WriteLine($"[Orleans] Silo active. Cluster={config.OrleansClusterId}");

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};

_ = new SyncWorker(cts.Token).RunAsync();
_ = new HeartbeatManager(cts.Token).RunAsync();

var dispatcher = PacketDispatcher.Instance;
dispatcher.Register(PacketId.LoginRequest,     LoginHandler.HandleAsync);
dispatcher.Register(PacketId.Heartbeat,        HeartbeatHandler.HandleAsync);
dispatcher.Register(PacketId.ReconnectRequest, ReconnectHandler.HandleAsync);
dispatcher.Register(PacketId.EnterRoom,        ChatHandler.HandleEnterRoomAsync);
dispatcher.Register(PacketId.LeaveRoom,        ChatHandler.HandleLeaveRoomAsync);
dispatcher.Register(PacketId.ChatMessage,      ChatHandler.HandleChatAsync);
dispatcher.Register(PacketId.WhisperMessage,   ChatHandler.HandleWhisperAsync);
dispatcher.Register(PacketId.MatchRequest,     MatchHandler.HandleAsync);
dispatcher.Register(PacketId.CharacterStat,    CharacterStatHandler.HandleAsync);

var server = new TcpServer(config.TcpPort, cts.Token);
await server.StartAsync();
await host.StopAsync();
