using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using CreamInstaller.Forms;
using CreamInstaller.Platforms.Epic;
using CreamInstaller.Platforms.Steam;
using CreamInstaller.Platforms.Ubisoft;
using CreamInstaller.Resources;
using CreamInstaller.Utility;
using static CreamInstaller.Resources.Resources;
namespace CreamInstaller;

public enum Platform
{
    None = 0,
    Paradox,
    Steam,
    Epic,
    Ubisoft
}

internal sealed class Selection : IEquatable<Selection>
{
    internal const string DefaultProxy = "winmm";

    internal static readonly ConcurrentDictionary<Selection, byte> All = new();

    internal readonly HashSet<string> DllDirectories;
    internal readonly List<(string directory, BinaryType binaryType)> ExecutableDirectories;
    internal readonly HashSet<Selection> ExtraSelections = [];
    internal readonly string Id;
    internal readonly string Name;
    internal readonly Platform Platform;
    internal readonly string RootDirectory;
    internal readonly TreeNode TreeNode;
    internal string Icon;
    internal bool UseExtraProtection;
    internal bool UseProxy;
    internal string Proxy;
    internal string Product;
    internal string Publisher;
    internal string SubIcon;
    internal string Website;
    internal InstalledUnlocker InstalledUnlocker;
    internal bool SteamApiDllMissing;

    internal IEnumerable<string> GetAvailableProxies()
    {
        if (!Program.UseSmokeAPI && Platform is Platform.Steam or Platform.Paradox)
            return CreamAPI.ProxyDLLs;
        if (Program.UseSmokeAPI && Platform is Platform.Steam or Platform.Paradox)
            return SmokeAPI.ProxyDLLs;
        return EmbeddedResources.Where(r => r.StartsWith("Koaloader", StringComparison.Ordinal)).Select(p =>
        {
            p.GetProxyInfoFromIdentifier(out string proxyName, out _);
            return proxyName;
        }).ToHashSet();
    }

    private Selection(Platform platform, string id, string name, string rootDirectory, HashSet<string> dllDirectories,
        List<(string directory, BinaryType binaryType)> executableDirectories)
    {
        Platform = platform;
        Id = id;
        Name = name;
        RootDirectory = rootDirectory;
        DllDirectories = dllDirectories;
        ExecutableDirectories = executableDirectories;
        _ = All.TryAdd(this, default);
        TreeNode = new() { Tag = Platform, Name = Id, Text = Name };
        SelectForm selectForm = SelectForm.Current;
        if (selectForm is null)
            return;
        Enabled = selectForm.allCheckBox.Checked;
        UseProxy = selectForm.proxyAllCheckBox.Checked;
    }

    internal static IEnumerable<Selection> AllEnabled => All.Keys.Where(s => s.Enabled);

    internal bool Enabled
    {
        get => TreeNode.Checked;
        set => TreeNode.Checked = value;
    }

    internal IEnumerable<SelectionDLC> DLC => SelectionDLC.All.Keys.Where(dlc => Equals(dlc.Selection, this));

    public bool Equals(Selection other) => other is not null &&
                                           (ReferenceEquals(this, other) ||
                                            Id == other.Id && Platform == other.Platform);

    internal static Selection GetOrCreate(Platform platform, string id, string name, string rootDirectory,
        HashSet<string> dllDirectories,
        List<(string directory, BinaryType binaryType)> executableDirectories)
        => FromId(platform, id) ??
           new Selection(platform, id, name, rootDirectory, dllDirectories, executableDirectories);

    internal void Remove()
    {
        _ = All.TryRemove(this, out _);
        TreeNode.Remove();
        foreach (SelectionDLC dlc in DLC)
            dlc.Selection = null;
    }

    private void Validate(List<(Platform platform, string id, string name)> programsToScan)
    {
        if (programsToScan is null || !programsToScan.Any(p => p.platform == Platform && p.id == Id))
        {
            Remove();
            return;
        }

        if (Program.IsGameBlocked(Name, RootDirectory))
        {
            Remove();
            return;
        }

        if (!RootDirectory.DirectoryExists())
        {
            Remove();
            return;
        }

        if (!SteamApiDllMissing)
        {
            _ = DllDirectories.RemoveWhere(directory => !directory.DirectoryExists());
            if (DllDirectories.Count < 1)
                Remove();
        }
    }

    internal static void ValidateAll(List<(Platform platform, string id, string name)> programsToScan)
    {
        foreach (Selection selection in All.Keys.ToHashSet())
            selection.Validate(programsToScan);
    }

    internal static Selection FromId(Platform platform, string gameId) =>
        All.Keys.FirstOrDefault(s => s.Platform == platform && s.Id == gameId);

