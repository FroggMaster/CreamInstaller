using System;
using System.Collections.Concurrent;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using CreamInstaller.Platforms.Paradox;
using CreamInstaller.Utility;

namespace CreamInstaller.Components;

internal sealed class ContextMenuItem : ToolStripMenuItem
{
    private static readonly ConcurrentDictionary<string, Image> Images = new();

    private readonly EventHandler onClickEvent;

    internal ContextMenuItem(string text, EventHandler onClick = null)
    {
        Text = text;
        onClickEvent = onClick;
    }

    internal ContextMenuItem(string text, string imageIdentifier, EventHandler onClick = null) : this(text, onClick)
        => _ = TryImageIdentifier(this, imageIdentifier);

    internal ContextMenuItem(string text, (string id, string iconUrl) imageIdentifierInfo, EventHandler onClick = null)
        : this(text, onClick)
        => _ = TryImageIdentifierInfo(this, imageIdentifierInfo);

    internal ContextMenuItem(string text, (string id, string iconUrl) imageIdentifierInfo,
        string imageIdentifierFallback, EventHandler onClick = null) :
        this(text, onClick)
    {
        async void OnFail() => await TryImageIdentifier(this, imageIdentifierFallback);
        _ = TryImageIdentifierInfo(this, imageIdentifierInfo, OnFail);
    }

    internal ContextMenuItem(string text, (string id, string iconUrl) imageIdentifierInfo,
        (string id, string iconUrl) imageIdentifierInfoFallback,
        EventHandler onClick = null) : this(text, onClick)
    {
        async void OnFail() => await TryImageIdentifierInfo(this, imageIdentifierInfoFallback);
        _ = TryImageIdentifierInfo(this, imageIdentifierInfo, OnFail);
    }

    private static async Task TryImageIdentifier(ContextMenuItem item, string imageIdentifier)
    {
        if (Images.TryGetValue(imageIdentifier, out Image image) && image is not null)
        {
            item.Image = image;
            return;
        }

        image = await Task.Run(async () =>
        {
            switch (imageIdentifier)
            {
                case "Paradox Launcher":
                    if (ParadoxLauncher.InstallPath.DirectoryExists())
                        foreach (string file in ParadoxLauncher.InstallPath.EnumerateDirectory("*.exe"))
                            return file.GetFileIconImage();
                    break;
                case "Notepad":
                    return IconGrabber.GetNotepadImage();
                case "Command Prompt":
                    return IconGrabber.GetCommandPromptImage();
                case "File Explorer":
                    return IconGrabber.GetFileExplorerImage();
                case "SteamDB":
                    return await HttpClientManager.GetImageFromUrl(
                        IconGrabber.GetDomainFaviconUrl("steamdb.info"));
                case "Steam Store":
                    return await HttpClientManager.GetImageFromUrl(
                        IconGrabber.GetDomainFaviconUrl("store.steampowered.com"));
                case "Steam Community":
                    return await HttpClientManager.GetImageFromUrl(
                        IconGrabber.GetDomainFaviconUrl("steamcommunity.com"));
                case "ScreamDB":
                    return await HttpClientManager.GetImageFromUrl(
                        IconGrabber.GetDomainFaviconUrl("scream-db.web.app"));
                case "Epic Games":
                    return await HttpClientManager.GetImageFromUrl(
                        IconGrabber.GetDomainFaviconUrl("epicgames.com"));
                case "Ubisoft Store":
                    return await HttpClientManager.GetImageFromUrl(
                        IconGrabber.GetDomainFaviconUrl("store.ubi.com"));
            }
            return null;
        });

        if (image is not null)
        {
            Images[imageIdentifier] = image;
            item.Image = image;
        }
    }

    private static async Task TryImageIdentifierInfo(ContextMenuItem item,
        (string id, string iconUrl) imageIdentifierInfo, Action onFail = null)
    {
        try
        {
            (string id, string iconUrl) = imageIdentifierInfo;
            string imageIdentifier = "Icon_" + id;
            
            if (Images.TryGetValue(imageIdentifier, out Image image) && image is not null)
            {
                item.Image = image;
                return;
            }

            image = await HttpClientManager.GetImageFromUrl(iconUrl);
            if (image is not null)
            {
                Images[imageIdentifier] = image;
                item.Image = image;
            }
            else
                onFail?.Invoke();
        }
        catch
        {
            // ignored
        }
    }

    protected override void OnClick(EventArgs e)
    {
        base.OnClick(e);
        onClickEvent?.Invoke(this, e);
    }
}