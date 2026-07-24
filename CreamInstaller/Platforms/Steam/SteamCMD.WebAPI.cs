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
    private const int WebApiConcurrency = 12;
    private static readonly SemaphoreSlim WebApiGate = new(WebApiConcurrency, WebApiConcurrency);

    internal static async Task<CmdAppData> QueryWebAPI(string appId, bool isDlc = false)
    {
        if (Program.Canceled)
            return null;

        string cacheFile = ProgramData.AppInfoPath + @$"\{appId}.cmd.json";
        bool cachedExists = cacheFile.FileExists();

        if (!cachedExists || ProgramData.CheckCooldown(appId + ".cmd", isDlc ? CooldownDlc : CooldownGame))
        {
            await WebApiGate.WaitAsync();
            try
            {
                (string response, bool permanentFailure) =
                    await HttpClientManager.EnsureGet($"https://api.steamcmd.net/v1/info/{appId}");
                if (permanentFailure)
                {
                    ProgramData.Log.Info("[SteamAPI] SteamCMD web API query failed for " +
                                        appId + (isDlc ? " (DLC)" : "") +
                                        ": Permanent failure, aborting", LogDestination.Steam);
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
                                // If the API returned empty data (common is null),
                                // don't cache it fall through to the VDF/SteamCMD.exe path.
                                if (data.Common?.Name is null)
                                {
                                    ProgramData.Log.Info(
                                        "[SteamAPI] SteamCMD web API returned empty data for " + appId +
                                        (isDlc ? " (DLC)" : ""), LogDestination.Steam);
                                    return null;
                                }
                                try
                                {
                                    cacheFile.WriteFile(JsonConvert.SerializeObject(data, Formatting.Indented));
                                }
                                catch (Exception e)
                                {
                                    ProgramData.Log.Info("[SteamAPI] SteamCMD web API query failed for " +
                                                        appId + (isDlc ? " (DLC)" : "")
                                                        + ": Unsuccessful serialization (" + e.Message + ")", LogDestination.Steam);
                                }
                                return data;
                            }
                            else
                                ProgramData.Log.Info(
                                    "[SteamAPI] SteamCMD web API query failed for " + appId +
                                    (isDlc ? " (DLC)" : "")
                                    + ": No data", LogDestination.Steam);
                        }
                        else
                            ProgramData.Log.Info(
                                "[SteamAPI] SteamCMD web API query failed for " + appId +
                                (isDlc ? " (DLC)" : "")
                                + ": Status not success (" + appDetails?.Status + ")", LogDestination.Steam);
                    }
                    catch (Exception e)
                    {
                        ProgramData.Log.Info("[SteamAPI] SteamCMD web API query failed for " +
                                            appId + (isDlc ? " (DLC)" : "")
                                            + ": Unsuccessful deserialization (" + e.Message + ")", LogDestination.Steam);
                    }
                }
                else
                    ProgramData.Log.Info(
                        "[SteamAPI] SteamCMD web API query failed for " + appId +
                        (isDlc ? " (DLC)" : "") +
                        ": Response null", LogDestination.Steam);
            }
            finally
            {
                WebApiGate.Release();
            }
        }

        // Fall back to cached data if available
        if (cachedExists)
            try
            {
                return JsonConvert.DeserializeObject<CmdAppData>(cacheFile.ReadFile());
            }
            catch
            {
                cacheFile.DeleteFile();
            }

        return null;
    }
}