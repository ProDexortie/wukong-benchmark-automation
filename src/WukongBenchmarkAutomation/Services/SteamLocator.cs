using System.Text.RegularExpressions;
using Microsoft.Win32;
using WukongBenchmarkAutomation.Services.Contracts;

namespace WukongBenchmarkAutomation.Services;

// Автоматически ищет Steam и папку с установленным Benchmark Tool
public class SteamLocator : ISteamLocator
{
    public const string AppId = "3132990";
    public const string InstallDirName = "Black Myth Wukong Benchmark Tool";

    string ISteamLocator.FindSteamExe() => FindSteamExe();
    string ISteamLocator.FindGameDir(string steamExe) => FindGameDir(steamExe);

    public static string FindSteamExe()
    {
        // Steam при установке сам пишет свой путь в реестр текущего пользователя
        var steamExe = Registry.GetValue(@"HKEY_CURRENT_USER\Software\Valve\Steam", "SteamExe", null) as string;
        if (!string.IsNullOrEmpty(steamExe) && File.Exists(steamExe))
            return Path.GetFullPath(steamExe);

        throw new FileNotFoundException("Steam не найден в реестре. Установите Steam или запустите его хотя бы один раз.");
    }

    public static string FindGameDir(string steamExe)
    {
        var steamDir = Path.GetDirectoryName(steamExe)!;

        // Библиотек Steam может быть несколько (диски C:, D:, ...), список лежит в libraryfolders.vdf
        var libraries = new List<string> { steamDir };
        var vdfPath = Path.Combine(steamDir, "steamapps", "libraryfolders.vdf");
        if (File.Exists(vdfPath))
        {
            libraries.AddRange(ParseLibraryFolders(File.ReadAllText(vdfPath)));
        }

        foreach (var library in libraries)
        {
            var gameDir = Path.Combine(library, "steamapps", "common", InstallDirName);
            if (File.Exists(Path.Combine(gameDir, "b1_benchmark.exe")))
                return gameDir;
        }

        throw new DirectoryNotFoundException(
            "Black Myth: Wukong Benchmark Tool не найден. " +
            "Укажите папку вручную: --game-dir \"D:\\...\\Black Myth Wukong Benchmark Tool\"");
    }

    // Парсит пути дополнительных библиотек установки Steam из содержимого libraryfolders.vdf
    public static List<string> ParseLibraryFolders(string vdfContent)
    {
        var libraries = new List<string>();
        foreach (Match m in Regex.Matches(vdfContent, "\"path\"\\s+\"([^\"]+)\""))
        {
            var path = m.Groups[1].Value.Replace(@"\\", @"\");
            if (!string.IsNullOrWhiteSpace(path) && !libraries.Contains(path))
            {
                libraries.Add(path);
            }
        }
        return libraries;
    }
}
