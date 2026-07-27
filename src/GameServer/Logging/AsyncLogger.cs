using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Channels;

namespace GameServer.Logging;

public enum LogLevel { Info, Error }

public sealed record LogEntry(LogLevel Level, string Message, DateTime TimestampUtc);

public sealed class AsyncLogger
{
    public static readonly AsyncLogger Instance = new();

    private const string LogFilePath = "logs/server.log";
    private static readonly TimeSpan LlmCooldown = TimeSpan.FromSeconds(60);
    private static readonly HttpClient Http = new();

    private readonly Channel<LogEntry> _channel = Channel.CreateUnbounded<LogEntry>();
    private string _lmStudioUrl = "";
    private string? _discordWebhookUrl;
    private DateTime _lastLlmCallAtUtc = DateTime.MinValue;

    private AsyncLogger() { }

    public void Init(string lmStudioUrl, string? discordWebhookUrl, CancellationToken ct)
    {
        _lmStudioUrl = lmStudioUrl;
        _discordWebhookUrl = discordWebhookUrl;
        Directory.CreateDirectory(Path.GetDirectoryName(LogFilePath)!);
        _ = RunAsync(ct);
    }

    public void LogInfo(string message) => Enqueue(LogLevel.Info, message);

    public void LogError(string message, Exception? exception = null)
        => Enqueue(LogLevel.Error, exception != null ? $"{message}\n{exception}" : message);

    private void Enqueue(LogLevel level, string message)
        => _channel.Writer.TryWrite(new LogEntry(level, message, DateTime.UtcNow));

    private async Task RunAsync(CancellationToken ct)
    {
        try
        {
            await foreach (var entry in _channel.Reader.ReadAllAsync(ct))
            {
                WriteToConsoleAndFile(entry);
                if (entry.Level == LogLevel.Error)
                    await TryAnalyzeErrorAsync(entry, ct);
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

    private static void WriteToConsoleAndFile(LogEntry entry)
    {
        var line = $"[{entry.TimestampUtc:yyyy-MM-dd HH:mm:ss}] [{entry.Level}] {entry.Message}";
        Console.WriteLine(line);
        File.AppendAllText(LogFilePath, line + Environment.NewLine);
    }

    private async Task TryAnalyzeErrorAsync(LogEntry entry, CancellationToken ct)
    {
        // 60초 쿨다운 — 에러 폭발 시 LLM 과호출 방지
        if (DateTime.UtcNow - _lastLlmCallAtUtc < LlmCooldown) return;
        _lastLlmCallAtUtc = DateTime.UtcNow;

        try
        {
            var analysis = await RequestLlmAnalysisAsync(entry.Message, ct);
            if (Environment.GetEnvironmentVariable("DEBUG_LLM_PIPELINE") != null)
                Console.WriteLine($"[DEBUG] LLM analysis: {analysis}");
            if (analysis != null)
                await PostToDiscordAsync(entry.Message, analysis, ct);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AsyncLogger] LLM/Discord 파이프라인 실패: {ex.Message}");
        }
    }

    private async Task<string?> RequestLlmAnalysisAsync(string errorMessage, CancellationToken ct)
    {
        var requestBody = new
        {
            model = "local-model",
            messages = new[]
            {
                new
                {
                    role = "user",
                    content = "다음 서버 에러 로그를 한국어로 간단히 분석해줘 (원인 추정 + 대응 방안 1~2줄):\n" + errorMessage,
                },
            },
            temperature = 0.3,
        };

        using var response = await Http.PostAsJsonAsync(_lmStudioUrl, requestBody, ct);
        if (!response.IsSuccessStatusCode) return null;

        using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
        return doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString();
    }

    private async Task PostToDiscordAsync(string errorMessage, string analysis, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(_discordWebhookUrl)) return;

        var embed = new
        {
            embeds = new[]
            {
                new
                {
                    title = "서버 에러 감지",
                    color = 0xE74C3C,
                    fields = new[]
                    {
                        new { name = "에러", value = Truncate(errorMessage, 1000) },
                        new { name = "LLM 분석", value = Truncate(analysis, 1000) },
                    },
                    timestamp = DateTime.UtcNow.ToString("o"),
                },
            },
        };

        await Http.PostAsJsonAsync(_discordWebhookUrl, embed, ct);
    }

    private static string Truncate(string text, int maxLength)
        => text.Length <= maxLength ? text : text[..maxLength] + "...";
}
