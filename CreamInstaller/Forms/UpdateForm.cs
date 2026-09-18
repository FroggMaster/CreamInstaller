using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using CreamInstaller.Components;
using CreamInstaller.Utility;
using Newtonsoft.Json;

namespace CreamInstaller.Forms;

internal sealed partial class UpdateForm : CustomForm
{
    private static readonly string PackagePath = ProgramData.DirectoryPath + @"\" + Program.RepositoryPackage;
    private static readonly string ExecutablePath = ProgramData.DirectoryPath + @"\" + Program.RepositoryExecutable;
    private static readonly string UpdaterPath = ProgramData.DirectoryPath + @"\updater.cmd";

    private readonly bool dialogMode;
    private CancellationTokenSource cancellation;
    private ProgramRelease latestRelease;

    internal bool CheckCompleted { get; private set; }
    internal bool CheckFailed { get; private set; }
    internal bool FoundUpdate { get; private set; }

    internal UpdateForm(bool dialogMode = false)
    {
        InitializeComponent();
        Text = Program.ApplicationNameShort;
        this.dialogMode = dialogMode;
    }

    /// <summary>
    /// Performs an on-demand update check from the settings dialog, reusing the same prompt shown
    /// at launch. When no update is available (or the check fails), an informational dialog is shown.
    /// </summary>
    internal static void ShowUpdateCheck(Form owner)
    {
        using UpdateForm form = new(true);
        form.Owner = owner;
        form.StartPosition = FormStartPosition.CenterParent;
        _ = form.ShowDialog(owner);
        if (!form.CheckCompleted || form.FoundUpdate)
            return;
        using DialogForm dialog = new(owner);
        if (form.CheckFailed)
            _ = dialog.Show(SystemIcons.Error,
                "An error occurred while checking for updates.\n\nPlease verify your internet connection and try again.",
                acceptButtonText: "OK", customFormText: "Update Check Failed");
        else
            _ = dialog.Show(SystemIcons.Information,
                $"You are already running the latest version of {Program.Name}.",
                acceptButtonText: "OK", customFormText: "No Updates Available");
    }

    private void StartProgram()
    {
        MainForm form = MainForm.Current;
        form.InheritLocation(this);
        form.FormClosing += (_, _) => Close();
        form.Show();
        Hide();
#if DEBUG
        DebugForm.Current.Open(form);
#endif
        ThemeManager.Apply(form); // apply current theme when transitioning
    }

    private async void OnLoad()
    {
        try
        {
            progressBar.Visible = false;
            ignoreButton.Visible = true;
            updateButton.Text = "Update";
            updateButton.Click -= OnUpdateCancel;
            progressLabel.Text = "Checking for updates . . .";
            changelogTreeView.Visible = false;
            changelogTreeView.Location = progressLabel.Location with
            {
                Y = progressLabel.Location.Y + progressLabel.Size.Height + 13
            };
            Refresh();
#if !DEBUG
            Version currentVersion = new(Program.Version);
#endif
            List<ProgramRelease> releases = null;
            (string response, _) =
                await HttpClientManager.EnsureGet(
                    $"https://api.github.com/repos/{Program.RepositoryOwner}/{Program.RepositoryName}/releases");
            CheckFailed = response is null;
            if (response is not null)
                releases = JsonConvert.DeserializeObject<List<ProgramRelease>>(response)
                    ?.Where(release => !release.Draft && release.Asset is not null
                                       && (Program.CheckPreReleases || !release.Prerelease)).ToList();
            latestRelease = FindUpdate(releases);
            CheckCompleted = true;
            if (latestRelease is null)
                ReturnToProgram();
            else
            {
                FoundUpdate = true;
                progressLabel.Text = latestRelease.Prerelease
                    ? $"A pre-release update is available: {latestRelease.CommitHash}"
                    : $"An update is available: v{latestRelease.Version}";
                ignoreButton.Enabled = true;
                updateButton.Enabled = true;
                updateButton.Click += OnUpdate;
                changelogTreeView.Visible = true;
                foreach (ProgramRelease release in releases)
                {
                    if (release.Prerelease && !ReferenceEquals(release, latestRelease))
                        continue;
#if !DEBUG
                    if (release.Version is { } releaseVersion && releaseVersion <= currentVersion)
                        continue;
#endif
                    TreeNode root = new(release.Name) { Name = release.Name };
                    changelogTreeView.Nodes.Add(root);
                    if (changelogTreeView.Nodes.Count > 0)
                        changelogTreeView.Nodes[0].EnsureVisible();
                    foreach (string change in release.Changes)
                        Invoke(delegate
                        {
                            TreeNode changeNode = new() { Text = change };
                            root.Nodes.Add(changeNode);
                            root.Expand();
                            if (changelogTreeView.Nodes.Count > 0)
                                changelogTreeView.Nodes[0].EnsureVisible();
                        });
                }
            }
        }
        catch (Exception ex)
        {
            ProgramData.Log.Error("UpdateForm OnLoad failed", ex);
            CheckFailed = true;
            CheckCompleted = true;
#if DEBUG
            ex.HandleFatalException();
#else
            ReturnToProgram();
#endif
        }
    }

