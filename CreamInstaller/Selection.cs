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
using ProxyKindType = CreamInstaller.Utility.ProxyKind;
namespace CreamInstaller;

public enum Platform
{
    None = 0,
    Paradox,
    Steam,
    Epic,
    Ubisoft
}

/// <summary>A selectable proxy DLL together with the mechanism that injects the unlocker.</summary>
internal readonly record struct ProxyOption(string Name, ProxyKind Kind)
{
    internal string FileName => Name + ".dll";

    internal string DisplayName => $"{FileName} ({(Kind == ProxyKind.Native ? "Native" : "Koaloader")})";
}

internal sealed class Selection : IEquatable<Selection>
{
    internal const string DefaultProxy = "winmm";

    internal static readonly ConcurrentDictionary<Selection, byte> All = new();

    internal readonly ConcurrentDictionary<string, SelectionDLC> DLCById = new();
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
    internal ProxyKindType ProxyKind;
    internal bool ProxyExplicitlySet;
    internal string Product;
    internal string Publisher;
    internal string SubIcon;
    internal string Website;
    internal InstalledUnlocker InstalledUnlocker;
    internal string ConfigDefaultAppStatus; // "unlocked", "locked", or "original" from existing SmokeAPI config (null if no config)
    internal bool SteamApiDllMissing;

    private ProxyOption? defaultProxyOption;
    private string primaryExecutablePath;
    private bool primaryExecutableResolved;
    private BinaryType primaryExecutableBinaryType = BinaryType.Unknown;
    private bool primaryExecutableBinaryTypeResolved;

    /// <summary>Native proxies only exist where a Steamworks unlocker is actually deployed.</summary>
    private bool NativeProxiesAvailable => Platform is Platform.Steam ||
                                          Platform is Platform.Paradox &&
                                          ExtraSelections.Any(s => s.Platform is Platform.Steam);

    /// <summary>
    /// Koaloader cannot inject CreamAPI, so Steam/Paradox selections only offer Koaloader proxies when
    /// SmokeAPI is the active Steam unlocker. Epic and Ubisoft always require Koaloader.
    /// </summary>
    internal bool KoaloaderProxiesAvailable => Platform switch
    {
        Platform.Epic or Platform.Ubisoft => true,
        Platform.Steam => Program.UseSmokeAPI,
        Platform.Paradox => !NativeProxiesAvailable || Program.UseSmokeAPI,
        _ => false
    };

    /// <summary>All proxy choices for this selection, native options first.</summary>
    internal IEnumerable<ProxyOption> GetAvailableProxies()
    {
        List<ProxyOption> options = [];
        if (NativeProxiesAvailable)
        {
            IEnumerable<string> nativeProxies = Program.UseSmokeAPI ? SmokeAPI.ProxyDLLs : CreamAPI.ProxyDLLs;
            options.AddRange(nativeProxies.Select(name => new ProxyOption(name, ProxyKindType.Native)));
        }

        if (KoaloaderProxiesAvailable)
        {
            (bool has32, bool has64) = GetProxyTargetBinaryTypes();
            options.AddRange(Koaloader.GetProxyNames()
                .Where(name => IsProxyBitnessCompatible(name, has32, has64))
                .Select(name => new ProxyOption(name, ProxyKindType.Koaloader)));
        }

        return options;
    }

    /// <summary>
    /// Resolves the proxy to install. Malformed or currently unavailable saved names fall back to the
    /// default proxy; an architecture-restricted proxy that cannot serve every intended executable
    /// directory is reported as an error so nothing is removed or overwritten first.
    /// </summary>
    internal ProxyOption ResolveInstallProxyOption(out string error)
    {
        error = null;
        bool persisted = Proxy is not null;
        if (persisted && !IsKnownProxyName(Proxy))
        {
            ProgramData.Log.Warn($"[Selection] Ignoring unknown saved proxy '{Proxy}' | Game: {Name} ({Id})");
            Proxy = null;
            defaultProxyOption = null;
            persisted = false;
        }

        ProxyOption option = GetSelectedProxyOption(true);
        if (!IsProxyOptionAvailable(option))
        {
            ProgramData.Log.Warn(
                $"[Selection] Saved proxy '{option.Name}' ({option.Kind}) is unavailable; using the default | Game: {Name} ({Id})");
            Proxy = null;
            defaultProxyOption = null;
            persisted = false;
            option = GetSelectedProxyOption(true);
        }

        if (option.Kind == ProxyKindType.Koaloader)
        {
            BinaryType proxyBinaryType = Koaloader.GetProxyBinaryType(option.Name);
            (bool has32, bool has64) = GetProxyTargetBinaryTypes();
            bool incompatible = proxyBinaryType is BinaryType.BIT32 && has64 ||
                                proxyBinaryType is BinaryType.BIT64 && has32;
            if (incompatible)
            {
                if (persisted)
                    error = $"{option.Name}.dll is available only for " +
                            $"{(proxyBinaryType == BinaryType.BIT32 ? "32-bit" : "64-bit")} executables and " +
                            $"cannot be used because at least one executable directory for {Name} contains a " +
                            $"{(has64 ? "64-bit" : "32-bit")} executable.";
                else
                {
                    Proxy = null;
                    defaultProxyOption = new ProxyOption(DefaultProxy,
                        NativeProxiesAvailable ? ProxyKindType.Native : ProxyKindType.Koaloader);
                    option = GetSelectedProxyOption(true);
                }
            }
        }

        return option;
    }

