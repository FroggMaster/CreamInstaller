using System;
using System.Collections.Concurrent;
using System.Drawing;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;


namespace CreamInstaller.Utility;

internal static class HttpClientManager
{
    private static readonly object _lock = new();
    private static HttpClient _httpClient;
    private static SocketsHttpHandler _handler;

    internal static HttpClient HttpClient
    {
        get
        {
            lock (_lock)
            {
                return _httpClient;
            }
        }
    }

    private static readonly ConcurrentDictionary<string, string> HttpContentCache = new();

    internal static void Setup()
    {
        lock (_lock)
        {
            // If already set up, don't recreate to avoid socket exhaustion
            if (_httpClient != null)
                return;

            // Create a SocketsHttpHandler with proper pooling and lifecycle settings
            _handler = new SocketsHttpHandler
            {
                PooledConnectionLifetime = TimeSpan.FromMinutes(10), // Rotate connections every 10 minutes to respect DNS changes
                PooledConnectionIdleTimeout = TimeSpan.FromMinutes(2), // Close idle connections after 2 minutes
                MaxConnectionsPerServer = 10, // Reasonable concurrent connection limit
                EnableMultipleHttp2Connections = true
            };

            // Create HttpClient with the handler
            _httpClient = new HttpClient(_handler, disposeHandler: false)
            {
                Timeout = TimeSpan.FromSeconds(30) // 30 second timeout for all requests
            };

            // Set user agent based on context
            if (CreamInstaller.Platforms.Epic.EpicStore.EpicBool)
            {
                _httpClient.DefaultRequestHeaders.UserAgent.Add(new("EpicGamesLauncher", "18.9.0-45233261+++Portal+Release-Live"));
                CreamInstaller.Platforms.Epic.EpicStore.EpicBool = false;
            }
            else
            {
                _httpClient.DefaultRequestHeaders.UserAgent.Add(new(Program.Name, Program.Version));
            }

            _httpClient.DefaultRequestHeaders.AcceptLanguage.Add(new(CultureInfo.CurrentCulture.ToString()));
        }
    }

    internal static async Task<(string content, bool permanentFailure)> EnsureGet(string url)
    {
        try
        {
            using HttpRequestMessage request = new(HttpMethod.Get, url);
            using HttpResponseMessage response =
                await HttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            if (response.StatusCode is HttpStatusCode.NotModified &&
                HttpContentCache.TryGetValue(url, out string content))
                return (content, false);
            _ = response.EnsureSuccessStatusCode();
            content = await response.Content.ReadAsStringAsync();
            HttpContentCache[url] = content;
            return (content, false);
        }
        catch (HttpRequestException e)
        {
            if (e.StatusCode is not null)
            {
                int code = (int)e.StatusCode.Value;
                bool permanent = code is >= 400 and < 500 and not 429;
                string label = permanent ? "Permanent failure" : code == 429 ? "Too many requests" : "Get request failed";
                string statusInfo = $" (HTTP {code}{(permanent ? " - Permanent" : code == 429 ? " - Rate Limited" : "")})";
                ProgramData.LogSteam($"[SteamAPI] {label} to {url}{statusInfo}: {e.Message}");
                return (null, permanent);
            }
            ProgramData.LogSteam($"[SteamAPI] Get request failed to {url}: {e.Message}");
            return (null, false);
        }
        catch (TaskCanceledException)
        {
            ProgramData.LogSteam("[SteamAPI] Get request timed out for " + url);
            return (null, false);
        }
        catch (OperationCanceledException)
        {
            ProgramData.LogSteam("[SteamAPI] Get request was cancelled for " + url);
            return (null, false);
        }
        catch (Exception e)
        {
            ProgramData.LogSteam("[SteamAPI] Get request failed to " + url + ": " + e.Message);
            return (null, false);
        }
    }

    internal static async Task<Image> GetImageFromUrl(string url)
    {
        try
        {
            return new Bitmap(await HttpClient.GetStreamAsync(new Uri(url)));
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Creates a new HttpClient for isolated/one-off use cases.
    /// The caller is responsible for disposing the returned client.
    /// </summary>
    internal static HttpClient CreateIsolatedClient(TimeSpan? timeout = null)
    {
        var handler = new SocketsHttpHandler
        {
            PooledConnectionLifetime = TimeSpan.FromMinutes(2),
            PooledConnectionIdleTimeout = TimeSpan.FromMinutes(1),
            MaxConnectionsPerServer = 5
        };

        var client = new HttpClient(handler, disposeHandler: true)
        {
            Timeout = timeout ?? TimeSpan.FromSeconds(30)
        };

        client.DefaultRequestHeaders.UserAgent.ParseAdd($"{Program.Name}/{Program.Version}");
        return client;
    }

    internal static void Dispose()
    {
        lock (_lock)
        {
            _httpClient?.Dispose();
            _httpClient = null;

            _handler?.Dispose();
            _handler = null;
        }
    }
}