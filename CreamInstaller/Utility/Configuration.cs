using System.Collections.Generic;
using System.Linq;

namespace CreamInstaller.Utility;

internal static class Configuration
{
    internal static bool BlockProtectedGames = true;
    internal static List<string> ProtectedGames = new() { "PAYDAY 2" };
    internal static List<string> ProtectedGameDirectories = new() { @"\EasyAntiCheat", @"\BattlEye" };
    internal static List<string> ProtectedGameDirectoryExceptions = new();

    // Added for SelectForm support
    internal static bool IsGameBlocked(string name, string directory = null)
        => BlockProtectedGames && (ProtectedGames.Contains(name) || (directory is not null &&
            !ProtectedGameDirectoryExceptions.Contains(name)
            && ProtectedGameDirectories.Any(path => (directory + path).DirectoryExists())));
}