    private static bool IsKnownProxyName(string name)
        => !string.IsNullOrWhiteSpace(name) &&
           (CreamAPI.ProxyDLLs.Contains(name, StringComparer.OrdinalIgnoreCase) ||
            SmokeAPI.ProxyDLLs.Contains(name, StringComparer.OrdinalIgnoreCase) ||
            Koaloader.GetProxyNames().Contains(name, StringComparer.OrdinalIgnoreCase));

    private bool IsProxyOptionAvailable(ProxyOption option)
    {
        if (option.Kind is not (ProxyKindType.Native or ProxyKindType.Koaloader))
            return false;
        if (option.Kind == ProxyKindType.Native)
        {
            if (!NativeProxiesAvailable)
                return false;
            IEnumerable<string> nativeProxies = Program.UseSmokeAPI ? SmokeAPI.ProxyDLLs : CreamAPI.ProxyDLLs;
            return nativeProxies.Contains(option.Name, StringComparer.OrdinalIgnoreCase);
        }

        return KoaloaderProxiesAvailable &&
               Koaloader.GetProxyNames().Contains(option.Name, StringComparer.OrdinalIgnoreCase);
    }

    private static bool IsProxyBitnessCompatible(string proxyName, bool has32, bool has64)
    {
        BinaryType binaryType = Koaloader.GetProxyBinaryType(proxyName);
        return binaryType switch
        {
            BinaryType.BIT32 => has32 && !has64,
            BinaryType.BIT64 => has64 && !has32,
            _ => true
        };
    }

    /// <summary>
    /// The executable directories that should receive a proxy DLL, deduplicated by path. The bitness
    /// is taken from the executables actually present in each directory; a directory containing both
    /// 32-bit and 64-bit game executables is marked Unknown so installation can fail before writing.
    /// </summary>
    internal IReadOnlyList<(string directory, BinaryType binaryType)> GetProxyDirectories()
    {
        List<(string directory, BinaryType binaryType)> directories = [];
        Dictionary<string, int> indices = new(StringComparer.OrdinalIgnoreCase);
        foreach ((string directory, BinaryType discoveredBinaryType) in ExecutableDirectories)
        {
            if (string.IsNullOrWhiteSpace(directory) || !directory.DirectoryExists())
                continue;
            BinaryType binaryType = ResolveDirectoryBinaryType(directory, discoveredBinaryType);
            if (indices.TryGetValue(directory, out int index))
            {
                BinaryType existing = directories[index].binaryType;
                if (existing != binaryType &&
                    existing is BinaryType.BIT32 or BinaryType.BIT64 &&
                    binaryType is BinaryType.BIT32 or BinaryType.BIT64)
                    directories[index] = (directory, BinaryType.Unknown);
                continue;
            }

            indices[directory] = directories.Count;
            directories.Add((directory, binaryType));
        }

        if (directories.Count == 0)
        {
            string primaryDirectory = Path.GetDirectoryName(PrimaryExecutablePath);
            if (!string.IsNullOrEmpty(primaryDirectory) && primaryDirectory.DirectoryExists())
            {
                BinaryType primaryBinaryType = ResolveDirectoryBinaryType(primaryDirectory,
                    PrimaryExecutableBinaryType);
                if (primaryBinaryType is BinaryType.BIT32 or BinaryType.BIT64 or BinaryType.Unknown)
                    directories.Add((primaryDirectory, primaryBinaryType));
            }
        }

        return directories;
    }

