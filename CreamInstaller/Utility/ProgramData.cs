using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Windows.Forms;
using CreamInstaller;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CreamInstaller.Utility;

internal enum LogDestination { App, Scan, Steam, Unlocker }

internal enum LogLevel { Info, Warning, Error }

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
    public bool Enabled { get; set; }
}

internal sealed class InstalledGameRecord
{
    [JsonConverter(typeof(StringEnumConverter))]
    public Platform Platform { get; set; }
    public string Id { get; set; }
    public string Name { get; set; }
    public string RootDirectory { get; set; }
    [JsonConverter(typeof(StringEnumConverter))]
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

    private static readonly string CachePath = DirectoryPath + @"\Cache";

    internal static readonly string AppInfoPath = CachePath + @"\appinfo";
    private static readonly string AppInfoVersionPath = AppInfoPath + @"\version.txt";

    private static readonly Version MinimumAppInfoVersion = Version.Parse("4.7.0.0");

    internal static readonly string CooldownPath = CachePath + @"\cooldown";

    private static readonly string OldProgramChoicesPath = DirectoryPath + @"\choices.txt";
    private static readonly string OldProgramChoicesJsonPath = DirectoryPath + @"\choices.json";
    private static readonly string OldKoaloaderProxyChoicesPath = DirectoryPath + @"\proxies.json";
    private static readonly string OldExtraProtectionChoicesPath = DirectoryPath + @"\extraprotection.json";
    private static readonly string ProgramChoicesPath = CachePath + @"\saved-game-choices.json";
    private static readonly string KoaloaderProxyChoicesPath = CachePath + @"\proxies.json";
    private static readonly string ExtraProtectionChoicesPath = CachePath + @"\extraprotection.json";
    private static readonly string InstalledGamesPath = CachePath + @"\installed.json";
    private static readonly string SettingsPath = CachePath + @"\settings.json";

    private static readonly string LogsPath = DirectoryPath + @"\Logs";
    internal static readonly string ScanLogPath = LogsPath + @"\game-scan.log";
    internal static readonly string SteamLogPath = LogsPath + @"\cream-steam.log";
    internal static readonly string AppLogPath = LogsPath + @"\CreamInstaller.log";
    internal static readonly string UnlockerLogPath = LogsPath + @"\unlocker.log";

internal readonly record struct LogEventArgs(string Message, LogDestination Destination, LogLevel Level, Exception Exception = null);

internal static event Action<LogEventArgs> OnLog;

