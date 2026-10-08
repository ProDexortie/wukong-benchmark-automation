using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using WukongBenchmarkAutomation.Models;
using WukongBenchmarkAutomation.Services;

namespace WukongBenchmarkAutomation.Gui.ViewModels;

public enum PipelineStep
{
    NotStarted = 0,
    Initializing = 1,
    CpuTest = 2,
    GpuTest = 3,
    Finalizing = 4,
    Completed = 5,
    Faulted = 6,
    Cancelled = 7
}

public class MainViewModel : INotifyPropertyChanged
{
    private readonly Dispatcher _dispatcher;
    private readonly DispatcherTimer _timer;
    private readonly Stopwatch _stopwatch = new();

    private BenchmarkRunner? _runner;
    private GameSettingsWriter? _settingsWriter;
    private CancellationTokenSource? _cts;

    private PipelineStep _currentStep = PipelineStep.NotStarted;
    private string _statusTitle = "Готов к запуску";
    private string _statusDescription = "Ожидание запуска сценария...";
    private string _elapsedFormatted = "00:00";
    private bool _isRunning;
    private bool _isCompleted;
    private bool _hasError;
    private string _errorMessage = string.Empty;

    private BenchmarkResult? _cpuResult;
    private BenchmarkResult? _gpuResult;
    private BenchmarkProfile? _cpuProfile;
    private BenchmarkProfile? _gpuProfile;
    private string _reportText = string.Empty;
    private string _reportPath = string.Empty;

    public ObservableCollection<string> LogMessages { get; } = new();
    public ObservableCollection<string> Warnings { get; } = new();

    public ICommand StartCommand { get; }
    public ICommand CancelCommand { get; }
    public ICommand OpenReportCommand { get; }
    public ICommand OpenFolderCommand { get; }
    public ICommand CopyReportCommand { get; }
    public ICommand RestartCommand { get; }

    public MainViewModel()
    {
        _dispatcher = Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;

        _timer = new DispatcherTimer(DispatcherPriority.Normal, _dispatcher)
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _timer.Tick += (s, e) =>
        {
            if (_isRunning)
            {
                var elapsed = _stopwatch.Elapsed;
                ElapsedFormatted = $"{elapsed.Minutes:00}:{elapsed.Seconds:00}";
            }
        };

        StartCommand = new RelayCommand(async () => await StartPipelineAsync(), () => !IsRunning);
        CancelCommand = new RelayCommand(async () => await CancelPipelineAsync(), () => IsRunning);
        OpenReportCommand = new RelayCommand(OpenReport, () => !string.IsNullOrEmpty(ReportPath) && File.Exists(ReportPath));
        OpenFolderCommand = new RelayCommand(OpenFolder, () => !string.IsNullOrEmpty(ReportPath));
        CopyReportCommand = new RelayCommand(CopyReport, () => !string.IsNullOrEmpty(ReportText));
        RestartCommand = new RelayCommand(async () => await StartPipelineAsync(), () => !IsRunning);
    }

    #region Properties

    public PipelineStep CurrentStep
    {
        get => _currentStep;
        set => SetField(ref _currentStep, value);
    }

    public string StatusTitle
    {
        get => _statusTitle;
        set => SetField(ref _statusTitle, value);
    }

    public string StatusDescription
    {
        get => _statusDescription;
        set => SetField(ref _statusDescription, value);
    }

    public string ElapsedFormatted
    {
        get => _elapsedFormatted;
        set => SetField(ref _elapsedFormatted, value);
    }

    public bool IsRunning
    {
        get => _isRunning;
        set
        {
            if (SetField(ref _isRunning, value))
            {
                CommandManager.InvalidateRequerySuggested();
            }
        }
    }

    public bool IsCompleted
    {
        get => _isCompleted;
        set => SetField(ref _isCompleted, value);
    }

    public bool HasError
    {
        get => _hasError;
        set => SetField(ref _hasError, value);
    }

    public string ErrorMessage
    {
        get => _errorMessage;
        set => SetField(ref _errorMessage, value);
    }