    private BinaryType ResolveDirectoryBinaryType(string directory, BinaryType discoveredBinaryType)
    {
        (bool has32, bool has64) = GetDirectoryGameBinaryTypes(directory, RootDirectory);
        if (has32 && has64)
            return BinaryType.Unknown;
        if (has32)
            return BinaryType.BIT32;
        if (has64)
            return BinaryType.BIT64;
        return discoveredBinaryType;
    }

    /// <summary>
    /// Actual game executable architectures directly inside a directory. Installers, uninstallers,
    /// and executables recognized as common incorrect helpers are ignored so their architecture cannot
    /// make a game directory look mixed or steer the proxy to the wrong bitness.
    /// </summary>
    internal static (bool has32, bool has64) GetDirectoryGameBinaryTypes(string directory, string rootDirectory)
    {
        bool has32 = false, has64 = false;
        foreach (string executable in directory.EnumerateDirectory("*.exe"))
        {
            if (Program.Canceled)
                break;
            if (IsIgnoredProxyExecutable(executable, rootDirectory))
                continue;
            if (!executable.TryGetFileBinaryType(out BinaryType binaryType))
                continue;
            has32 |= binaryType is BinaryType.BIT32;
            has64 |= binaryType is BinaryType.BIT64;
            if (has32 && has64)
                break;
        }

        return (has32, has64);
    }

    private static bool IsIgnoredProxyExecutable(string executable, string rootDirectory)
    {
        string fileName = Path.GetFileNameWithoutExtension(executable);
        if (fileName.StartsWith("unins", StringComparison.OrdinalIgnoreCase) ||
            fileName.Contains("uninstall", StringComparison.OrdinalIgnoreCase) ||
            fileName.Contains("installer", StringComparison.OrdinalIgnoreCase))
            return true;
        return !string.IsNullOrWhiteSpace(rootDirectory) &&
               executable.StartsWith(rootDirectory, StringComparison.OrdinalIgnoreCase) &&
               rootDirectory.IsCommonIncorrectExecutable(executable);
    }

    /// <summary>Every architecture present in the intended executable directories, used to filter proxies.</summary>
    internal (bool has32, bool has64) GetProxyTargetBinaryTypes()
    {
        bool has32 = false, has64 = false;
        foreach ((string _, BinaryType binaryType) in GetProxyDirectories())
        {
            // An Unknown entry means the same directory contains both architectures, so treat both as
            // present and only offer proxies that exist for both.
            has32 |= binaryType is BinaryType.BIT32 or BinaryType.Unknown;
            has64 |= binaryType is BinaryType.BIT64 or BinaryType.Unknown;
        }

        if (!has32 && !has64)
        {
            BinaryType primaryBinaryType = PrimaryExecutableBinaryType;
            has32 = primaryBinaryType is BinaryType.BIT32;
            has64 = primaryBinaryType is BinaryType.BIT64;
        }

        return (has32, has64);
    }

    /// <summary>The proxy that would be used, preferring one the primary executable actually imports.</summary>
    internal ProxyOption GetSelectedProxyOption(bool materialize = false)
    {
        ProxyOption option = Proxy is not null ? new ProxyOption(Proxy, ProxyKind) : GetDefaultProxyOption();
        // Without a Steamworks unlocker there are no native forwarded exports; Koaloader is required.
        if (!NativeProxiesAvailable)
            option = option with { Kind = ProxyKindType.Koaloader };
        else if (!KoaloaderProxiesAvailable)
        {
            // CreamAPI has no Koaloader support; fall back to one of its native proxy DLLs.
            IEnumerable<string> nativeProxies = Program.UseSmokeAPI ? SmokeAPI.ProxyDLLs : CreamAPI.ProxyDLLs;
            string name = nativeProxies.Contains(option.Name, StringComparer.OrdinalIgnoreCase)
                ? option.Name
                : DefaultProxy;
            option = new ProxyOption(name, ProxyKindType.Native);
        }

        if (materialize)
        {
            Proxy = option.Name == DefaultProxy && option.Kind == ProxyKindType.Native ? null : option.Name;
            ProxyKind = option.Kind;
            ProxyExplicitlySet = true;
        }

        return option;
    }

