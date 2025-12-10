using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using CreamInstaller.Resources;
using CreamInstaller.Utility;
using static CreamInstaller.Resources.Resources;

namespace CreamInstaller.Forms;

// Partial class containing Save/Load/Reset selection functionality
internal sealed partial class SelectForm
{
    #region Selection State Checks

    private bool AreSelectionsDefault()
        => EnumerateTreeNodes(selectionTreeView.Nodes).All(node
            => node.Parent is null || node.Tag is not Platform and not DLCType ||
               (node.Text == "Unknown" ? !node.Checked : node.Checked));

    private static bool AreProxySelectionsDefault() => Selection.All.Keys.All(selection => !selection.UseProxy);

    private bool CanSaveDlc() =>
        installButton.Enabled && (ProgramData.ReadDlcChoices().Any() || !AreSelectionsDefault());

    private static bool CanSaveProxy() =>
        ProgramData.ReadProxyChoices().Any() || !AreProxySelectionsDefault();

    private bool CanSaveSelections() => CanSaveDlc() || CanSaveProxy();

    private static bool CanLoadDlc() => ProgramData.ReadDlcChoices().Any();

    private static bool CanLoadProxy() => ProgramData.ReadProxyChoices().Any();

    private static bool CanLoadSelections() => CanLoadDlc() || CanLoadProxy();

    private bool CanResetDlc() => !AreSelectionsDefault();

    private static bool CanResetProxy() => !AreProxySelectionsDefault();

    private bool CanResetSelections() => CanResetDlc() || CanResetProxy();

    #endregion

    #region Selection Event Handlers

    private void OnSaveSelections(object sender, System.EventArgs e)
    {
        List<(Platform platform, string gameId, string dlcId)> dlcChoices = ProgramData.ReadDlcChoices().ToList();
        foreach (SelectionDLC dlc in SelectionDLC.All.Keys)
        {
            _ = dlcChoices.RemoveAll(n =>
                n.platform == dlc.Selection.Platform && n.gameId == dlc.Selection.Id && n.dlcId == dlc.Id);
            if (dlc.Name == "Unknown" ? dlc.Enabled : !dlc.Enabled)
                dlcChoices.Add((dlc.Selection.Platform, dlc.Selection.Id, dlc.Id));
        }

        ProgramData.WriteDlcChoices(dlcChoices);

        List<(Platform platform, string id, string proxy, bool enabled)> proxyChoices =
            ProgramData.ReadProxyChoices().ToList();
        foreach (Selection selection in Selection.All.Keys)
        {
            _ = proxyChoices.RemoveAll(c => c.platform == selection.Platform && c.id == selection.Id);
            if (selection.UseProxy)
                proxyChoices.Add((selection.Platform, selection.Id,
                    selection.Proxy == Selection.DefaultProxy ? null : selection.Proxy,
                    selection.UseProxy));
        }

        ProgramData.WriteProxyChoices(proxyChoices);

        loadButton.Enabled = CanLoadSelections();
        saveButton.Enabled = CanSaveSelections();
    }

    private void OnLoadSelections(object sender, System.EventArgs e)
    {
        List<(Platform platform, string gameId, string dlcId)> dlcChoices = ProgramData.ReadDlcChoices().ToList();
        foreach (SelectionDLC dlc in SelectionDLC.All.Keys)
        {
            dlc.Enabled = dlcChoices.Any(c =>
                c.platform == dlc.Selection?.Platform && c.gameId == dlc.Selection?.Id && c.dlcId == dlc.Id)
                ? dlc.Name == "Unknown"
                : dlc.Name != "Unknown";
            OnTreeViewNodeCheckedChanged("OnLoadSelections", new TreeViewEventArgs(dlc.TreeNode, TreeViewAction.ByMouse));
        }

        List<(Platform platform, string id, string proxy, bool enabled)> proxyChoices =
            ProgramData.ReadProxyChoices().ToList();
        foreach (Selection selection in Selection.All.Keys)
            if (proxyChoices.Any(c => c.platform == selection.Platform && c.id == selection.Id))
            {
                (Platform platform, string id, string proxy, bool enabled)
                    choice = proxyChoices.First(c => c.platform == selection.Platform && c.id == selection.Id);
                (Platform platform, string id, string proxy, bool enabled) = choice;
                string currentProxy = proxy;
                if (proxy is not null && proxy.Contains('.'))
                    proxy.GetProxyInfoFromIdentifier(out currentProxy, out _);
                if (proxy != currentProxy && proxyChoices.Remove(choice))
                    proxyChoices.Add((platform, id, currentProxy, enabled));
                if (currentProxy is null or Selection.DefaultProxy && !enabled)
                    _ = proxyChoices.RemoveAll(c => c.platform == platform && c.id == id);
                else
                {
                    selection.UseProxy = enabled;
                    selection.Proxy = currentProxy == Selection.DefaultProxy ? currentProxy : proxy;
                }
            }
            else
            {
                selection.UseProxy = false;
                selection.Proxy = null;
            }

        ProgramData.WriteProxyChoices(proxyChoices);
        loadButton.Enabled = CanLoadSelections();

        OnProxyChanged();
    }

    private void OnResetSelections(object sender, System.EventArgs e)
    {
        foreach (SelectionDLC dlc in SelectionDLC.All.Keys)
        {
            dlc.Enabled = dlc.Name != "Unknown";
            OnTreeViewNodeCheckedChanged("OnResetSelections", new TreeViewEventArgs(dlc.TreeNode, TreeViewAction.ByMouse));
        }

        foreach (Selection selection in Selection.All.Keys)
        {
            selection.UseProxy = false;
            selection.Proxy = null;
        }

        OnProxyChanged();
    }

    #endregion
}