    private void ReturnToProgram()
    {
        if (dialogMode)
        {
            if (!IsDisposed && !Disposing)
                Close();
        }
        else
            StartProgram();
    }

    /// <summary>
    /// Selects the release to offer as an update. Stable releases win when their version is newer
    /// than the running build; otherwise, when pre-release checking is enabled, the rolling
    /// pre-release is offered whenever its commit hash differs from this build's commit hash.
    /// </summary>
    private static ProgramRelease FindUpdate(List<ProgramRelease> releases)
    {
        if (releases is null || releases.Count == 0)
            return null;
#if DEBUG
        return releases[0];
#else
        ProgramRelease latestStable = releases.FirstOrDefault(release => !release.Prerelease);
        if (latestStable?.Version is { } stableVersion && stableVersion > new Version(Program.Version))
            return latestStable;
        if (Program.CheckPreReleases)
        {
            ProgramRelease prerelease = releases.FirstOrDefault(release => release.Prerelease);
            if (prerelease?.CommitHash is { } commitHash
                && (Program.ShortCommitHash is null
                    || !commitHash.Equals(Program.ShortCommitHash, StringComparison.OrdinalIgnoreCase)))
                return prerelease;
        }

        return null;
#endif
    }

    private void OnLoad(object sender, EventArgs _)
    {
        bool retry = true;
        while (retry)
        {
            try
            {
                UpdaterPath.DeleteFile();
                OnLoad();
                retry = false;
            }
            catch (Exception e)
            {
                retry = e.HandleException(this);
                if (!retry)
                    Close();
            }
        }
    }

    private void OnIgnore(object sender, EventArgs e) => ReturnToProgram();

