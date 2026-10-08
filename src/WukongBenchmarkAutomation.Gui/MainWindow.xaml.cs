using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using System.Windows.Threading;
using WukongBenchmarkAutomation.Gui.ViewModels;

namespace WukongBenchmarkAutomation.Gui;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;

    public MainWindow()
    {
        InitializeComponent();

        _viewModel = new MainViewModel();
        DataContext = _viewModel;

        // Автоматическая прокрутка списка логов при добавлении новых строк
        ((INotifyCollectionChanged)_viewModel.LogMessages).CollectionChanged += (s, e) =>
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                Dispatcher.BeginInvoke(DispatcherPriority.Background, () =>
                {
                    if (LogListBox.Items.Count > 0)
                    {
                        LogListBox.ScrollIntoView(LogListBox.Items[^1]);
                    }
                });
            }
        };

        Loaded += MainWindow_Loaded;
        Closing += MainWindow_Closing;
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        // Автоматический старт при открытии окна
        await _viewModel.StartPipelineAsync();
    }

    private void MainWindow_Closing(object? sender, CancelEventArgs e)
    {
        if (_viewModel.IsRunning)
        {
            var result = MessageBox.Show(
                "Тестирование всё ещё выполняется. Вы уверены, что хотите выйти?\n\n(Исходные настройки игры будут восстановлены)",
                "Подтверждение",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.No)
            {
                e.Cancel = true;
                return;
            }

            _viewModel.EnsureCleanupOnExit();
        }
    }

    private void ExitButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}