    public BenchmarkResult? CpuResult
    {
        get => _cpuResult;
        set
        {
            if (SetField(ref _cpuResult, value))
            {
                OnPropertyChanged(nameof(CpuAvgFps));
                OnPropertyChanged(nameof(CpuMinFps));
                OnPropertyChanged(nameof(CpuMaxFps));
                OnPropertyChanged(nameof(Cpu95Fps));
                OnPropertyChanged(nameof(CpuVram));
                OnPropertyChanged(nameof(CpuResolution));
                OnPropertyChanged(nameof(CpuImageQuality));
                OnPropertyChanged(nameof(CpuPreset));
                OnPropertyChanged(nameof(CpuRayTracing));
                OnPropertyChanged(nameof(CpuModelName));
                OnPropertyChanged(nameof(GpuModelName));
                OnPropertyChanged(nameof(GpuDriverName));
                OnPropertyChanged(nameof(RamSizeText));
                OnPropertyChanged(nameof(OsVersionText));
            }
        }
    }

    public BenchmarkResult? GpuResult
    {
        get => _gpuResult;
        set
        {
            if (SetField(ref _gpuResult, value))
            {
                OnPropertyChanged(nameof(GpuAvgFps));
                OnPropertyChanged(nameof(GpuMinFps));
                OnPropertyChanged(nameof(GpuMaxFps));
                OnPropertyChanged(nameof(Gpu95Fps));
                OnPropertyChanged(nameof(GpuVram));
                OnPropertyChanged(nameof(GpuResolution));
                OnPropertyChanged(nameof(GpuImageQuality));
                OnPropertyChanged(nameof(GpuPreset));
                OnPropertyChanged(nameof(GpuRayTracing));
            }
        }
    }

    public string ReportText
    {
        get => _reportText;
        set => SetField(ref _reportText, value);
    }

    public string ReportPath
    {
        get => _reportPath;
        set => SetField(ref _reportPath, value);
    }

    public string CpuModelName => GpuResult?.CPUModel ?? CpuResult?.CPUModel ?? "—";
    public string GpuModelName => GpuResult?.GPUModel ?? CpuResult?.GPUModel ?? "—";
    public string GpuDriverName => GpuResult?.GpuDriverVer ?? CpuResult?.GpuDriverVer ?? "—";
    public string RamSizeText => GpuResult?.SysMem ?? CpuResult?.SysMem ?? "—";
    public string OsVersionText => (GpuResult ?? CpuResult) != null ? $"Windows {(GpuResult ?? CpuResult)!.SysVer}" : "—";
    public int ProcessorThreads => Environment.ProcessorCount;
    public string CpuAvgFps => CpuResult != null ? $"{CpuResult.FPSAvg:0}" : "—";
    public string GpuAvgFps => GpuResult != null ? $"{GpuResult.FPSAvg:0}" : "—";
    public string CpuMinFps => CpuResult != null ? $"{CpuResult.FPSMin:0}" : "—";
    public string GpuMinFps => GpuResult != null ? $"{GpuResult.FPSMin:0}" : "—";
    public string CpuMaxFps => CpuResult != null ? $"{CpuResult.FPSMax:0}" : "—";
    public string GpuMaxFps => GpuResult != null ? $"{GpuResult.FPSMax:0}" : "—";
    public string Cpu95Fps => CpuResult != null ? $"{CpuResult.FPS95:0}" : "—";
    public string Gpu95Fps => GpuResult != null ? $"{GpuResult.FPS95:0}" : "—";
    public string CpuVram => CpuResult != null ? $"{CpuResult.VideoMem:0.0} ГБ" : "—";
    public string GpuVram => GpuResult != null ? $"{GpuResult.VideoMem:0.0} ГБ" : "—";
    public string CpuResolution => CpuResult != null
        ? CpuResult.ScreenResolution.Replace("×", "x").Replace(" ", "")
        : "1280x720";
    public string GpuResolution => GpuResult != null
        ? GpuResult.ScreenResolution.Replace("×", "x").Replace(" ", "")
        : "—";
    public string CpuImageQuality => CpuResult != null ? $"{CpuResult.ImageQuality} px, DLSS 25%" : "—";
    public string GpuImageQuality => GpuResult != null ? $"{GpuResult.ImageQuality} px, DLSS 100%" : "—";
    public string CpuPreset => "Пользовательское";
    public string GpuPreset => "Реалистичное";
    public string CpuRayTracing => "Выкл";
    public string GpuRayTracing => GpuResult != null ? (GpuResult.Rtx > 0 ? $"Вкл, уровень {GpuResult.Rtx}" : "Выкл") : "—";

    #endregion

    #region Pipeline Logic

