using System.ComponentModel;
using System.Windows;
using Microsoft.Extensions.Logging;
using TraincrewDepMelody.Application.Audio;
using TraincrewDepMelody.Application.Modes;
using TraincrewDepMelody.Application.UI;
using TraincrewDepMelody.Infrastructure.Api;
using TraincrewDepMelody.Infrastructure.Logging;
using TraincrewDepMelody.Infrastructure.Repositories;
using TraincrewDepMelody.Infrastructure.Settings;
using TraincrewDepMelody.Models;

namespace TraincrewDepMelody.Presentation.ViewModels;

/// <summary>
/// メインビューモデル
/// </summary>
public class MainViewModel : INotifyPropertyChanged
{
    #region フィールド
    private readonly ModeManager? _modeManager;
    private readonly ILoggerFactory _loggerFactory;
    private readonly AudioPlayer? _audioPlayer;
    private readonly AudioRepository? _audioRepository;
    private readonly SettingsManager? _settingsManager;
    private readonly TopmostController? _topmostController;
    #endregion

    #region プロパティ
    public ApplicationState ApplicationState { get; }
    public AppSettings Settings => _settingsManager?.Settings ?? new AppSettings();
    #endregion

    #region コンストラクタ
    public MainViewModel()
    {
        ApplicationState = new ApplicationState();

        // LoggerFactory作成
        _loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddProvider(new FileLoggerProvider());
            builder.SetMinimumLevel(LogLevel.Information);
        });

        try
        {
            // 設定読み込み
            _settingsManager = new SettingsManager();
            _settingsManager.Load();

            // 依存関係注入
            _audioPlayer = new AudioPlayer(_loggerFactory.CreateLogger<AudioPlayer>());
            _audioRepository = new AudioRepository(_loggerFactory);
            var api = new TraincrewApi();
            var apiClient = new TraincrewApiClient(api, _loggerFactory.CreateLogger<TraincrewApiClient>());
            var stationRepository = new StationRepository(_loggerFactory.CreateLogger<StationRepository>());

            // 初期化
            apiClient.Connect();
            _audioRepository.LoadProfile(_settingsManager.Settings.CurrentProfile);
            stationRepository.LoadFromCsv(_settingsManager.Settings.StationDefinition);
            _audioPlayer.SetVolume(_settingsManager.Settings.Volume);

            _modeManager = new ModeManager(
                _audioPlayer,
                _audioRepository,
                apiClient,
                stationRepository,
                ApplicationState,
                _loggerFactory
            );

            // TopmostController初期化
            _topmostController = new TopmostController(System.Windows.Application.Current.MainWindow);
            _topmostController.SetMode(_settingsManager.Settings.Topmost);

            var logger = _loggerFactory.CreateLogger<MainViewModel>();
            logger.LogInformation("MainViewModel initialized successfully");
        }
        catch (Exception ex)
        {
            var logger = _loggerFactory.CreateLogger<MainViewModel>();
            logger.LogError(ex, "Failed to initialize MainViewModel");
            MessageBox.Show($"初期化に失敗しました: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    #endregion

    #region パブリックメソッド
    /// <summary>
    /// ボタン押下
    /// </summary>
    public void OnButtonPressed()
    {
        if (_modeManager == null) return;

        var logger = _loggerFactory.CreateLogger<MainViewModel>();
        logger.LogInformation("Button pressed");
        _modeManager.OnButtonPressed();
    }

    /// <summary>
    /// ボタンリリース
    /// </summary>
    public void OnButtonReleased()
    {
        if (_modeManager == null) return;

        var logger = _loggerFactory.CreateLogger<MainViewModel>();
        logger.LogInformation("Button released");
        _modeManager.OnButtonReleased();
    }

    /// <summary>
    /// 状態更新 (100ms間隔で呼ばれる)
    /// </summary>
    public void Update()
    {
        _modeManager?.Update();
        _topmostController?.Update(ApplicationState);
        OnPropertyChanged(nameof(ApplicationState));
    }

    /// <summary>
    /// 設定を適用
    /// </summary>
    public void ApplySettings(AppSettings newSettings)
    {
        if (_settingsManager == null || _audioPlayer == null || _audioRepository == null) return;

        var logger = _loggerFactory.CreateLogger<MainViewModel>();

        try
        {
            // 音量変更
            if (Math.Abs(newSettings.Volume - _settingsManager.Settings.Volume) > 0.01)
            {
                _audioPlayer.SetVolume(newSettings.Volume);
                logger.LogInformation($"Volume changed to {newSettings.Volume:F2}");
            }

            // プロファイル変更
            if (newSettings.ProfileFile != _settingsManager.Settings.ProfileFile)
            {
                var profilePath = System.IO.Path.Combine("profiles", newSettings.ProfileFile);
                _audioRepository.LoadProfile(profilePath);
                logger.LogInformation($"Profile changed to {newSettings.ProfileFile}");
            }

            // 設定プロパティをコピー
            _settingsManager.Settings.Volume = newSettings.Volume;
            _settingsManager.Settings.ProfileFile = newSettings.ProfileFile;
            _settingsManager.Settings.Topmost = newSettings.Topmost;
            _settingsManager.Settings.EnableKeyboard = newSettings.EnableKeyboard;
            _settingsManager.Settings.InputKey = newSettings.InputKey;

            // Topmost設定を適用
            _topmostController?.SetMode(newSettings.Topmost);

            // 設定を保存
            _settingsManager.Save();

            logger.LogInformation("Settings applied successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to apply settings");
            MessageBox.Show($"設定の適用に失敗しました: {ex.Message}", "エラー",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    #endregion

    #region INotifyPropertyChanged
    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged(string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
    #endregion
}