using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
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

    public MainWindow()
    {
        InitializeComponent();

        _viewModel = new MainViewModel();
        DataContext = _viewModel;

        // タイマー初期化 (20ms間隔で状態更新)
        _updateTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(20)
        };
        _updateTimer.Tick += (s, e) => _viewModel.Update();
        _updateTimer.Start();

        // キーボード入力
        KeyDown += OnKeyDown;
        KeyUp += OnKeyUp;

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
    /// キーダウン
    /// </summary>
    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Space && !e.IsRepeat)
        {
            _viewModel.OnButtonPressed();
        }
    }

    /// <summary>
    /// キーアップ
    /// </summary>
    private void OnKeyUp(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Space)
        {
            _viewModel.OnButtonReleased();
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