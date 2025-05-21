using System;
using System.Collections.Concurrent;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Exiled.API.Features;

namespace DamageLoggerPlugin
{
    internal static class DiscordService
    {
        private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(4) };
        private static readonly ConcurrentQueue<string> Queue = new();
        private static CancellationTokenSource _cts;
        private static string _authHeader;
        private static ulong _channelId;
        private static bool _debug;

        private const int FlushIntervalMs = 1200;
        private const int DiscordLimit = 1800;
        private const int MaxQueueLen = 500;

        internal static void Initialize(string botToken, ulong channelId, bool debug)
        {
            _authHeader = $"Bot {botToken.Replace("Bot ", string.Empty)}";
            _channelId = channelId;
            _debug = debug;

            _cts?.Cancel();
            _cts = new CancellationTokenSource();
            _ = Task.Run(() => WorkerAsync(_cts.Token));
        }

        internal static void Dispose() => _cts?.Cancel();

        internal static void Enqueue(string line)
        {
            if (_channelId == 0 || string.IsNullOrWhiteSpace(_authHeader) || string.IsNullOrWhiteSpace(line)) return;
            if (Queue.Count >= MaxQueueLen) return; // drop excessive to protect perf
            Queue.Enqueue(line);
        }

        private static async Task WorkerAsync(CancellationToken token)
        {
            var url = $"https://discord.com/api/v10/channels/{_channelId}/messages";
            var sb = new StringBuilder(DiscordLimit);
            var last = Environment.TickCount;

            while (!token.IsCancellationRequested)
            {
                if (Queue.TryDequeue(out var msg))
                {
                    if (sb.Length + msg.Length + 2 >= DiscordLimit)
                    {
                        await FlushAsync(sb.ToString(), url, token);
                        sb.Clear();
                        last = Environment.TickCount;
                    }
                    if (sb.Length > 0) sb.Append('\n');
                    sb.Append(msg);
                }
                else
                {
                    if (sb.Length > 0 && Environment.TickCount - last >= FlushIntervalMs)
                    {
                        await FlushAsync(sb.ToString(), url, token);
                        sb.Clear();
                        last = Environment.TickCount;
                    }
                    await Task.Delay(50, token).ConfigureAwait(false);
                }
            }
        }

        private static async Task FlushAsync(string payload, string url, CancellationToken token)
        {
            try
            {
                using var req = new HttpRequestMessage(HttpMethod.Post, url)
                {
                    Content = new StringContent(JsonSerializer.Serialize(new { content = payload }), Encoding.UTF8, "application/json")
                };
                req.Headers.TryAddWithoutValidation("Authorization", _authHeader);
                var resp = await Http.SendAsync(req, token).ConfigureAwait(false);
                if (_debug && !resp.IsSuccessStatusCode)
                {
                    var body = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
                    Log.Debug($"[DiscordService] {resp.StatusCode} {body}");
                }
            }
            catch (Exception ex) when (!token.IsCancellationRequested)
            {
                if (_debug) Log.Debug($"[DiscordService] Flush error: {ex.Message}");
            }
        }
    }
}