    internal InstalledUnlocker DetectInstalledUnlocker()
    {
        foreach (string directory in DllDirectories)
        {
            if (Platform is Platform.Steam or Platform.Paradox)
            {
                // Use uniquely-named config files to distinguish CreamAPI from SmokeAPI.
                // Both share steam_api_o.dll so the _o files alone are ambiguous.
                directory.GetSmokeApiComponents(out _, out _, out _, out _, out string smokeOldConfig,
                    out string smokeConfig, out _, out _, out _);
                if (smokeConfig.FileExists() || smokeOldConfig.FileExists())
                    return InstalledUnlocker.SmokeAPI;

                directory.GetCreamApiComponents(out _, out _, out _, out _, out string creamConfig);
                if (creamConfig.FileExists())
                {
                    ReadCreamApiConfig(creamConfig);
                    return InstalledUnlocker.CreamAPI;
                }

                // Fallback: config was deleted but _o files remain identify by replacement DLL content
                directory.GetSmokeApiComponents(out string smokeApi32, out string api32_o,
                    out string smokeApi64, out string api64_o, out _, out _, out _, out _, out _);
                if (api32_o.FileExists() || api64_o.FileExists())
                {
                    if ((smokeApi32.FileExists() && smokeApi32.IsResourceFile(ResourceIdentifier.Steamworks32))
                        || (smokeApi64.FileExists() && smokeApi64.IsResourceFile(ResourceIdentifier.Steamworks64)))
                        return InstalledUnlocker.SmokeAPI;
                    return InstalledUnlocker.CreamAPI;
                }
            }

            if (Platform is Platform.Epic or Platform.Paradox)
            {
                directory.GetScreamApiComponents(out _, out string api32_o, out _, out string api64_o,
                    out _, out string config, out _, out _);
                if (config.FileExists() || api32_o.FileExists() || api64_o.FileExists())
                    return InstalledUnlocker.ScreamAPI;
            }

            if (Platform is Platform.Ubisoft)
            {
                directory.GetUplayR1Components(out _, out string api32_o, out _, out string api64_o,
                    out string config, out _);
                if (config.FileExists() || api32_o.FileExists() || api64_o.FileExists())
                    return InstalledUnlocker.UplayR1;
                directory.GetUplayR2Components(out _, out _, out _, out api32_o, out _, out api64_o,
                    out config, out _);
                if (config.FileExists() || api32_o.FileExists() || api64_o.FileExists())
                    return InstalledUnlocker.UplayR2;
            }
        }

        foreach ((string directory, _) in ExecutableDirectories)
        {
            directory.GetKoaloaderComponents(out _, out string config, out _);
            if (directory.GetKoaloaderProxies().Any(proxy =>
                    proxy.FileExists() && proxy.IsResourceFile(ResourceIdentifier.Koaloader))
                || config.FileExists())
                return InstalledUnlocker.Koaloader;

            if (Platform is Platform.Steam or Platform.Paradox)
            {
                directory.GetSmokeApiComponents(out _, out _, out _, out _, out _, out string smokeConfig, out _, out _, out _);
                if (smokeConfig.FileExists())
                    return InstalledUnlocker.SmokeAPI;
                directory.GetCreamApiComponents(out _, out _, out _, out _, out string creamConfig);
                if (creamConfig.FileExists())
                    return InstalledUnlocker.CreamAPI;
                if (directory.GetSmokeApiProxies().Any(proxy =>
                        proxy.FileExists() && (proxy.IsResourceFile(ResourceIdentifier.Steamworks32) ||
                                               proxy.IsResourceFile(ResourceIdentifier.Steamworks64))))
                    return InstalledUnlocker.SmokeAPI;
                if (directory.GetCreamApiProxies().Any(proxy =>
                        proxy.FileExists() && (proxy.IsResourceFile(ResourceIdentifier.Steamworks32) ||
                                               proxy.IsResourceFile(ResourceIdentifier.Steamworks64))))
                    return InstalledUnlocker.CreamAPI;
            }
        }

        return InstalledUnlocker.None;
    }

    internal string DetectInstalledProxy()
    {
        HashSet<string> knownProxies = ["winmm", "winhttp", "version"];
        foreach (string directory in DllDirectories)
            foreach (string proxy in knownProxies)
            {
                string proxyPath = directory + @"\" + proxy + ".dll";
                if (proxyPath.FileExists())
                    return proxy;
            }
        return null;
    }

    private void ReadCreamApiConfig(string configPath)
    {
        try
        {
            if (!configPath.FileExists())
                return;

            string[] lines = File.ReadAllLines(configPath);
            foreach (string line in lines)
            {
                string trimmed = line.Trim();
                if (trimmed.StartsWith("extraprotection", StringComparison.OrdinalIgnoreCase))
                {
                    string[] parts = trimmed.Split('=');
                    if (parts.Length == 2)
                    {
                        string value = parts[1].Trim();
                        UseExtraProtection = value.Equals("true", StringComparison.OrdinalIgnoreCase);
                    }
                    break;
                }
            }
        }
        catch
        {
            // If we can't read the config, leave UseExtraProtection at its default value
        }
    }

    public override bool Equals(object obj) => ReferenceEquals(this, obj) || obj is Selection other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(Id, (int)Platform);
}