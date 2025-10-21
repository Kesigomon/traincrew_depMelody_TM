using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Input;
using TraincrewDepMelody.Models;

namespace TraincrewDepMelody.Presentation.Views;

/// <summary>
/// SettingsWindow.xaml の相互作用ロジック
/// </summary>
public partial class SettingsWindow : Window
{
    private AppSettings _settings;
    private readonly string _profileDirectory = "profiles";
    private bool _isCapturingKey;
    private string _capturedKey = "End";

    public AppSettings Settings => _settings;
    public bool IsOkClicked { get; private set; }

    public SettingsWindow(AppSettings settings)
    {
        InitializeComponent();

        _settings = settings;

        LoadSettings();
        LoadProfiles();
        LoadRecentLogs();

        // キーボードイベントを登録
        PreviewKeyDown += OnWindowKeyDown;
    }

    /// <summary>
    /// 設定を読み込み
    /// </summary>
    private void LoadSettings()
    {
        VolumeSlider.Value = _settings.Volume * 100;
        EnableKeyboardCheckBox.IsChecked = _settings.EnableKeyboard;

        // Topmost設定
        var topmostValue = _settings.Topmost switch
        {
            TopmostMode.Always => "Always",
            TopmostMode.PlayingOnly => "PlayingOnly",
            TopmostMode.AtStationOnly => "AtStationOnly",
            TopmostMode.None => "None",
            _ => "Always"
        };

        foreach (var item in TopmostComboBox.Items)
        {
            if (item is System.Windows.Controls.ComboBoxItem comboItem &&
                comboItem.Tag?.ToString() == topmostValue)
            {
                TopmostComboBox.SelectedItem = item;
                break;
            }
        }

        // InputKey設定
        _capturedKey = string.IsNullOrWhiteSpace(_settings.InputKey) ? "End" : _settings.InputKey;
        InputKeyTextBox.Text = _capturedKey;
    }

    /// <summary>
    /// プロファイル一覧を読み込み
    /// </summary>
    private void LoadProfiles()
    {
        ProfileComboBox.Items.Clear();

        if (!Directory.Exists(_profileDirectory))
        {
            Directory.CreateDirectory(_profileDirectory);
        }

        var csvFiles = Directory.GetFiles(_profileDirectory, "*.csv");

        foreach (var file in csvFiles)
        {
            var fileName = Path.GetFileName(file);
            ProfileComboBox.Items.Add(fileName);

            if (fileName == _settings.ProfileFile)
            {
                ProfileComboBox.SelectedItem = fileName;
            }
        }

        if (ProfileComboBox.SelectedItem == null && ProfileComboBox.Items.Count > 0)
        {
            ProfileComboBox.SelectedIndex = 0;
        }
    }

    /// <summary>
    /// 最新のログを読み込み
    /// </summary>
    private void LoadRecentLogs()
    {
        try
        {
            var logDirectory = "log";
            if (!Directory.Exists(logDirectory))
            {
                LogTextBlock.Text = "ログファイルが見つかりません";
                return;
            }

            var logFiles = Directory.GetFiles(logDirectory, "*.log")
                .OrderByDescending(f => File.GetLastWriteTime(f))
                .ToList();

            if (logFiles.Count == 0)
            {
                LogTextBlock.Text = "ログファイルが見つかりません";
                return;
            }

            // 最新のログファイルの末尾100行を読み込み
            var latestLog = logFiles[0];
            var lines = File.ReadLines(latestLog).TakeLast(100).ToList();
            LogTextBlock.Text = string.Join(Environment.NewLine, lines);
        }
        catch (Exception ex)
        {
            LogTextBlock.Text = $"ログ読み込みエラー: {ex.Message}";
        }
    }