    private static string FormatLogEntry(string message)
    {
        string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture);
        return $"[{timestamp}] {message}{Environment.NewLine}";
    }

    private static string FormatLogErrorEntry(string message, Exception ex)
    {
        string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture);
        return $"[{timestamp}] [ERROR] {message}{Environment.NewLine}[{timestamp}] [ERROR]   Exception: {ex}{Environment.NewLine}";
    }

    private readonly record struct LogEntry(string Path, string Text);

    private static readonly Channel<LogEntry> LogChannel = Channel.CreateUnbounded<LogEntry>();
    private static readonly Task LogConsumer;

    static ProgramData()
    {
        LogConsumer = ConsumeLogAsync();
    }

    private static async Task ConsumeLogAsync()
    {
        ChannelReader<LogEntry> reader = LogChannel.Reader;
        List<LogEntry> batch = new(64);
        while (await reader.WaitToReadAsync())
        {
            batch.Clear();
            while (reader.TryRead(out LogEntry entry))
            {
                batch.Add(entry);
                if (batch.Count >= 64)
                    break;
            }
            if (batch.Count > 0)
                FlushLogBatch(batch);
        }
    }

    private static void FlushLogBatch(List<LogEntry> entries)
    {
        try
        {
            foreach (IGrouping<string, LogEntry> group in entries.GroupBy(e => e.Path))
            {
                StringBuilder combined = new(group.Sum(e => e.Text.Length));
                foreach (LogEntry entry in group)
                    _ = combined.Append(entry.Text);
                File.AppendAllText(group.Key, combined.ToString(), Encoding.UTF8);
            }
        }
        catch
        {
            // ignored; logging must never crash the application
        }
    }

    private static string GetLogPath(LogDestination destination) => destination switch
    {
        LogDestination.Scan => ScanLogPath,
        LogDestination.Steam => SteamLogPath,
        LogDestination.Unlocker => UnlockerLogPath,
        _ => AppLogPath
    };

    internal static class Log
    {
        public static void Info(string message, LogDestination destination = LogDestination.App)
        {
            try
            {
                LogChannel.Writer.TryWrite(new LogEntry(GetLogPath(destination), FormatLogEntry(message)));
            }
            catch
            {
                // ignored; logging must never crash the application
            }
            OnLog?.Invoke(new LogEventArgs(message, destination, LogLevel.Info));
        }

        public static void Warn(string message)
        {
            try
            {
                LogChannel.Writer.TryWrite(new LogEntry(AppLogPath, FormatLogEntry($"[WARN] {message}")));
            }
            catch
            {
                // ignored; logging must never crash the application
            }
            OnLog?.Invoke(new LogEventArgs(message, LogDestination.App, LogLevel.Warning));
        }

        public static void Error(string message, Exception ex = null)
        {
            try
            {
                string entry = ex is not null
                    ? FormatLogErrorEntry(message, ex)
                    : FormatLogEntry($"[ERROR] {message}");
                LogChannel.Writer.TryWrite(new LogEntry(AppLogPath, entry));
            }
            catch
            {
                // ignored; logging must never crash the application
            }
            OnLog?.Invoke(new LogEventArgs(message, LogDestination.App, LogLevel.Error, ex));
        }
    }

    internal static void ClearLog()
    {
        try
        {
            if (File.Exists(ScanLogPath))
                File.Delete(ScanLogPath);
            if (File.Exists(SteamLogPath))
                File.Delete(SteamLogPath);
            if (File.Exists(UnlockerLogPath))
                File.Delete(UnlockerLogPath);
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
            CachePath.CreateDirectory();
            if (OldProgramChoicesPath.FileExists())
                OldProgramChoicesPath.DeleteFile();
            MigrateOldPath(OldProgramChoicesJsonPath, ProgramChoicesPath);
            MigrateOldPath(OldKoaloaderProxyChoicesPath, KoaloaderProxyChoicesPath);
            MigrateOldPath(OldExtraProtectionChoicesPath, ExtraProtectionChoicesPath);
            LogsPath.CreateDirectory();
            MigrateOldPath(DirectoryPath + @"\game-scan.log", ScanLogPath);
            MigrateOldPath(DirectoryPath + @"\cream-steam.log", SteamLogPath);
            MigrateOldPath(DirectoryPath + @"\CreamInstaller.log", AppLogPath);
            MigrateOldPath(DirectoryPath + @"\unlocker.log", UnlockerLogPath);

            // cleanup legacy paths no longer used
            string oldCooldown = DirectoryPath + @"\cooldown";
            try { if (Directory.Exists(oldCooldown)) Directory.Delete(oldCooldown, true); } catch { }
        });

    private static void MigrateOldPath(string oldPath, string newPath)
    {
        if (!oldPath.FileExists())
            return;
        if (newPath.FileExists())
        {
            oldPath.DeleteFile();
            return;
        }
        try
        {
            string content = oldPath.ReadFile();
            newPath.WriteFile(content);
            oldPath.DeleteFile();
            Log.Info($"Migrated {Path.GetFileName(oldPath)} -> {Path.GetFileName(newPath)}");
        }
        catch
        {
            // ignored; migration failure must not crash the application
        }
    }

    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, DateTime> CooldownCache = new();

    internal static bool CheckCooldown(string identifier, int cooldown)
    {
        DateTime now = DateTime.UtcNow;

        if (CooldownCache.TryGetValue(identifier, out DateTime cached) &&
            (now - cached).TotalSeconds <= cooldown)
            return false;

        DateTime? lastCheck = GetCooldown(identifier);
        DateTime effective = lastCheck ?? now;
        bool cooldownOver = (now - effective).TotalSeconds > cooldown;

        CooldownCache[identifier] = now;

        if (cooldownOver || now == effective)
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

    internal static SettingsModel LoadSettings()
    {
        try
        {
            if (JsonConvert.DeserializeObject<SettingsModel>(SettingsPath.ReadFile()) is SettingsModel settings)
                return settings;
        }
        catch
        {
            // ignored
        }

        return new SettingsModel();
    }

    internal static void SaveSettings(SettingsModel settings)
    {
        try
        {
            SettingsPath.WriteFile(JsonConvert.SerializeObject(settings, Formatting.Indented));
        }
        catch
        {
            // ignored
        }
    }
}