    public async Task StartPipelineAsync()
    {
        if (IsRunning) return;

        IsRunning = true;
        IsCompleted = false;
        HasError = false;
        ErrorMessage = string.Empty;
        CurrentStep = PipelineStep.Initializing;
        StatusTitle = "Инициализация...";
        StatusDescription = "Поиск установленного Steam и Benchmark Tool...";
        ElapsedFormatted = "00:00";
        _stopwatch.Restart();
        _timer.Start();

        LogMessages.Clear();
        Warnings.Clear();
        CpuResult = null;
        GpuResult = null;
        _cts = new CancellationTokenSource();

        AddLog("Запуск тестирования...");

        try
        {
            await Task.Run(() => ExecutePipeline(_cts.Token));

            // Проверка: завершились штатно или была отмена
            if (_cts.IsCancellationRequested)
            {
                CurrentStep = PipelineStep.Cancelled;
                StatusTitle = "Тестирование прервано";
                StatusDescription = "Тестирование отменено. Исходные настройки восстановлены.";
                AddLog("Тестирование отменено пользователем.");
                return;
            }

            CurrentStep = PipelineStep.Completed;
            StatusTitle = "Тестирование завершено";
            StatusDescription = $"Отчёт готов. Прошло времени: {ElapsedFormatted}.";
            IsCompleted = true;
            AddLog("Тестирование завершено. Исходные настройки игры восстановлены.");
        }
        catch (OperationCanceledException)
        {
            CurrentStep = PipelineStep.Cancelled;
            StatusTitle = "Тестирование прервано";
            StatusDescription = "Тестирование отменено. Исходные настройки восстановлены.";
            AddLog("Тестирование отменено пользователем.");
        }
        catch (Exception ex)
        {
            CurrentStep = PipelineStep.Faulted;
            HasError = true;
            ErrorMessage = ex.Message;
            StatusTitle = "Произошла ошибка";
            StatusDescription = ex.Message;
            AddLog($"Ошибка: {ex.Message}");
        }
        finally
        {
            _stopwatch.Stop();
            _timer.Stop();
            IsRunning = false;
        }
    }

