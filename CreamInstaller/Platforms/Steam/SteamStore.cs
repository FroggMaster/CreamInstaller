using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CreamInstaller.Utility;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
#if DEBUG
using System;
using CreamInstaller.Forms;
#endif

namespace CreamInstaller.Platforms.Steam;

internal static class SteamStore
{
    private const int CooldownGame = 600;
    private const int CooldownDlc = 1200;

#if DEBUG
    private static string FormatErrorLog(int attempts, string appId, string gameName, bool isDlc, string reason, 
        string parentGameName = null, string parentGameAppId = null)
    {
        if (isDlc && parentGameName != null && parentGameAppId != null)
        {
            return $"[SteamQuery][Attempt {attempts}][FAILED]\n" +
                   $"BaseGame: \"{parentGameName}\" ({parentGameAppId})\n" +
                   $"DLC: \"{gameName}\" ({appId})\n" +
                   $"Type: DLC\n" +
                   $"Reason: {reason}\n" +
                   "-------";
        }

        string type = isDlc ? "DLC" : "Game";
        return $"[SteamQuery][Attempt {attempts}][FAILED] AppId: {appId} | Name: \"{gameName}\" | Type: {type} | Reason: {reason}";
    }
#endif

    internal static async Task<HashSet<string>> ParseDlcAppIds(StoreAppData storeAppData)
        => await Task.Run(() =>
        {
            HashSet<string> dlcIds = new();
            if (storeAppData.DLC is null)
                return dlcIds;
            foreach (string dlcId in from appId in storeAppData.DLC
                     where appId > 0
                     select appId.ToString(CultureInfo.InvariantCulture))
                _ = dlcIds.Add(dlcId);
            return dlcIds;
        });

    internal static async Task<StoreAppData> QueryStoreAPI(string appId, bool isDlc = false, int attempts = 0, string parentGameName = null, string parentGameAppId = null)
    {
        string gameName = "Unknown";
        while (!Program.Canceled)
        {
            attempts++;
            string cacheFile = ProgramData.AppInfoPath + @$"\{appId}.json";
            bool cachedExists = cacheFile.FileExists();
            if (!cachedExists || ProgramData.CheckCooldown(appId, isDlc ? CooldownDlc : CooldownGame))
            {
                string response =
                    await HttpClientManager.EnsureGet($"https://store.steampowered.com/api/appdetails?appids={appId}");
                if (response is not null)
                {
                    Dictionary<string, JToken> apps =
                        JsonConvert.DeserializeObject<Dictionary<string, JToken>>(response);
                    if (apps is not null)
                        foreach (KeyValuePair<string, JToken> app in apps)
                            try
                            {
                                StoreAppDetails storeAppDetails =
                                    JsonConvert.DeserializeObject<StoreAppDetails>(app.Value.ToString());
                                if (storeAppDetails is not null)
                                {
                                    StoreAppData data = storeAppDetails.Data;
                                    if (data?.Name is not null)
                                        gameName = data.Name;

                                    if (!storeAppDetails.Success)
                                    {
#if DEBUG
                                        DebugForm.Current.Log(
                                            FormatErrorLog(attempts, appId, gameName, isDlc, "Query unsuccessful", parentGameName, parentGameAppId),
                                            LogTextBox.Warning);
#endif
                                        if (data is null)
                                            return null;
                                    }

                                    if (data is not null)
                                    {
                                        try
                                        {
                                            cacheFile.WriteFile(JsonConvert.SerializeObject(data, Formatting.Indented));
                                        }
                                        catch
#if DEBUG
                                            (Exception e)
                                        {
                                            DebugForm.Current.Log(
                                                FormatErrorLog(attempts, appId, gameName, isDlc, $"Unsuccessful serialization ({e.Message})", parentGameName, parentGameAppId));
                                        }
#else
                                        {
                                            // ignored
                                        }
#endif
                                        return data;
                                    }
#if DEBUG
                                    DebugForm.Current.Log(
                                        FormatErrorLog(attempts, appId, gameName, isDlc, "Response data null", parentGameName, parentGameAppId));
#endif
                                }
#if DEBUG
                                else
                                {
                                    DebugForm.Current.Log(
                                        FormatErrorLog(attempts, appId, gameName, isDlc, "Response details null", parentGameName, parentGameAppId));
                                }
#endif
                            }
                            catch
#if DEBUG
                                (Exception e)
                            {
                                DebugForm.Current.Log(
                                    FormatErrorLog(attempts, appId, gameName, isDlc, $"Unsuccessful deserialization ({e.Message})", parentGameName, parentGameAppId));
                            }
#else
                            {
                                // ignored
                            }
#endif
#if DEBUG
                    else
                    {
                        DebugForm.Current.Log(
                            FormatErrorLog(attempts, appId, gameName, isDlc, "Response deserialization null", parentGameName, parentGameAppId));
                    }
#endif
                }
#if DEBUG
                else
                {
                    DebugForm.Current.Log(
                        FormatErrorLog(attempts, appId, gameName, isDlc, "Null or empty response", parentGameName, parentGameAppId),
                        LogTextBox.Warning);
                }
#endif
            }

            if (cachedExists)
                try
                {
                    return JsonConvert.DeserializeObject<StoreAppData>(cacheFile.ReadFile());
                }
                catch
                {
                    cacheFile.DeleteFile();
                }

            if (isDlc)
                break;
            if (attempts > 10)
            {
#if DEBUG
                DebugForm.Current.Log(
                    FormatErrorLog(attempts, appId, gameName, isDlc, "Maximum retry attempts exceeded (10)", parentGameName, parentGameAppId));
#endif
                break;
            }

            Thread.Sleep(1000);
        }

        return null;
    }
}