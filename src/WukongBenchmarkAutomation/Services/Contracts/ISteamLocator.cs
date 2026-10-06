namespace WukongBenchmarkAutomation.Services.Contracts;

// Интерфейс для поиска исполняемого файла Steam и каталога бенчмарка.
public interface ISteamLocator
{
    string FindSteamExe();
    string FindGameDir(string steamExe);
}