    private void ExecutePipeline(CancellationToken token)
    {
        if (token.IsCancellationRequested) return;

        // Поиск Steam и бенчмарка
        SetStage(PipelineStep.Initializing, "Поиск Steam и игры", "Чтение реестра Windows и файлов конфигурации Steam...");
        AddLog("Определение пути к клиенту Steam...");
        var steamExe = SteamLocator.FindSteamExe();
        AddLog($"Steam найден: {steamExe}");

        AddLog("Поиск каталога Black Myth: Wukong Benchmark Tool...");
        var gameDir = SteamLocator.FindGameDir(steamExe);
        AddLog($"Каталог бенчмарка: {gameDir}");

        _runner = new BenchmarkRunner(steamExe, gameDir);
        if (_runner.IsGameRunning())
            throw new InvalidOperationException("Бенчмарк уже запущен. Закройте его перед запуском утилиты.");

        _settingsWriter = new GameSettingsWriter(gameDir);
        AddLog("Создание резервной копии оригинального GameUserSettings.ini...");
        _settingsWriter.Backup();

        var (nativeWidth, nativeHeight) = DisplayHelper.GetNativeResolution();
        AddLog($"Определено разрешение монитора: {nativeWidth}x{nativeHeight}");

        _cpuProfile = BenchmarkProfile.CreateCpuProfile(1280, 720, nativeWidth, nativeHeight);

        try
        {
            // CPU-тест
            if (token.IsCancellationRequested) return;
            SetStage(PipelineStep.CpuTest, "Выполняется CPU-тест", "Разрешение 1280x720, DLSS 25%, максимальная растительность и дальность");
            AddLog("Применение настроек CPU-теста...");
            _settingsWriter.Apply(_cpuProfile);

            AddLog("Запуск CPU-теста через Steam...");
            var cpu = RunWithCancellation(token);
            if (cpu == null || token.IsCancellationRequested) return;

            CpuResult = cpu;
            AddLog($"CPU-тест завершён. Средний FPS: {cpu.FPSAvg:0}.");

            // GPU-тест
            if (token.IsCancellationRequested) return;
            var hasRtx = cpu.GPUModel.Contains("RTX", StringComparison.OrdinalIgnoreCase);
            _gpuProfile = BenchmarkProfile.CreateGpuProfile(nativeWidth, nativeHeight, hasRtx);

            var rtText = hasRtx ? "Вкл, уровень 3" : "Выкл";
            SetStage(PipelineStep.GpuTest, "Выполняется GPU-тест", $"Нативное разрешение {nativeWidth}x{nativeHeight}, DLSS 100%, трассировка лучей: {rtText}");
            AddLog($"Применение настроек GPU-теста, трассировка лучей: {rtText}...");
            _settingsWriter.Apply(_gpuProfile);

            AddLog("Запуск GPU-теста через Steam...");
            var gpu = RunWithCancellation(token);
            if (gpu == null || token.IsCancellationRequested) return;

            GpuResult = gpu;
            AddLog($"GPU-тест завершён. Средний FPS: {gpu.FPSAvg:0}.");

            // Завершение и отчёт
            SetStage(PipelineStep.Finalizing, "Формирование отчёта", "Восстановление исходных файлов и сборка статистики...");
        }
        finally
        {
            AddLog("Завершение процессов игры и восстановление настроек...");
            _runner?.StopGame();
            _settingsWriter?.Restore();
        }

        if (CpuResult != null && GpuResult != null && _cpuProfile != null && _gpuProfile != null)
        {
            var report = ReportPrinter.Build(CpuResult, GpuResult, _cpuProfile, _gpuProfile);
            ReportText = report;

            var reportsDir = Path.Combine(AppContext.BaseDirectory, "reports");
            Directory.CreateDirectory(reportsDir);
            var reportFile = Path.Combine(reportsDir, $"report_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
            File.WriteAllText(reportFile, report, Encoding.UTF8);
            ReportPath = reportFile;
            AddLog($"Отчёт сохранён: {reportFile}");
        }
    }

    private BenchmarkResult? RunWithCancellation(CancellationToken token)
    {
        var runTask = Task.Run(() => _runner!.Run());

        while (!runTask.IsCompleted)
        {
            if (token.IsCancellationRequested)
            {
                _runner?.StopGame();
                try
                {
                    // Даем процессу игры до 3 секунд на закрытие
                    runTask.Wait(TimeSpan.FromSeconds(3));
                }
                catch
                {
                    // Игнорируем исключения принудительно остановленного процесса бенчмарка
                }
                return null;
            }
            Thread.Sleep(200);
        }

        try
        {
            return runTask.GetAwaiter().GetResult();
        }
        catch
        {
            if (token.IsCancellationRequested)
                return null;
            throw;
        }
    }

    public async Task CancelPipelineAsync()
    {
        if (!IsRunning || _cts == null) return;

        StatusTitle = "Отмена...";
        StatusDescription = "Остановка процессов и восстановление настроек...";
        AddLog("Пользователь запросил отмену. Завершаем работу...");
        _cts.Cancel();
        await Task.Yield();
    }

    public void EnsureCleanupOnExit()
    {
        try
        {
            _cts?.Cancel();
            _runner?.StopGame();
            _settingsWriter?.Restore();
        }
        catch
        {
            // Игнорируем ошибки при аварийном закрытии
        }
    }

    #endregion

    #region Helper Methods

    private void SetStage(PipelineStep step, string title, string desc)
    {
        _dispatcher.Invoke(() =>
        {
            CurrentStep = step;
            StatusTitle = title;
            StatusDescription = desc;
        });
    }

    private void AddLog(string message)
    {
        var time = DateTime.Now.ToString("HH:mm:ss");
        var log = $"[{time}] {message}";
        _dispatcher.Invoke(() =>
        {
            LogMessages.Add(log);
        });
    }

    private void OpenReport()
    {
        if (File.Exists(ReportPath))
        {
            Process.Start(new ProcessStartInfo(ReportPath) { UseShellExecute = true });
        }
    }

    private void OpenFolder()
    {
        if (!string.IsNullOrEmpty(ReportPath))
        {
            var dir = Path.GetDirectoryName(ReportPath);
            if (Directory.Exists(dir))
            {
                Process.Start(new ProcessStartInfo("explorer.exe", dir) { UseShellExecute = true });
            }
        }
    }

    private void CopyReport()
    {
        if (!string.IsNullOrEmpty(ReportText))
        {
            Clipboard.SetText(ReportText);
            MessageBox.Show("Текст отчёта скопирован в буфер обмена.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    #endregion

    #region INotifyPropertyChanged

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    #endregion
}
