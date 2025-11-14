using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CreamInstaller.Components;
using CreamInstaller.Forms;
using CreamInstaller.Utility;
using static CreamInstaller.Resources.Resources;

namespace CreamInstaller.Resources;

internal static class ScreamAPI
{
    internal static void GetScreamApiComponents(this string directory, out string api32, out string api32_o,
        out string api64, out string api64_o,
        out string old_config, out string config, out string old_log, out string log)
    {
        api32 = directory + @"\EOSSDK-Win32-Shipping.dll";
        api32_o = directory + @"\EOSSDK-Win32-Shipping_o.dll";
        api64 = directory + @"\EOSSDK-Win64-Shipping.dll";
        api64_o = directory + @"\EOSSDK-Win64-Shipping_o.dll";
        old_config = directory + @"\ScreamAPI.json";
		config = directory + @"\ScreamAPI.config.json";
        old_log = directory + @"\ScreamAPI.log";
		log = directory + @"\ScreamAPI.log.log";
    }

    internal static void CheckConfig(string directory, Selection selection, InstallForm installForm = null)
    {
        directory.GetScreamApiComponents(out _, out _, out _, out _, out string old_config, out string config, out _, out _);
        HashSet<SelectionDLC> overrideCatalogItems =
            selection.DLC.Where(dlc => dlc.Type is DLCType.Epic && !dlc.Enabled).ToHashSet();
        int entitlementCount = 0;
        HashSet<SelectionDLC> injectedEntitlements = [];
        foreach (SelectionDLC dlc in selection.DLC.Where(dlc => dlc.Type is DLCType.EpicEntitlement))
        {
            if (dlc.Enabled)
                _ = injectedEntitlements.Add(dlc);
            entitlementCount++;
        }

        foreach (Selection extraSelection in selection.ExtraSelections)
        {
            foreach (SelectionDLC extraDlc in extraSelection.DLC.Where(dlc => dlc.Type is DLCType.Epic && !dlc.Enabled))
                _ = overrideCatalogItems.Add(extraDlc);
            foreach (SelectionDLC extraDlc in extraSelection.DLC.Where(dlc => dlc.Type is DLCType.EpicEntitlement))
            {
                if (extraDlc.Enabled)
                    _ = injectedEntitlements.Add(extraDlc);
                entitlementCount++;
            }
        }

        if (injectedEntitlements.Count == entitlementCount)
            injectedEntitlements.Clear();

        if (config.FileExists())
        {
            config.DeleteFile();
            installForm?.UpdateUser($"Deleted unnecessary configuration: {Path.GetFileName(config)}", LogTextBox.Action,
                false);
        }
        /*if (installForm is not null)
            installForm.UpdateUser("Generating ScreamAPI configuration for " + selection.Name + $" in directory \"{directory}\" . . . ", LogTextBox.Operation);*/
        config.CreateFile(true, installForm)?.Close();
        StreamWriter writer = new(config, true, Encoding.UTF8);
        WriteConfig(writer,
            new(overrideCatalogItems.ToDictionary(dlc => dlc.Id, dlc => dlc), PlatformIdComparer.String),
            new(injectedEntitlements.ToDictionary(dlc => dlc.Id, dlc => dlc), PlatformIdComparer.String),
            installForm);
        writer.Flush();
        writer.Close();
    }

    private static void WriteConfig(StreamWriter writer, SortedList<string, SelectionDLC> overrideCatalogItems,
        SortedList<string, SelectionDLC> injectedEntitlements, InstallForm installForm = null)
    {
        writer.WriteLine("{");
        /*writer.WriteLine("  \"$schema\": \"https://raw.githubusercontent.com/acidicoala/ScreamAPI/refs/tags/v4.0.0/res/ScreamAPI.schema.json\",");*/
        writer.WriteLine("  \"$version\": 3,");
        writer.WriteLine("  \"logging\": false,");
        writer.WriteLine("  \"log_eos\": false,");
        writer.WriteLine("  \"block_metrics\": false,");
        writer.WriteLine("  \"namespace_id\": \"\",");
        writer.WriteLine("  \"default_dlc_status\": \"unlocked\",");
        if (overrideCatalogItems.Count > 0)
        {
            writer.WriteLine("  \"override_dlc_status\": {");
            KeyValuePair<string, SelectionDLC> lastOverrideCatalogItem = overrideCatalogItems.Last();
            foreach (KeyValuePair<string, SelectionDLC> pair in overrideCatalogItems)
            {
                SelectionDLC selectionDlc = pair.Value;
                writer.WriteLine($"      \"{selectionDlc.Id}\": \"locked\"{(pair.Equals(lastOverrideCatalogItem) ? "" : ",")}");
                installForm?.UpdateUser(
                    $"Added locked catalog item to ScreamAPI.json with id {selectionDlc.Id} ({selectionDlc.Name})",
                    LogTextBox.Action,
                    false);
            }

            writer.WriteLine("  },");
        }
        else
            writer.WriteLine("  \"override_dlc_status\": {},");

        writer.WriteLine("  \"extra_graphql_endpoints\": [],");
        writer.WriteLine("  \"extra_entitlements\": {}");
        /*if (injectedEntitlements.Count > 0)
        {
            writer.WriteLine("    \"default_dlc_status\": original,");
            writer.WriteLine("    \"inject\": [");
            KeyValuePair<string, SelectionDLC> lastEntitlement = injectedEntitlements.Last();
            foreach (KeyValuePair<string, SelectionDLC> pair in injectedEntitlements)
            {
                SelectionDLC selectionDlc = pair.Value;
                writer.WriteLine($"      \"{selectionDlc.Id}\"{(pair.Equals(lastEntitlement) ? "" : ",")}");
                installForm?.UpdateUser(
                    $"Added injected entitlement to ScreamAPI.json with id {selectionDlc.Id} ({selectionDlc.Name})",
                    LogTextBox.Action,
                    false);
            }

            writer.WriteLine("    ]");
        }
        else
        {
            writer.WriteLine("    \"unlock_all\": true,");
            writer.WriteLine("    \"auto_inject\": true,");
            writer.WriteLine("    \"inject\": []");
        }

        writer.WriteLine("  }");*/
        writer.WriteLine("}");
    }