    private ProxyOption GetDefaultProxyOption()
    {
        if (defaultProxyOption is { } cached)
            return cached;

        bool supportsNative = NativeProxiesAvailable;
        defaultProxyOption = new ProxyOption(DefaultProxy,
            supportsNative ? ProxyKindType.Native : ProxyKindType.Koaloader);
        if (!supportsNative)
            return defaultProxyOption.Value;

        string executable = PrimaryExecutablePath;
        if (executable is null || !PeImports.TryGetImportedModules(executable, out HashSet<string> imports) ||
            imports.Count == 0)
            return defaultProxyOption.Value;

        foreach (string native in CreamAPI.ProxyDLLs)
            if (imports.Contains(native + ".dll"))
            {
                defaultProxyOption = new ProxyOption(native, ProxyKindType.Native);
                return defaultProxyOption.Value;
            }

        (bool has32, bool has64) = GetProxyTargetBinaryTypes();
        foreach (string koaloader in Koaloader.GetProxyNames(PrimaryExecutableBinaryType))
            if (KoaloaderProxiesAvailable && IsProxyBitnessCompatible(koaloader, has32, has64) &&
                imports.Contains(koaloader + ".dll"))
            {
                defaultProxyOption = new ProxyOption(koaloader, ProxyKindType.Koaloader);
                return defaultProxyOption.Value;
            }

        return defaultProxyOption.Value;
    }

    /// <summary>The executable that should host the proxy DLL, resolved lazily.</summary>
    internal string PrimaryExecutablePath
    {
        get
        {
            if (primaryExecutableResolved)
                return primaryExecutablePath;
            primaryExecutableResolved = true;
            primaryExecutablePath = ResolvePrimaryExecutable();
            return primaryExecutablePath;
        }
    }

    /// <summary>Clears the cached import-based default so a Steam unlocker toggle is reflected immediately.</summary>
    internal void ResetProxyDefaultCache() => defaultProxyOption = null;

    /// <summary>The bitness of the primary executable; Unknown when it cannot be determined.</summary>
    internal BinaryType PrimaryExecutableBinaryType
    {
        get
        {
            if (primaryExecutableBinaryTypeResolved)
                return primaryExecutableBinaryType;
            primaryExecutableBinaryTypeResolved = true;
            string executable = PrimaryExecutablePath;
            if (executable is not null && executable.TryGetFileBinaryType(out BinaryType binaryType))
                primaryExecutableBinaryType = binaryType;
            return primaryExecutableBinaryType;
        }
    }

    private string ResolvePrimaryExecutable()
    {
        if (string.IsNullOrWhiteSpace(RootDirectory) || !RootDirectory.DirectoryExists())
            return null;

        try
        {
            List<string> executables = [];
            foreach (string path in RootDirectory.EnumerateDirectory("*.exe", true))
            {
                if (Program.Canceled)
                    return null;
                if (RootDirectory.IsCommonIncorrectExecutable(path) || !path.TryGetFileBinaryType(out _))
                    continue;
                executables.Add(path);
            }

            if (executables.Count == 0)
                return null;

            string rootExecutable = executables.FirstOrDefault(path =>
                string.Equals(Path.GetDirectoryName(path), RootDirectory, StringComparison.OrdinalIgnoreCase));
            if (rootExecutable is not null)
                return rootExecutable;

            return executables
                .OrderBy(path => path.Count(c => c is '\\' or '/'))
                .ThenByDescending(path =>
                {
                    try
                    {
                        return new FileInfo(path).Length;
                    }
                    catch
                    {
                        return 0L;
                    }
                })
                .First();
        }
        catch (Exception e)
        {
            ProgramData.Log.Error($"[Selection] Failed to resolve the primary executable | Game: {Name} ({Id})", e);
            return null;
        }
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
        // New selections follow the configured default; saved per-game choices are applied later by
        // MainForm.LoadSelections and MainForm.LoadSavedInstalledGames.
        UseProxy = Program.EnableProxyByDefault;
        MainForm selectForm = MainForm.Current;
        if (selectForm is null)
            return;
        // New root game selections default to selected. Primary/root games that are already installed or cached are
        // deselected afterwards (see MainForm.DeselectInstalledSelections) so they are not treated as new selections.
        // This deliberately does not read selectForm.allCheckBox: that reflects whether every node is currently
        // checked, which becomes false once installed games are deselected and must not make future additions unchecked.
        Enabled = true;
    }