    /// <summary>
    /// 音量変更イベント
    /// </summary>
    private void OnVolumeChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (VolumeLabel != null)
        {
            VolumeLabel.Text = $"{(int)e.NewValue}%";
        }
    }

    /// <summary>
    /// プロファイル再読み込み
    /// </summary>
    private void OnReloadProfileClick(object sender, RoutedEventArgs e)
    {
        LoadProfiles();
        MessageBox.Show("プロファイルを再読み込みしました", "情報",
            MessageBoxButton.OK, MessageBoxImage.Information);
    }

    /// <summary>
    /// プロファイルフォルダを開く
    /// </summary>
    private void OnOpenProfileFolderClick(object sender, RoutedEventArgs e)
    {
        if (!Directory.Exists(_profileDirectory))
        {
            Directory.CreateDirectory(_profileDirectory);
        }

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = Path.GetFullPath(_profileDirectory),
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"フォルダを開けませんでした: {ex.Message}", "エラー",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// OKボタンクリック
    /// </summary>
    private void OnOkClick(object sender, RoutedEventArgs e)
    {
        // 設定を保存
        _settings.Volume = VolumeSlider.Value / 100.0;
        _settings.EnableKeyboard = EnableKeyboardCheckBox.IsChecked ?? true;

        if (ProfileComboBox.SelectedItem is string selectedProfile)
        {
            _settings.ProfileFile = selectedProfile;
        }

        // Topmost設定
        if (TopmostComboBox.SelectedItem is System.Windows.Controls.ComboBoxItem selectedItem)
        {
            _settings.Topmost = selectedItem.Tag?.ToString() switch
            {
                "Always" => TopmostMode.Always,
                "PlayingOnly" => TopmostMode.PlayingOnly,
                "AtStationOnly" => TopmostMode.AtStationOnly,
                "None" => TopmostMode.None,
                _ => TopmostMode.Always
            };
        }

        // InputKey設定
        _settings.InputKey = _capturedKey;

        IsOkClicked = true;
        Close();
    }

    /// <summary>
    /// キャンセルボタンクリック
    /// </summary>
    private void OnCancelClick(object sender, RoutedEventArgs e)
    {
        IsOkClicked = false;
        Close();
    }

    /// <summary>
    /// キーキャプチャボタンクリック
    /// </summary>
    private void OnCaptureKeyClick(object sender, RoutedEventArgs e)
    {
        _isCapturingKey = true;
        CaptureKeyButton.Content = "キーを押してください...";
        CaptureKeyButton.IsEnabled = false;
        InputKeyTextBox.Text = "キーを押してください...";
        InputKeyTextBox.Background = System.Windows.Media.Brushes.LightYellow;

        // フォーカスをTextBoxに移動（キー入力を受け取るため）
        InputKeyTextBox.Focus();
    }

    /// <summary>
    /// ウィンドウのキーダウンイベント
    /// </summary>
    private void OnWindowKeyDown(object sender, KeyEventArgs e)
    {
        if (!_isCapturingKey)
        {
            return;
        }

        // キャプチャを無効化するキー（Escapeでキャンセル）
        if (e.Key == Key.Escape)
        {
            _isCapturingKey = false;
            CaptureKeyButton.Content = "キーを設定";
            CaptureKeyButton.IsEnabled = true;
            InputKeyTextBox.Text = _capturedKey;
            InputKeyTextBox.Background = System.Windows.Media.Brushes.WhiteSmoke;
            e.Handled = true;
            return;
        }

        // システムキー（Alt, Ctrl, Shift, Windows）は単独では登録しない
        if (e.Key is 
            Key.LeftAlt or Key.RightAlt or Key.LeftCtrl or Key.RightCtrl or Key.LeftShift or Key.RightShift or Key.LWin 
            or Key.RWin or Key.System)
        {
            e.Handled = true;
            return;
        }

        // キーを取得
        var key = e.Key == Key.System ? e.SystemKey : e.Key;

        // キーを保存
        _capturedKey = key.ToString();
        InputKeyTextBox.Text = _capturedKey;
        InputKeyTextBox.Background = System.Windows.Media.Brushes.WhiteSmoke;

        // キャプチャモードを終了
        _isCapturingKey = false;
        CaptureKeyButton.Content = "キーを設定";
        CaptureKeyButton.IsEnabled = true;

        e.Handled = true;
    }
}