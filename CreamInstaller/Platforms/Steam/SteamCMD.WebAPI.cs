using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CreamInstaller.Utility;
using Newtonsoft.Json;

namespace CreamInstaller.Platforms.Steam;

internal static partial class SteamCMD
{
    private const int CooldownGame = 600;
    private const int CooldownDlc = 1200;

    internal static async Task<CmdAppData> QueryWebAPI(string appId, bool isDlc = false, int attempts = 0)
    {
        while (!Program.Canceled)
        {
            attempts++;
            string cacheFile = ProgramData.AppInfoPath + @$"\{appId}.cmd.json";
            bool cachedExists = cacheFile.FileExists();
            if (!cachedExists || ProgramData.CheckCooldown(appId + ".cmd", isDlc ? CooldownDlc : CooldownGame))
            {
                (string response, bool permanentFailure) =
                    await HttpClientManager.EnsureGet($"https://api.steamcmd.net/v1/info/{appId}");
if (permanentFailure)
                {
                    ProgramData.Log.Info("[SteamAPI] SteamCMD web API query failed on attempt #" + attempts + " for " +
                                        appId + (isDlc ? " (DLC)" : "") +
                                        ": Permanent failure, aborting retries", LogDestination.Steam);
                    return null;
                }
                if (response is not null)
                {
                    try
                    {
                        CmdAppDetails appDetails = JsonConvert.DeserializeObject<CmdAppDetails>(response);
                        if (appDetails is not null && appDetails.Status == "success")
                        {
                            if (appDetails.Data.Values.Count != 0)
                            {
                                CmdAppData data = appDetails.Data.Values.First();
                                try
                                {
                                    cacheFile.WriteFile(JsonConvert.SerializeObject(data, Formatting.Indented));
                                }
catch (Exception e)
                                {
                                    ProgramData.Log.Info("[SteamAPI] SteamCMD web API query failed on attempt #" + attempts +
                                                        " for " + appId + (isDlc ? " (DLC)" : "")
                                                        + ": Unsuccessful serialization (" + e.Message + ")", LogDestination.Steam);
                                }
                                return data;
                            }
                            else
                                ProgramData.Log.Info(
                                    "[SteamAPI] SteamCMD web API query failed on attempt #" + attempts + " for " + appId +
                                    (isDlc ? " (DLC)" : "")
                                    + ": No data", LogDestination.Steam);
                        }
                        else
                            ProgramData.Log.Info(
                                "[SteamAPI] SteamCMD web API query failed on attempt #" + attempts + " for " + appId +
                                (isDlc ? " (DLC)" : "")
                                + ": Status not success (" + appDetails?.Status + ")", LogDestination.Steam);
                    }
catch (Exception e)
                    {
                        ProgramData.Log.Info("[SteamAPI] SteamCMD web API query failed on attempt #" + attempts + " for " +
                                            appId + (isDlc ? " (DLC)" : "")
                                            + ": Unsuccessful deserialization (" + e.Message + ")", LogDestination.Steam);
                    }
                }
                else
                    ProgramData.Log.Info(
                        "[SteamAPI] SteamCMD web API query failed on attempt #" + attempts + " for " + appId +
                        (isDlc ? " (DLC)" : "") +
                        ": Response null", LogDestination.Steam);
            }

            if (cachedExists)
                try
                {
                    return JsonConvert.DeserializeObject<CmdAppData>(cacheFile.ReadFile());
                }
                catch
                {
                    cacheFile.DeleteFile();
                }

            if (isDlc)
                break;
            if (attempts > 3)
            {
                ProgramData.Log.Info("[SteamAPI] Failed to query SteamCMD web API after 3 tries: " + appId, LogDestination.Steam);
                break;
            }

            int delayMs = Math.Min(1000 * (int)Math.Pow(2, attempts - 1), 10000);
            await Task.Delay(delayMs + Random.Shared.Next(0, 1000));
        }

        return null;
    }
}