    /// <summary>
    /// Applies the global proxying default unless this game already has an installed/unlocker configuration,
    /// an explicit proxy choice, or requires proxying because its Steam API DLL is missing.
    /// </summary>
    internal void ApplyProxyDefault()
    {
        if (ProxyExplicitlySet || SteamApiDllMissing || InstalledUnlocker != InstalledUnlocker.None)
            return;
        UseProxy = Program.EnableProxyByDefault;
        if (!UseProxy)
        {
            Proxy = null;
            ProxyKind = ProxyKindType.Native;
        }
    }

    internal static IEnumerable<Selection> AllEnabled => All.Keys.Where(s => s.Enabled);

    internal bool Enabled
    {
        get => TreeNode.Checked;
        set => TreeNode.Checked = value;
    }

    internal IEnumerable<SelectionDLC> DLC => DLCById.Values;

    internal InstalledGameRecord ToInstalledGameRecord(InstalledGameRecord existing = null) => new()
    {
        Platform = Platform,
        Id = Id,
        Name = Name,
        RootDirectory = RootDirectory,
        Unlocker = InstalledUnlocker,
        UseProxy = existing?.UseProxy ?? UseProxy,
        ProxyDllName = (existing?.UseProxy ?? UseProxy) ? (existing?.ProxyDllName ?? Proxy ?? DefaultProxy) : null,
        ProxyKind = (existing?.UseProxy ?? UseProxy)
            ? (ProxyExplicitlySet ? ProxyKind : existing?.ProxyKind ?? ProxyKind)
            : ProxyKindType.Native,
        UseExtraProtection = existing?.UseExtraProtection ?? UseExtraProtection,
        Dlc = DLC.Select(dlc => new InstalledDlcRecord
        {
            DlcType = dlc.Type.ToString(),
            Id = dlc.Id,
            Name = dlc.Name,
            Enabled = dlc.Enabled
        }).ToList()
    };

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
        foreach (SelectionDLC dlc in DLCById.Values.ToList())
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
                {
                    ProgramData.Log.Info($"[Unlocker] SmokeAPI detected | Game: {Name} ({Id})", LogDestination.Unlocker);
                    return InstalledUnlocker.SmokeAPI;
                }

