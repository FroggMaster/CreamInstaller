using System;
using System.Collections.Generic;
using System.Diagnostics;
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

    private CancellationTokenSource cancellation;
    private ProgramRelease latestRelease;

    internal UpdateForm()
    {
        InitializeComponent();
        Text = Program.ApplicationNameShort;
    }

    private void StartProgram()
    {
        SelectForm form = SelectForm.Current;
        form.InheritLocation(this);
        form.FormClosing += (_, _) => Close();
        form.Show();
        Hide();
#if DEBUG
        DebugForm.Current.Attach(form);
#endif
        ThemeManager.Apply(form); // apply current theme when transitioning
    }

    private void ResetUpdateUI()
    {
        progressBar.Visible = false;
        progressBar.Value = 0;
        ignoreButton.Visible = true;
        ignoreButton.Enabled = true;
        updateButton.Text = "Update";
        updateButton.Enabled = true;
        updateButton.Click -= OnUpdateCancel;
        updateButton.Click -= OnUpdate;
        updateButton.Click += OnUpdate;
        changelogTreeView.Location = progressLabel.Location with
        {
            Y = progressLabel.Location.Y + progressLabel.Size.Height + 13
        };
    }

    private async Task CheckForUpdatesAsync()
    {
        progressBar.Visible = false;
        ignoreButton.Visible = true;
        ignoreButton.Enabled = false;
        updateButton.Text = "Update";
        updateButton.Enabled = false;
        updateButton.Click -= OnUpdateCancel;
        updateButton.Click -= OnUpdate;
        progressLabel.Text = $"Checking for updates . . . (current: v{Program.Version})";
        changelogTreeView.Visible = false;
        changelogTreeView.Nodes.Clear();
        changelogTreeView.Location = progressLabel.Location with
        {
            Y = progressLabel.Location.Y + progressLabel.Size.Height + 13
        };
        Refresh();
#if !DEBUG
        Version currentVersion = new(Program.Version);
#endif
        List<ProgramRelease> releases = null;
        bool networkError = false;
        try
        {
            string response =
                await HttpClientManager.EnsureGet(
                    $"https://api.github.com/repos/{Program.RepositoryOwner}/{Program.RepositoryName}/releases");
            if (response is not null)
                releases = JsonConvert.DeserializeObject<List<ProgramRelease>>(response)
                    ?.Where(release => !release.Draft && !release.Prerelease && release.Asset is not null).ToList();
            else
                networkError = true;
        }
        catch
        {
            networkError = true;
        }

        if (networkError)
        {
            progressLabel.Text = $"Could not check for updates (v{Program.Version})";
            ignoreButton.Enabled = true;
            ignoreButton.Text = "Continue";
            return;
        }

        latestRelease = releases?.FirstOrDefault();
#if DEBUG
        if (latestRelease?.Version is not { } latestVersion)
#else
        if (latestRelease?.Version is not { } latestVersion || latestVersion <= currentVersion)
#endif
            StartProgram();
        else
        {
            progressLabel.Text = $"Update available: v{Program.Version} → v{latestVersion}";
            ignoreButton.Enabled = true;
            ignoreButton.Text = "Later";
            updateButton.Enabled = true;
            updateButton.Click += OnUpdate;
            changelogTreeView.Visible = true;
            foreach (ProgramRelease release in releases)
            {
#if !DEBUG
                if (release.Version <= currentVersion)
                    continue;
#endif
                TreeNode root = new(release.Name) { Name = release.Name };
                changelogTreeView.Nodes.Add(root);
                foreach (string change in release.Changes)
                {
                    TreeNode changeNode = new() { Text = change };
                    root.Nodes.Add(changeNode);
                }
                root.Expand();
            }
            if (changelogTreeView.Nodes.Count > 0)
                changelogTreeView.Nodes[0].EnsureVisible();
        }
    }

    private async void OnLoad(object sender, EventArgs _)
    {
        bool shouldRetry = true;
        while (shouldRetry)
        {
            shouldRetry = false;
            try
            {
                UpdaterPath.DeleteFile();
                await CheckForUpdatesAsync();
            }
            catch (Exception e)
            {
                if (e.HandleException(this))
                    shouldRetry = true;
                else
                    Close();
            }
        }
    }

    private void OnIgnore(object sender, EventArgs e) => StartProgram();

    private async void OnUpdate(object sender, EventArgs e)
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
        double totalBytes = latestRelease.Asset.Size;
        double totalMB = totalBytes / (1024 * 1024);
        Progress<(int percent, long downloaded)> progress = new();
        IProgress<(int percent, long downloaded)> iProgress = progress;
        progress.ProgressChanged += delegate(object _, (int percent, long downloaded) p)
        {
            double downloadedMB = p.downloaded / (1024.0 * 1024.0);
            progressLabel.Text = $"Downloading . . . {downloadedMB:F1} / {totalMB:F1} MB ({p.percent}%)";
            progressBar.Value = p.percent;
        };
        progressLabel.Text = $"Downloading . . . 0 / {totalMB:F1} MB";
        cancellation = new();
        bool success = true;
        PackagePath.DeleteFile(true);
        await using FileStream update = PackagePath.CreateFile(true);
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
                int report = (int)(bytesRead / totalBytes * 100);
                if (report <= progressBar.Value)
                    continue;
                iProgress.Report((report, bytesRead));
            }

            iProgress.Report(((int)(bytesRead / totalBytes * 100), bytesRead));
            if (cancellation is null || Program.Canceled)
                throw new TaskCanceledException();
        }
        catch (TaskCanceledException)
        {
            success = false;
            retry = false; // Don't retry on user cancel, just reset UI
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

        // Handle cancel - reset UI so user can retry or skip
        if (!canContinue && !retry)
        {
            progressLabel.Text = "Update cancelled";
            ResetUpdateUI();
            return;
        }

        if (canContinue)
            updateButton.Enabled = false;
        ExecutablePath.DeleteFile(canContinue);
        if (canContinue)
        {
            progressLabel.Text = "Extracting update . . .";
            progressBar.Style = ProgressBarStyle.Marquee;
            Refresh();
            await Task.Run(() => PackagePath.ExtractZip(ProgramData.DirectoryPath, true, this));
            progressBar.Style = ProgressBarStyle.Blocks;
        }
        PackagePath.DeleteFile(canContinue);
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
            StartProgram();
        else
            await CheckForUpdatesAsync();
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