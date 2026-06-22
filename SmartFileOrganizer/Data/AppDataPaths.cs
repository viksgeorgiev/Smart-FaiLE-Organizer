using System.IO;

namespace SmartFileOrganizer.Data;

public static class AppDataPaths
{
    private static readonly string AppFolder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "SmartFileOrganizer");

    public static string RulesFilePath => Path.Combine(AppFolder, "rules.json");

    public static string LogFilePath => Path.Combine(AppFolder, "logs.json");

    public static void EnsureAppFolderExists()
    {
        Directory.CreateDirectory(AppFolder);
    }
}
