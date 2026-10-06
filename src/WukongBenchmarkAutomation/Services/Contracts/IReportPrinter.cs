namespace WukongBenchmarkAutomation.Services.Contracts;

using WukongBenchmarkAutomation.Models;

// Интерфейс для формирования текстового отчета по результатам тестов.

public interface IReportPrinter
{
    string Build(BenchmarkResult cpu, BenchmarkResult gpu, BenchmarkProfile cpuProfile, BenchmarkProfile gpuProfile);
}
