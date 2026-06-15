using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using CreamInstaller;
using Newtonsoft.Json;

namespace CreamInstaller.Utility;

internal enum InstalledUnlocker
{
    None = 0,
    CreamAPI,
    SmokeAPI,
    ScreamAPI,
    UplayR1,
    UplayR2,
    Koaloader
}

internal sealed class InstalledDlcRecord
{
    public string DlcType { get; set; }
    public string Id { get; set; }
    public string Name { get; set; }
}

internal sealed class InstalledGameRecord
{
    public Platform Platform { get; set; }
    public string Id { get; set; }
    public string Name { get; set; }
    public string RootDirectory { get; set; }
    public InstalledUnlocker Unlocker { get; set; }
    public bool UseProxy { get; set; }

    [JsonProperty("Proxy DLL Name")]
    public string ProxyDllName { get; set; }

    public bool UseExtraProtection { get; set; }
    public List<InstalledDlcRecord> Dlc { get; set; } = [];
}

internal static class ProgramData
{
    private static readonly string DirectoryPathOld =
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\CreamInstaller";

    internal static readonly string DirectoryPath =
        Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) + @"\CreamInstaller";

    internal static readonly string AppInfoPath = DirectoryPath + @"\appinfo";
    private static readonly string AppInfoVersionPath = AppInfoPath + @"\version.txt";

    private static readonly Version MinimumAppInfoVersion = Version.Parse("4.7.0.0");

    internal static readonly string CooldownPath = DirectoryPath + @"\cooldown";

    private static readonly string OldProgramChoicesPath = DirectoryPath + @"\choices.txt";
    private static readonly string ProgramChoicesPath = DirectoryPath + @"\choices.json";
    private static readonly string DlcChoicesPath = DirectoryPath + @"\dlc.json";
    private static readonly string KoaloaderProxyChoicesPath = DirectoryPath + @"\proxies.json";
    private static readonly string ExtraProtectionChoicesPath = DirectoryPath + @"\extraprotection.json";
    private static readonly string InstalledGamesPath = DirectoryPath + @"\installed.json";

    internal static readonly string ScanLogPath = Path.Combine(DirectoryPath, "game-scan.log");
internal static readonly string SteamLogPath = Path.Combine(DirectoryPath, "cream-steam.log");
internal static readonly string AppLogPath = Path.Combine(DirectoryPath, "CreamInstaller.log");

