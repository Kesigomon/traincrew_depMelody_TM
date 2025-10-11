using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using TraincrewDepMelody.Application.Audio;
using TraincrewDepMelody.Infrastructure.Api;
using TraincrewDepMelody.Infrastructure.Repositories;
using TraincrewDepMelody.Models;

namespace TraincrewDepMelody.Application.Modes;

/// <summary>
/// モード管理クラス
/// </summary>
public class ModeManager
{
    #region フィールド
    private IMode _currentMode;
    private readonly StationMode _stationMode;
    private readonly VehicleMode _vehicleMode;

    private readonly IAudioPlayer _audioPlayer;
    private readonly IAudioRepository _audioRepository;
    private readonly TraincrewApiClient _apiClient;
    private readonly IStationRepository _stationRepository;
    private readonly ApplicationState _state;
    private readonly ILogger<ModeManager> _logger;
    private readonly ILoggerFactory _loggerFactory;

    private StationInfo? _previousStation;
    private bool _isFirstPressInStationMode = true;
    #endregion

    #region プロパティ
    public IMode CurrentMode => _currentMode;
    #endregion

    #region コンストラクタ
    public ModeManager(
        IAudioPlayer audioPlayer,
        IAudioRepository audioRepository,
        TraincrewApiClient apiClient,
        IStationRepository stationRepository,
        ApplicationState state,
        ILoggerFactory loggerFactory)
    {
        _audioPlayer = audioPlayer;
        _audioRepository = audioRepository;
        _apiClient = apiClient;
        _stationRepository = stationRepository;
        _state = state;
        _loggerFactory = loggerFactory;
        _logger = loggerFactory.CreateLogger<ModeManager>();

        // モード初期化
        _stationMode = new StationMode(
            _audioPlayer,
            _audioRepository,
            _state,
            _loggerFactory.CreateLogger<StationMode>(),
            OnStationMelodyNotFound);
        _vehicleMode = new VehicleMode(_audioPlayer, _audioRepository, _state, _loggerFactory.CreateLogger<VehicleMode>());

        // 初期モードは車両モード
        _currentMode = _vehicleMode;
        _state.CurrentMode = ModeType.Vehicle;
    }
    #endregion

    #region パブリックメソッド
    /// <summary>
    /// ボタン押下イベント
    /// </summary>
    public void OnButtonPressed()
    {
        // 駅モード切替判定
        if (_currentMode is VehicleMode && _state.IsAtStation)
        {
            _logger.LogInformation("At station, switching to StationMode");
            SwitchMode(_stationMode);
            _isFirstPressInStationMode = true;
        }
        // 駅モード中の2回目以降のボタン押下は車両モードに切り替え
        else if (_currentMode is StationMode)
        {
            if (_isFirstPressInStationMode)
            {
                _logger.LogInformation("StationMode: First button press");
                _isFirstPressInStationMode = false;
            }
            else
            {
                _logger.LogInformation("StationMode: Second button press, switching to VehicleMode");
                SwitchMode(_vehicleMode);
            }
        }

        _currentMode.OnButtonPressed();
    }

    /// <summary>
    /// ボタンリリースイベント
    /// </summary>
    public void OnButtonReleased()
    {
        _currentMode.OnButtonReleased();
    }

    /// <summary>
    /// 状態更新
    /// </summary>
    public async void Update()
    {
        // API情報取得
        try
        {
            await _apiClient.FetchData();

            _state.GameStatus = _apiClient.GetGameStatus();
            _state.OccupiedTracks = _apiClient.GetTrackCircuits();
            _state.TrainNumber = _apiClient.GetTrainNumber();
            _state.Direction = DetermineDirection(_state.TrainNumber);

            // 駅判定
            var currentStation = _stationRepository.FindStation(_state.OccupiedTracks);
            _state.CurrentStation = currentStation;
            _state.IsAtStation = currentStation != null;

            // 駅到着/発車検知
            DetectStationChange(currentStation);

            // ゲーム状態による音声制御
            HandleGameStatus(_state.GameStatus);

            // モード更新
            _currentMode.Update();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update state");
        }
    }

    /// <summary>
    /// モード切替
    /// </summary>
    public void SwitchMode(IMode newMode)
    {
        _logger.LogInformation($"Mode switch: {_currentMode.GetType().Name} -> {newMode.GetType().Name}");

        _currentMode.OnExit();
        _currentMode = newMode;
        _currentMode.OnEnter();

        _state.CurrentMode = newMode is StationMode ? ModeType.Station : ModeType.Vehicle;
    }
    #endregion

    #region プライベートメソッド
    /// <summary>
    /// 駅メロディが見つからなかった場合の処理
    /// </summary>
    private void OnStationMelodyNotFound()
    {
        _logger.LogInformation("Station melody not found, switching to VehicleMode");
        SwitchMode(_vehicleMode);
        _isFirstPressInStationMode = true; // フラグをリセット
        // 車両モードで再生を開始
        _vehicleMode.OnButtonPressed();
    }

    /// <summary>
    /// 上下線判定
    /// </summary>
    private Direction DetermineDirection(string trainNumber)
    {
        if (string.IsNullOrEmpty(trainNumber))
        {
            return Direction.Up;
        }

        // 数字部分を抽出
        var match = Regex.Match(trainNumber, @"\d+");
        if (match.Success)
        {
            var lastDigit = int.Parse(match.Value[^1].ToString());
            return lastDigit % 2 == 0 ? Direction.Up : Direction.Down;
        }

        return Direction.Up;
    }

    /// <summary>
    /// 駅到着/発車検知
    /// </summary>
    private void DetectStationChange(StationInfo? currentStation)
    {
        // 駅到着
        if (currentStation != null && _previousStation == null)
        {
            _logger.LogInformation($"Arrived at {currentStation.StationName} platform {currentStation.Platform}");
        }
        // 駅発車
        else if (currentStation == null && _previousStation != null)
        {
            _logger.LogInformation($"Departed from {_previousStation.StationName}");

            // 駅モード中なら車両モードに切替
            if (_currentMode is StationMode)
            {
                SwitchMode(_vehicleMode);
                _isFirstPressInStationMode = true; // フラグをリセット
            }
        }

        _previousStation = currentStation;
    }

    /// <summary>
    /// ゲーム状態による音声制御
    /// </summary>
    private void HandleGameStatus(GameStatus status)
    {
        switch (status)
        {
            case GameStatus.Running:
                if (_audioPlayer.IsPaused)
                {
                    _audioPlayer.Resume();
                }
                break;

            case GameStatus.Paused:
                if (_audioPlayer.IsPlaying)
                {
                    _audioPlayer.Pause();
                }
                break;

            case GameStatus.Stopped:
                _audioPlayer.StopAll();

                // 駅モード中なら車両モードに切替
                if (_currentMode is StationMode)
                {
                    SwitchMode(_vehicleMode);
                    _isFirstPressInStationMode = true; // フラグをリセット
                }
                break;
        }
    }
    #endregion
}