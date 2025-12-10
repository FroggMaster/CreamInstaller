using System;
using System.Collections.Concurrent;
using System.Drawing;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
#if DEBUG
using CreamInstaller.Forms;
#endif

namespace CreamInstaller.Utility;

internal static class HttpClientManager
{
    internal static HttpClient HttpClient;

    private static readonly ConcurrentDictionary<string, string> HttpContentCache = new();

    /// <summary>
    /// Default timeout for HTTP requests in seconds.
    /// </summary>
    internal const int DefaultTimeoutSeconds = 30;

    /// <summary>
    /// Extended timeout for larger requests in seconds.
    /// </summary>
    internal const int ExtendedTimeoutSeconds = 120;

    internal static void Setup()
    {
        HttpClient = new()
        {
            Timeout = TimeSpan.FromSeconds(DefaultTimeoutSeconds)
        };
        if (CreamInstaller.Platforms.Epic.EpicStore.EpicBool)
        {
            HttpClient.DefaultRequestHeaders.UserAgent.Add(new("EpicGamesLauncher", "18.9.0-45233261+++Portal+Release-Live"));
            CreamInstaller.Platforms.Epic.EpicStore.EpicBool = false;
        }
        else
        {
            HttpClient.DefaultRequestHeaders.UserAgent.Add(new(Program.Name, Program.Version));
        }
        HttpClient.DefaultRequestHeaders.AcceptLanguage.Add(new(CultureInfo.CurrentCulture.ToString()));
    }

    internal static async Task<string> EnsureGet(string url, CancellationToken cancellationToken = default)
    {
        try
        {
            using HttpRequestMessage request = new(HttpMethod.Get, url);
            using HttpResponseMessage response =
                await HttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            if (response.StatusCode is HttpStatusCode.NotModified &&
                HttpContentCache.TryGetValue(url, out string content))
                return content;
            _ = response.EnsureSuccessStatusCode();
            content = await response.Content.ReadAsStringAsync(cancellationToken);
            HttpContentCache[url] = content;
            return content;
        }
        catch (OperationCanceledException)
        {
            // Includes TaskCanceledException (timeout) since it inherits from OperationCanceledException
#if DEBUG
            DebugForm.Current.Log("Request cancelled or timed out to " + url, LogTextBox.Warning);
#endif
            return null;
        }
        catch (HttpRequestException e)
        {
            if (e.StatusCode != HttpStatusCode.TooManyRequests)
            {
#if DEBUG
                DebugForm.Current.Log("Get request failed to " + url + ": " + e, LogTextBox.Warning);
#endif
                return null;
            }
#if DEBUG
            DebugForm.Current.Log("Too many requests to " + url, LogTextBox.Error);
#endif
            return null;
        }
#if DEBUG
        catch (Exception e)
        {
            DebugForm.Current.Log("Get request failed to " + url + ": " + e, LogTextBox.Warning);
            return null;
        }
#else
        catch
        {
            return null;
        }
#endif
    }

    internal static async Task<Image> GetImageFromUrl(string url, CancellationToken cancellationToken = default)
    {
        try
        {
            return new Bitmap(await HttpClient.GetStreamAsync(new Uri(url), cancellationToken));
        }
        catch (OperationCanceledException)
        {
            // Includes TaskCanceledException (timeout) since it inherits from OperationCanceledException
            return null;
        }
        catch
        {
            return null;
        }
    }

    internal static void Dispose() => HttpClient?.Dispose();
}