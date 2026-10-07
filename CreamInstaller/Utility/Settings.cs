namespace CreamInstaller.Utility;

public enum DefaultAppStatus
{
    Unlocked,
    Locked,
    Original
}

public enum GamesLayout
{
    Stacked = 0,
    Tabs
}

internal sealed class SettingsModel
{
    public bool UseSmokeAPI { get; set; } = true;
    public bool BlockProtectedGames { get; set; } = true;
    public bool DarkModeEnabled { get; set; } = true;
    public bool SortByName { get; set; } = true;
    public bool CheckPreReleases { get; set; }
    public DefaultAppStatus DefaultAppStatus { get; set; } = DefaultAppStatus.Unlocked;
    public GamesLayout GamesLayout { get; set; } = GamesLayout.Tabs;
}
