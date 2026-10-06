namespace WukongBenchmarkAutomation.Services.Contracts;

using WukongBenchmarkAutomation.Models;

// Интерфейс для запуска бенчмарка и получения результатов.
public interface IBenchmarkRunner
{
    bool IsGameRunning();
    BenchmarkResult Run();
    void StopGame();
}