    private async void OnUpdate(object sender, EventArgs e)
    {
        try
        {
            progressBar.Value = 0;
            progressBar.Visible = true;
            ignoreButton.Visible = false;
            updateButton.Text = "Cancel";
            updateButton.Click -= OnUpdate;
            updateButton.Click += OnUpdateCancel;
            changelogTreeView.Location =
                progressBar.Location with { Y = progressBar.Location.Y + progressBar.Size.Height + 6 };
            Refresh();
            Progress<int> progress = new();
            IProgress<int> iProgress = progress;
            progress.ProgressChanged += delegate(object _, int _progress)
            {
                progressLabel.Text = $"Updating . . . {_progress}%";
                progressBar.Value = _progress;
            };
            progressLabel.Text = "Updating . . . ";
            cancellation = new();
            bool success = true;
            bool prerelease = latestRelease.Prerelease;
            string downloadPath = prerelease ? ExecutablePath : PackagePath;
            downloadPath.DeleteFile(true);
            await using FileStream update = downloadPath.CreateFile(true);
            bool retry = true;
            try
            {
                if (cancellation is null || Program.Canceled)
                    throw new TaskCanceledException();
                using HttpResponseMessage response = await HttpClientManager.HttpClient.GetAsync(
                    latestRelease.Asset.BrowserDownloadUrl,
                    HttpCompletionOption.ResponseHeadersRead, cancellation.Token);
                _ = response.EnsureSuccessStatusCode();
                if (cancellation is null || Program.Canceled)
                    throw new TaskCanceledException();
                await using Stream download = await response.Content.ReadAsStreamAsync(cancellation.Token);
                double bytes = latestRelease.Asset.Size;
                byte[] buffer = new byte[16384];
                long bytesRead = 0;
                int newBytes;
                while (cancellation is not null && !Program.Canceled
                                                && (newBytes = await download.ReadAsync(buffer.AsMemory(0, buffer.Length),
                                                    cancellation.Token)) != 0)
                {
                    if (cancellation is null || Program.Canceled)
                        throw new TaskCanceledException();
                    await update.WriteAsync(buffer.AsMemory(0, newBytes), cancellation.Token);
                    bytesRead += newBytes;
                    int report = (int)(bytesRead / bytes * 100);
                    if (report <= progressBar.Value)
                        continue;
                    iProgress.Report(report);
                }

                iProgress.Report((int)(bytesRead / bytes * 100));
                if (cancellation is null || Program.Canceled)
                    throw new TaskCanceledException();
            }
            catch (TaskCanceledException)
            {
                success = false;
            }
            catch (Exception ex)
            {
                retry = ex.HandleException(this, Program.Name + " encountered an exception while updating");
                success = false;
            }

            cancellation?.Dispose();
            cancellation = null;
            await update.DisposeAsync();
            bool canContinue = success && !Program.Canceled;
            if (canContinue)
                updateButton.Enabled = false;
            if (prerelease)
            {
                // Pre-release builds are published as a standalone executable that has already
                // been staged directly at ExecutablePath, so no archive extraction is needed.
                if (!canContinue)
                    ExecutablePath.DeleteFile();
            }
            else
            {
                ExecutablePath.DeleteFile(canContinue);
                if (canContinue)
                    await Task.Run(() => PackagePath.ExtractZip(ProgramData.DirectoryPath, true, this));
                PackagePath.DeleteFile(canContinue);
            }
            if (canContinue)
            {
                string path = Program.CurrentProcessFilePath;
                string directory = Path.GetDirectoryName(path);
                string file = Path.GetFileName(path);
                StringBuilder commands = new();
                _ = commands.AppendLine(CultureInfo.InvariantCulture, $"chcp 65001");
                _ = commands.AppendLine(CultureInfo.InvariantCulture, $":LOOP");
                _ = commands.AppendLine(CultureInfo.InvariantCulture, $"TASKKILL /F /T /PID {Program.CurrentProcessId}");
                _ = commands.AppendLine(CultureInfo.InvariantCulture, $"TASKLIST | FIND \" {Program.CurrentProcessId} \"");
                _ = commands.AppendLine(CultureInfo.InvariantCulture, $"IF NOT ERRORLEVEL 1 (");
                _ = commands.AppendLine(CultureInfo.InvariantCulture, $"   TIMEOUT /T 1");
                _ = commands.AppendLine(CultureInfo.InvariantCulture, $"   GOTO LOOP");
                _ = commands.AppendLine(CultureInfo.InvariantCulture, $")");
                _ = commands.AppendLine(CultureInfo.InvariantCulture, $"MOVE /Y \"{ExecutablePath}\" \"{path}\"");
                _ = commands.AppendLine(CultureInfo.InvariantCulture, $"START \"\" /D \"{directory}\" \"{file}\"");
#if DEBUG
                _ = commands.AppendLine(CultureInfo.InvariantCulture, $"PAUSE");
#endif
                _ = commands.AppendLine(CultureInfo.InvariantCulture, $"EXIT");
                UpdaterPath.WriteFile(commands.ToString(), true, this, Encoding.Default);
                Process process = new();
                ProcessStartInfo startInfo = new()
                {
                    WorkingDirectory = ProgramData.DirectoryPath, FileName = "cmd.exe",
                    Arguments = $"/C START \"UPDATER\" /B {Path.GetFileName(UpdaterPath)}",
#if DEBUG
                    CreateNoWindow = false
#else
                    CreateNoWindow = true
#endif
                };
                process.StartInfo = startInfo;
                _ = process.Start();
                return;
            }

            if (!retry)
                ReturnToProgram();
            else
                OnLoad();
        }
        catch (Exception ex)
        {
            ProgramData.Log.Error("UpdateForm OnUpdate failed", ex);
            // Show error to user
            ex.HandleException(this, Program.Name + " encountered an unexpected error during update");
            ReturnToProgram();
        }
    }

    private void OnUpdateCancel(object sender, EventArgs e)
    {
        cancellation?.Cancel();
        cancellation?.Dispose();
        cancellation = null;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            components?.Dispose();
        base.Dispose(disposing);
        OnUpdateCancel(null, null);
    }
}