    internal static async Task Uninstall(string directory, InstallForm installForm = null, bool deleteOthers = true)
        => await Task.Run(() =>
        {
            directory.GetScreamApiComponents(out string api32, out string api32_o, out string api64, out string api64_o,
                out string old_config, out string config, out string old_log, out string log);
            if (api32_o.FileExists())
            {
                if (api32.FileExists())
                {
                    api32.DeleteFile(true);
                    installForm?.UpdateUser($"Deleted ScreamAPI: {Path.GetFileName(api32)}", LogTextBox.Action, false);
                }

                api32_o.MoveFile(api32!);
                installForm?.UpdateUser($"Restored EOS: {Path.GetFileName(api32_o)} -> {Path.GetFileName(api32)}",
                    LogTextBox.Action, false);
            }

            if (api64_o.FileExists())
            {
                if (api64.FileExists())
                {
                    api64.DeleteFile(true);
                    installForm?.UpdateUser($"Deleted ScreamAPI: {Path.GetFileName(api64)}", LogTextBox.Action, false);
                }

                api64_o.MoveFile(api64!);
                installForm?.UpdateUser($"Restored EOS: {Path.GetFileName(api64_o)} -> {Path.GetFileName(api64)}",
                    LogTextBox.Action, false);
            }

            if (!deleteOthers)
                return;
            if (config.FileExists())
            {
                config.DeleteFile();
                installForm?.UpdateUser($"Deleted configuration: {Path.GetFileName(config)}", LogTextBox.Action, false);
            }

            if (log.FileExists())
            {
                log.DeleteFile();
                installForm?.UpdateUser($"Deleted log: {Path.GetFileName(log)}", LogTextBox.Action, false);
            }
        });

    internal static async Task Install(string directory, Selection selection, InstallForm installForm = null,
        bool generateConfig = true)
        => await Task.Run(() =>
        {
            directory.GetScreamApiComponents(out string api32, out string api32_o, out string api64, out string api64_o,
                out _, out _, out _, out _);
            if (api32.FileExists() && !api32_o.FileExists())
            {
                api32.MoveFile(api32_o!, true);
                installForm?.UpdateUser($"Renamed EOS: {Path.GetFileName(api32)} -> {Path.GetFileName(api32_o)}",
                    LogTextBox.Action, false);
            }

            if (api32_o.FileExists())
            {
                "ScreamAPI.EOSSDK-Win32-Shipping.dll".WriteManifestResource(api32);
                installForm?.UpdateUser($"Wrote ScreamAPI: {Path.GetFileName(api32)}", LogTextBox.Action, false);
            }

            if (api64.FileExists() && !api64_o.FileExists())
            {
                api64.MoveFile(api64_o!, true);
                installForm?.UpdateUser($"Renamed EOS: {Path.GetFileName(api64)} -> {Path.GetFileName(api64_o)}",
                    LogTextBox.Action, false);
            }

            if (api64_o.FileExists())
            {
                "ScreamAPI.EOSSDK-Win64-Shipping.dll".WriteManifestResource(api64);
                installForm?.UpdateUser($"Wrote ScreamAPI: {Path.GetFileName(api64)}", LogTextBox.Action, false);
            }

            if (generateConfig)
                CheckConfig(directory, selection, installForm);
        });

    internal static readonly Dictionary<ResourceIdentifier, HashSet<string>> ResourceMD5s = new()
    {
        [ResourceIdentifier.EpicOnlineServices32] =
        [
            "069A57B1834A960193D2AD6B96926D70", // ScreamAPI v3.0.0
            "E2FB3A4A9583FDC215832E5F935E4440", // ScreamAPI v3.0.1
            "8B4B30AFAE8D7B06413EE2F2266B20DB", // ScreamAPI v4.0.0-rc01
            "F2C1A6B3EF73ED14E810851DBF418453" // ScreamAPI v4.0.0
        ],
        [ResourceIdentifier.EpicOnlineServices64] =
        [
            "0D62E57139F1A64F807A9934946A9474", // ScreamAPI v3.0.0
            "3875C7B735EE80C23239CC4749FDCBE6", // ScreamAPI v3.0.1
            "CBC89E2221713B0D4482F91282030A88", // ScreamAPI v4.0.0-rc01
            "2F98D62283AA024CBD756921B9533489" // ScreamAPI v4.0.0
        ]
    };
}