using Microsoft.Extensions.Logging;
using TraincrewDepMelody.Application.Audio;
using TraincrewDepMelody.Infrastructure.Repositories;
using TraincrewDepMelody.Models;

namespace TraincrewDepMelody.Application.Modes;

/// <summary>
/// 駅モード
/// </summary>
public class StationMode : IMode
{
    #region フィールド
    private readonly AudioPlayer _audioPlayer;
    private readonly AudioRepository _audioRepository;
    private readonly ApplicationState _state;
    private readonly ILogger<StationMode> _logger;

    private bool _isFirstPress = true;
    private PlaybackState _playbackState = PlaybackState.Idle;
    #endregion

    #region コンストラクタ
    public StationMode(
        AudioPlayer audioPlayer,
        AudioRepository audioRepository,
        ApplicationState state,
        ILogger<StationMode> logger)
    {
        _audioPlayer = audioPlayer;
        _audioRepository = audioRepository;
        _state = state;
        _logger = logger;

        // 再生終了イベント登録
        _audioPlayer.PlaybackFinished += OnPlaybackFinished;
    }
    #endregion

    #region IMode実装
    public void OnEnter()
    {
        _logger.LogInformation("Enter StationMode");
        _isFirstPress = true;
        _playbackState = PlaybackState.Idle;
    }

    public void OnExit()
    {
        _logger.LogInformation("Exit StationMode");
        _audioPlayer.StopAll();
        _playbackState = PlaybackState.Idle;
    }

    public void OnButtonPressed()
    {
        if (_isFirstPress)
        {
            _logger.LogInformation("StationMode: First button press");
            _isFirstPress = false;

            // 駅メロディー再生
            PlayStationMelody();
        }
        else
        {
            // 2回目以降の押下は車両モードの動作
            _logger.LogInformation("StationMode: Additional button press (vehicle mode behavior)");

            // 車両チャンネルのアナウンス再生中なら停止
            if (_playbackState == PlaybackState.PlayingAnnouncement)
            {
                _audioPlayer.Stop("vehicle");
            }

            // 車両メロディーをループ再生
            PlayVehicleMelody();
        }
    }

    public void OnButtonReleased()
    {
        // 駅モードではリリースを無視 (初回のみ)
        // 2回目以降は車両モードの動作
        if (!_isFirstPress && _playbackState == PlaybackState.PlayingMelodyLoop)
        {
            _audioPlayer.Stop("vehicle");
            PlayVehicleDoorClosing();
        }
    }

    public void Update()
    {
        // 特になし
    }
    #endregion

    #region プライベートメソッド
    /// <summary>
    /// 駅メロディー再生
    /// </summary>
    private void PlayStationMelody()
    {
        var station = _state.CurrentStation;
        if (station == null)
        {
            _logger.LogWarning("Station info not available");
            return;
        }

        var melodyPath = _audioRepository.GetStationMelody(station.StationName, station.Platform, _state.Direction);

        _logger.LogInformation($"Playing station melody: {melodyPath}");
        _audioPlayer.Play("station", melodyPath, loop: false);
        _playbackState = PlaybackState.PlayingMelody;
    }

    /// <summary>
    /// 駅ドア締まりますアナウンス再生
    /// </summary>
    private void PlayStationDoorClosing()
    {
        var station = _state.CurrentStation;
        if (station == null)
        {
            _logger.LogWarning("Station info not available");
            return;
        }

        var announcementPath = _audioRepository.GetStationDoorClosing(station.IsOddPlatform);

        _logger.LogInformation($"Playing station door closing: {announcementPath}");
        _audioPlayer.Play("station", announcementPath, loop: false);
        _playbackState = PlaybackState.PlayingAnnouncement;
    }

    /// <summary>
    /// 車両メロディー再生
    /// </summary>
    private void PlayVehicleMelody()
    {
        var melodyPath = _audioRepository.GetVehicleMelody(_state.Direction);

        _logger.LogInformation($"Playing vehicle melody: {melodyPath}");
        _audioPlayer.Play("vehicle", melodyPath, loop: true);
        _playbackState = PlaybackState.PlayingMelodyLoop;
    }

    /// <summary>
    /// 車両ドア締まりますアナウンス再生
    /// </summary>
    private void PlayVehicleDoorClosing()
    {
        var announcementPath = _audioRepository.GetVehicleDoorClosing();

        _logger.LogInformation($"Playing vehicle door closing: {announcementPath}");
        _audioPlayer.Play("vehicle", announcementPath, loop: false);
        _playbackState = PlaybackState.PlayingAnnouncement;
    }

    /// <summary>
    /// 再生終了イベント
    /// </summary>
    private void OnPlaybackFinished(object? sender, EventArgs e)
    {
        if (_playbackState == PlaybackState.PlayingMelody)
        {
            // メロディー終了 → ドア締まりますへ
            PlayStationDoorClosing();
        }
        else if (_playbackState == PlaybackState.PlayingAnnouncement)
        {
            // アナウンス終了 → 待機
            _playbackState = PlaybackState.Idle;
        }
    }
    #endregion

    #region 内部列挙型
    private enum PlaybackState
    {
        Idle,
        PlayingMelody,
        PlayingMelodyLoop,
        PlayingAnnouncement
    }
    #endregion
}