                directory.GetCreamApiComponents(out _, out _, out _, out _, out string creamConfig);
                if (creamConfig.FileExists())
                {
                    ProgramData.Log.Info($"[Unlocker] CreamAPI detected | Game: {Name} ({Id})", LogDestination.Unlocker);
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
                    {
                        ProgramData.Log.Info($"[Unlocker] SmokeAPI detected (via _o files) | Game: {Name} ({Id})", LogDestination.Unlocker);
                        return InstalledUnlocker.SmokeAPI;
                    }
                    ProgramData.Log.Info($"[Unlocker] CreamAPI detected (via _o files) | Game: {Name} ({Id})", LogDestination.Unlocker);
                    return InstalledUnlocker.CreamAPI;
                }
            }

            if (Platform is Platform.Epic or Platform.Paradox)
            {
                directory.GetScreamApiComponents(out _, out string api32_o, out _, out string api64_o,
                    out _, out string config, out _, out _);
                if (config.FileExists() || api32_o.FileExists() || api64_o.FileExists())
                {
                    ProgramData.Log.Info($"[Unlocker] ScreamAPI detected | Game: {Name} ({Id})", LogDestination.Unlocker);
                    return InstalledUnlocker.ScreamAPI;
                }
            }

            if (Platform is Platform.Ubisoft)
            {
                directory.GetUplayR1Components(out _, out string api32_o, out _, out string api64_o,
                    out string config, out _);
                if (config.FileExists() || api32_o.FileExists() || api64_o.FileExists())
                {
                    ProgramData.Log.Info($"[Unlocker] UplayR1 detected | Game: {Name} ({Id})", LogDestination.Unlocker);
                    return InstalledUnlocker.UplayR1;
                }
                directory.GetUplayR2Components(out _, out _, out _, out api32_o, out _, out api64_o,
                    out config, out _);
                if (config.FileExists() || api32_o.FileExists() || api64_o.FileExists())
                {
                    ProgramData.Log.Info($"[Unlocker] UplayR2 detected | Game: {Name} ({Id})", LogDestination.Unlocker);
                    return InstalledUnlocker.UplayR2;
                }
            }
        }

        foreach ((string directory, _) in ExecutableDirectories)
        {
            directory.GetKoaloaderComponents(out _, out string config, out _);
            if (directory.GetKoaloaderProxies().Any(proxy =>
                    proxy.FileExists() && proxy.IsResourceFile(ResourceIdentifier.Koaloader))
                || config.FileExists())
            {
                ProgramData.Log.Info($"[Unlocker] Koaloader detected | Game: {Name} ({Id})", LogDestination.Unlocker);
                return InstalledUnlocker.Koaloader;
            }

            if (Platform is Platform.Steam or Platform.Paradox)
            {
                directory.GetSmokeApiComponents(out _, out _, out _, out _, out _, out string smokeConfig, out _, out _, out _);
                if (smokeConfig.FileExists())
                {
                    ProgramData.Log.Info($"[Unlocker] SmokeAPI detected (proxy) | Game: {Name} ({Id})", LogDestination.Unlocker);
                    return InstalledUnlocker.SmokeAPI;
                }
                directory.GetCreamApiComponents(out _, out _, out _, out _, out string creamConfig);
                if (creamConfig.FileExists())
                {
                    ProgramData.Log.Info($"[Unlocker] CreamAPI detected (proxy) | Game: {Name} ({Id})", LogDestination.Unlocker);
                    return InstalledUnlocker.CreamAPI;
                }
                if (directory.GetSmokeApiProxies().Any(proxy =>
                        proxy.FileExists() && (proxy.IsResourceFile(ResourceIdentifier.Steamworks32) ||
                                               proxy.IsResourceFile(ResourceIdentifier.Steamworks64))))
                {
                    ProgramData.Log.Info($"[Unlocker] SmokeAPI proxy DLL detected | Game: {Name} ({Id})", LogDestination.Unlocker);
                    return InstalledUnlocker.SmokeAPI;
                }
                if (directory.GetCreamApiProxies().Any(proxy =>
                        proxy.FileExists() && (proxy.IsResourceFile(ResourceIdentifier.Steamworks32) ||
                                               proxy.IsResourceFile(ResourceIdentifier.Steamworks64))))
                {
                    ProgramData.Log.Info($"[Unlocker] CreamAPI proxy DLL detected | Game: {Name} ({Id})", LogDestination.Unlocker);
                    return InstalledUnlocker.CreamAPI;
                }
            }
        }

        ProgramData.Log.Info($"[Unlocker] No installed unlocker found | Game: {Name} ({Id})", LogDestination.Unlocker);
        return InstalledUnlocker.None;
    }

    internal (string name, ProxyKindType kind)? DetectInstalledProxy()
    {
        foreach (string directory in EnumerateProxyDirectories())
            foreach (string proxy in Koaloader.GetProxyNames())
            {
                string proxyPath = directory + @"\" + proxy + ".dll";
                if (!proxyPath.FileExists())
                    continue;
                if (proxyPath.IsResourceFile(ResourceIdentifier.Koaloader))
                    return (proxy, ProxyKindType.Koaloader);
                if (proxyPath.IsResourceFile(ResourceIdentifier.Steamworks32) ||
                    proxyPath.IsResourceFile(ResourceIdentifier.Steamworks64))
                    return (proxy, ProxyKindType.Native);
            }

        return null;
    }

    private IEnumerable<string> EnumerateProxyDirectories()
    {
        HashSet<string> seen = new(StringComparer.OrdinalIgnoreCase);
        foreach ((string directory, _) in ExecutableDirectories)
            if (!string.IsNullOrWhiteSpace(directory) && seen.Add(directory))
                yield return directory;
        foreach (string directory in DllDirectories)
            if (!string.IsNullOrWhiteSpace(directory) && seen.Add(directory))
                yield return directory;
    }

    private void ReadCreamApiConfig(string configPath)
    {
        try
        {
            if (!configPath.FileExists())
            {
                ProgramData.Log.Info($"[CreamAPI] Config not found: {configPath} | Game: {Name} ({Id})", LogDestination.Unlocker);
                return;
            }

            ProgramData.Log.Info($"[CreamAPI] Reading config: {configPath} | Game: {Name} ({Id})", LogDestination.Unlocker);
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
                        ProgramData.Log.Info($"[CreamAPI] ExtraProtection = {UseExtraProtection} | Game: {Name} ({Id})", LogDestination.Unlocker);
                    }
                    break;
                }
            }
        }
        catch (Exception e)
        {
            ProgramData.Log.Error($"[Unlocker] Error reading config: {configPath} | Game: {Name} ({Id})", e);
        }
    }

    public override bool Equals(object obj) => ReferenceEquals(this, obj) || obj is Selection other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(Id, (int)Platform);
}