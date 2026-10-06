using WukongBenchmarkAutomation.Services;
using Xunit;

namespace WukongBenchmarkAutomation.Tests;

public class SteamLocatorTests
{
    private const string SampleVdf = """
    "libraryfolders"
    {
        "0"
        {
            "path"      "C:\\Program Files (x86)\\Steam"
            "label"     ""
        }
        "1"
        {
            "path"      "D:\\SteamLibrary"
            "label"     "Games SSD"
        }
        "2"
        {
            "path"      "E:\\Games\\SteamLibrary"
            "label"     ""
        }
    }
    """;

    [Fact]
    public void ParseLibraryFolders_ExtractsAllUniquePaths()
    {
        var paths = SteamLocator.ParseLibraryFolders(SampleVdf);

        Assert.Equal(3, paths.Count);
        Assert.Contains(@"C:\Program Files (x86)\Steam", paths);
        Assert.Contains(@"D:\SteamLibrary", paths);
        Assert.Contains(@"E:\Games\SteamLibrary", paths);
    }

    [Fact]
    public void ParseLibraryFolders_WhenEmptyOrCorrupted_ReturnsEmptyList()
    {
        Assert.Empty(SteamLocator.ParseLibraryFolders(""));
        Assert.Empty(SteamLocator.ParseLibraryFolders("invalid content with no paths"));
    }
}
