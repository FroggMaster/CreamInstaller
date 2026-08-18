namespace CreamInstaller.Utility;

public enum DefaultAppStatus
{
    Unlocked,
    Locked,
    Original
}

internal sealed class SettingsModel
{
    public bool UseSmokeAPI { get; set; } = true;
    public bool BlockProtectedGames { get; set; } = true;
    public bool DarkModeEnabled { get; set; } = true;
    public bool SortByName { get; set; } = true;
    public DefaultAppStatus DefaultAppStatus { get; set; } = DefaultAppStatus.Unlocked;
}
