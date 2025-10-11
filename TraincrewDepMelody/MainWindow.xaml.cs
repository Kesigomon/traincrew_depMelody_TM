using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using TraincrewDepMelody.Infrastructure.Input;
using TraincrewDepMelody.Presentation.ViewModels;
using TraincrewDepMelody.Presentation.Views;

namespace TraincrewDepMelody;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;
    private readonly DispatcherTimer _updateTimer;
    private readonly GlobalKeyboardHook _keyboardHook;

    public MainWindow()
    {
        InitializeComponent();

        _viewModel = new MainViewModel();
        DataContext = _viewModel;

        // タイマー初期化 (100ms間隔で状態更新)
        _updateTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(100)
        };
        _updateTimer.Tick += (s, e) => _viewModel.Update();
        _updateTimer.Start();

        // グローバルキーボードフック初期化
        _keyboardHook = new GlobalKeyboardHook();
        _keyboardHook.KeyDown += OnGlobalKeyDown;
        _keyboardHook.KeyUp += OnGlobalKeyUp;
        _keyboardHook.Start();

        // ウィンドウが閉じられるときにフックを停止
        Closed += (s, e) => _keyboardHook.Dispose();

        // ウィンドウドラッグ可能
        MouseLeftButtonDown += (s, e) =>
        {
            if (e.OriginalSource is not System.Windows.Controls.MenuItem)
            {
                DragMove();
            }
        };
    }

    /// <summary>
    /// ボタンマウスダウン
    /// </summary>
    private void OnButtonMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
        {
            _viewModel.OnButtonPressed();
            e.Handled = true;
        }
    }

    /// <summary>
    /// ボタンマウスアップ
    /// </summary>
    private void OnButtonMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
        {
            _viewModel.OnButtonReleased();
            e.Handled = true;
        }
    }

    /// <summary>
    /// グローバルキーダウン（ウィンドウにフォーカスがなくても検知）
    /// </summary>
    private void OnGlobalKeyDown(object? sender, GlobalKeyEventArgs e)
    {
        if (e is { Key: Key.End, IsRepeat: false })
        {
            Dispatcher.Invoke(() => _viewModel.OnButtonPressed());
        }
    }

    /// <summary>
    /// グローバルキーアップ（ウィンドウにフォーカスがなくても検知）
    /// </summary>
    private void OnGlobalKeyUp(object? sender, GlobalKeyEventArgs e)
    {
        if (e.Key == Key.End)
        {
            Dispatcher.Invoke(() => _viewModel.OnButtonReleased());
        }
    }

    /// <summary>
    /// 設定画面表示
    /// </summary>
    private void OnSettingsClick(object sender, RoutedEventArgs e)
    {
        var settingsWindow = new SettingsWindow(_viewModel.Settings)
        {
            Owner = this
        };

        settingsWindow.ShowDialog();

        if (settingsWindow.IsOkClicked)
        {
            _viewModel.ApplySettings(settingsWindow.Settings);
        }
    }

    /// <summary>
    /// 終了
    /// </summary>
    private void OnExitClick(object sender, RoutedEventArgs e)
    {
        System.Windows.Application.Current.Shutdown();
    }
}