internal static event Action<string> OnLogWarning;
internal static event Action<string> OnLogError;

    private static readonly object LogLock = new();

    internal static void Log(string message)
    {
        try
        {
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture);
            string entry = $"[{timestamp}] {message}{Environment.NewLine}";
            lock (LogLock)
                File.AppendAllText(ScanLogPath, entry, Encoding.UTF8);
        }
        catch
        {
            // ignored; logging must never crash the application
        }
    }

    internal static void LogSteam(string message)
    {
        try
        {
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture);
            string entry = $"[{timestamp}] {message}{Environment.NewLine}";
            lock (LogLock)
                File.AppendAllText(SteamLogPath, entry, Encoding.UTF8);
        }
        catch
        {
            // ignored; logging must never crash the application
        }
    }

    internal static void LogWarning(string message)
    {
        try
        {
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture);
            string entry = $"[{timestamp}] [WARN] {message}{Environment.NewLine}";
            lock (LogLock)
                File.AppendAllText(AppLogPath, entry, Encoding.UTF8);
        }
        catch
        {
            // ignored; logging must never crash the application
        }
        OnLogWarning?.Invoke(message);
    }

    internal static void LogError(string message, Exception ex = null)
    {
        try
        {
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture);
            string entry = ex is not null
                ? $"[{timestamp}] [ERROR] {message}{Environment.NewLine}[{timestamp}] [ERROR]   Exception: {ex}{Environment.NewLine}"
                : $"[{timestamp}] [ERROR] {message}{Environment.NewLine}";
            lock (LogLock)
                File.AppendAllText(AppLogPath, entry, Encoding.UTF8);
        }
        catch
        {
            // ignored; logging must never crash the application
        }
        OnLogError?.Invoke(message);
    }

    internal static void ClearLog()
    {
        try
        {
            if (File.Exists(ScanLogPath))
                File.Delete(ScanLogPath);
        }
        catch
        {
            // ignored
        }
    }

    internal static async Task Setup(Form form = null)
        => await Task.Run(() =>
        {
            if (DirectoryPathOld.DirectoryExists())
            {
                DirectoryPath.DeleteDirectory();
                DirectoryPathOld.MoveDirectory(DirectoryPath, true, form);
            }

            DirectoryPath.CreateDirectory();
            if (!AppInfoVersionPath.FileExists() ||
                !Version.TryParse(AppInfoVersionPath.ReadFile(), out Version version) ||
                version < MinimumAppInfoVersion)
            {
                AppInfoPath.DeleteDirectory();
                AppInfoPath.CreateDirectory();
                AppInfoVersionPath.WriteFile(Program.Version);
            }

            CooldownPath.CreateDirectory();
            if (OldProgramChoicesPath.FileExists())
                OldProgramChoicesPath.DeleteFile();
        });

    internal static bool CheckCooldown(string identifier, int cooldown)
    {
        DateTime now = DateTime.UtcNow;
        DateTime lastCheck = GetCooldown(identifier) ?? now;
        bool cooldownOver = (now - lastCheck).TotalSeconds > cooldown;
        if (cooldownOver || now == lastCheck)
            SetCooldown(identifier, now);
        return cooldownOver;
    }

    private static DateTime? GetCooldown(string identifier)
    {
        if (!CooldownPath.DirectoryExists())
            return null;
        string cooldownFile = CooldownPath + @$"\{identifier}.txt";
        if (!cooldownFile.FileExists())
            return null;
        try
        {
            if (DateTime.TryParse(cooldownFile.ReadFile(), out DateTime cooldown))
                return cooldown;
        }
        catch
        {
            // ignored
        }

        return null;
    }

    private static void SetCooldown(string identifier, DateTime time)
    {
        CooldownPath.CreateDirectory();
        string cooldownFile = CooldownPath + @$"\{identifier}.txt";
        try
        {
            cooldownFile.WriteFile(time.ToString(CultureInfo.InvariantCulture));
        }
        catch
        {
            // ignored
        }
    }

    internal static IEnumerable<(Platform platform, string id)> ReadProgramChoices()
    {
        if (ProgramChoicesPath.FileExists())
            try
            {
                if (JsonConvert.DeserializeObject(ProgramChoicesPath.ReadFile(),
                        typeof(List<(Platform platform, string id)>)) is
                    List<(Platform platform, string id)> choices)
                    return choices;
            }
            catch
            {
                // ignored
            }

        return [];
    }

    internal static void WriteProgramChoices(IEnumerable<(Platform platform, string id)> choices)
    {
        try
        {
            if (choices is null || !choices.Any())
                ProgramChoicesPath.DeleteFile();
            else
                ProgramChoicesPath.WriteFile(JsonConvert.SerializeObject(choices));
        }
        catch
        {
            // ignored
        }
    }

    internal static IEnumerable<(Platform platform, string gameId, string dlcId)> ReadDlcChoices()
    {
        if (DlcChoicesPath.FileExists())
            try
            {
                if (JsonConvert.DeserializeObject(DlcChoicesPath.ReadFile(),
                        typeof(IEnumerable<(Platform platform, string gameId, string dlcId)>)) is
                    IEnumerable<(Platform platform, string gameId, string dlcId)> choices)
                    return choices;
            }
            catch
            {
                // ignored
            }

        return [];
    }

    internal static void WriteDlcChoices(List<(Platform platform, string gameId, string dlcId)> choices)
    {
        try
        {
            if (choices is null || choices.Count == 0)
                DlcChoicesPath.DeleteFile();
            else
                DlcChoicesPath.WriteFile(JsonConvert.SerializeObject(choices));
        }
        catch
        {
            // ignored
        }
    }

    internal static IEnumerable<(Platform platform, string id, string proxy, bool enabled)> ReadProxyChoices()
    {
        if (KoaloaderProxyChoicesPath.FileExists())
            try
            {
                if (JsonConvert.DeserializeObject(KoaloaderProxyChoicesPath.ReadFile(),
                        typeof(IEnumerable<(Platform platform, string id, string proxy, bool enabled)>)) is
                    IEnumerable<(Platform platform, string id, string proxy, bool enabled)> choices)
                    return choices;
            }
            catch
            {
                // ignored
            }

        return [];
    }

    internal static void WriteProxyChoices(
        IEnumerable<(Platform platform, string id, string proxy, bool enabled)> choices)
    {
        try
        {
            if (choices is null || !choices.Any())
                KoaloaderProxyChoicesPath.DeleteFile();
            else
                KoaloaderProxyChoicesPath.WriteFile(JsonConvert.SerializeObject(choices));
        }
        catch
        {
            // ignored
        }
    }

    internal static IEnumerable<(Platform platform, string id)> ReadExtraProtectionChoices()
    {
        if (ExtraProtectionChoicesPath.FileExists())
            try
            {
                if (JsonConvert.DeserializeObject(ExtraProtectionChoicesPath.ReadFile(),
                        typeof(IEnumerable<(Platform platform, string id)>)) is
                    IEnumerable<(Platform platform, string id)> choices)
                    return choices;
            }
            catch
            {
                // ignored
            }

        return [];
    }

    internal static void WriteExtraProtectionChoices(IEnumerable<(Platform platform, string id)> choices)
    {
        try
        {
            if (choices is null || !choices.Any())
                ExtraProtectionChoicesPath.DeleteFile();
            else
                ExtraProtectionChoicesPath.WriteFile(JsonConvert.SerializeObject(choices));
        }
        catch
        {
            // ignored
        }
    }

    internal static List<InstalledGameRecord> ReadInstalledGames()
    {
        if (InstalledGamesPath.FileExists())
            try
            {
                if (JsonConvert.DeserializeObject<List<InstalledGameRecord>>(InstalledGamesPath.ReadFile()) is
                    { } records)
                    return records;
            }
            catch
            {
                // ignored
            }

        return [];
    }

    internal static void WriteInstalledGames(IEnumerable<InstalledGameRecord> records)
    {
        try
        {
            List<InstalledGameRecord> list = records?.ToList() ?? [];
            if (list.Count == 0)
                InstalledGamesPath.DeleteFile();
            else
                InstalledGamesPath.WriteFile(JsonConvert.SerializeObject(list, Formatting.Indented));
        }
        catch
        {
            // ignored
        }
    }

    internal static void UpsertInstalledGame(InstalledGameRecord record)
    {
        List<InstalledGameRecord> records = ReadInstalledGames();
        _ = records.RemoveAll(r => r.Platform == record.Platform && r.Id == record.Id);
        records.Add(record);
        WriteInstalledGames(records);
    }

    internal static void RemoveInstalledGame(Platform platform, string id)
    {
        List<InstalledGameRecord> records = ReadInstalledGames();
        if (records.RemoveAll(r => r.Platform == platform && r.Id == id) > 0)
            WriteInstalledGames(records);
    }
}