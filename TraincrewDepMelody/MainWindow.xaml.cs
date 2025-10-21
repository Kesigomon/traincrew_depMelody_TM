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
    private Key _configuredKey = Key.End; // デフォルトはEnd
    private bool _isSettingsWindowOpen = false;

    public MainWindow()
    {
        InitializeComponent();

        _viewModel = new MainViewModel();
        DataContext = _viewModel;

        // 設定ファイルからキーを読み込み
        LoadInputKeyFromSettings();

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
        // 設定ウィンドウが開いている間は無効
        if (_isSettingsWindowOpen)
        {
            return;
        }

        // キーボード入力が無効化されている場合は無効
        if (!_viewModel.Settings.EnableKeyboard)
        {
            return;
        }

        if (e.Key == _configuredKey && !e.IsRepeat)
        {
            Dispatcher.Invoke(() => _viewModel.OnButtonPressed());
        }
    }

    /// <summary>
    /// グローバルキーアップ（ウィンドウにフォーカスがなくても検知）
    /// </summary>
    private void OnGlobalKeyUp(object? sender, GlobalKeyEventArgs e)
    {
        // 設定ウィンドウが開いている間は無効
        if (_isSettingsWindowOpen)
        {
            return;
        }

        // キーボード入力が無効化されている場合は無効
        if (!_viewModel.Settings.EnableKeyboard)
        {
            return;
        }

        if (e.Key == _configuredKey)
        {
            Dispatcher.Invoke(() => _viewModel.OnButtonReleased());
        }
    }

    /// <summary>
    /// 設定ファイルからInputKeyを読み込む
    /// </summary>
    private void LoadInputKeyFromSettings()
    {
        try
        {
            var keyString = _viewModel.Settings.InputKey;
            _configuredKey = ParseKeyFromString(keyString);
        }
        catch
        {
            // パースに失敗した場合はデフォルト値を使用
            _configuredKey = Key.End;
        }
    }

    /// <summary>
    /// 文字列からKeyを変換
    /// </summary>
    private Key ParseKeyFromString(string keyString)
    {
        if (string.IsNullOrWhiteSpace(keyString))
        {
            return Key.End;
        }

        // Enum.TryParseを使ってキー名を変換
        if (Enum.TryParse<Key>(keyString, true, out var key))
        {
            return key;
        }

        // パースに失敗した場合はデフォルト
        return Key.End;
    }

    /// <summary>
    /// 入力キーを更新（設定変更時に呼ばれる）
    /// </summary>
    public void UpdateInputKey(string keyString)
    {
        _configuredKey = ParseKeyFromString(keyString);
    }

    /// <summary>
    /// 設定画面表示
    /// </summary>
    private void OnSettingsClick(object sender, RoutedEventArgs e)
    {
        // 設定ウィンドウが開いている間はキー操作を無効化
        _isSettingsWindowOpen = true;

        var settingsWindow = new SettingsWindow(_viewModel.Settings)
        {
            Owner = this
        };

        settingsWindow.Closed += (s, args) =>
        {
            // 設定ウィンドウが閉じたらキー操作を有効化
            _isSettingsWindowOpen = false;
        };

        settingsWindow.ShowDialog();

        if (settingsWindow.IsOkClicked)
        {
            _viewModel.ApplySettings(settingsWindow.Settings);

            // 入力キーを更新
            UpdateInputKey(settingsWindow.Settings.InputKey);
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