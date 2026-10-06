namespace WukongBenchmarkAutomation.Services.Contracts;

using WukongBenchmarkAutomation.Models;

// Интерфейс для управления файлом GameUserSettings.ini
public interface IGameSettingsWriter
{
    string IniPath { get; }
    void Backup();
    void Restore();
    void Apply(BenchmarkProfile profile);
}
