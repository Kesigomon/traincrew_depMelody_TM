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
    private readonly Action _onStationMelodyNotFound;

    private PlaybackState _playbackState = PlaybackState.Idle;
    #endregion

    #region コンストラクタ
    public StationMode(
        AudioPlayer audioPlayer,
        AudioRepository audioRepository,
        ApplicationState state,
        ILogger<StationMode> logger,
        Action onStationMelodyNotFound)
    {
        _audioPlayer = audioPlayer;
        _audioRepository = audioRepository;
        _state = state;
        _logger = logger;
        _onStationMelodyNotFound = onStationMelodyNotFound;

        // 再生終了イベント登録
        _audioPlayer.PlaybackFinished += OnPlaybackFinished;
    }
    #endregion

    #region IMode実装
    public void OnEnter()
    {
        _logger.LogInformation("Enter StationMode");
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
        _logger.LogInformation("StationMode: Button pressed");
        // 駅メロディー再生
        PlayStationMelody();
    }

    public void OnButtonReleased()
    {
        // 駅モードではリリースを無視
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

        // 駅メロディーが見つからない場合は、ModeManagerに通知
        if (melodyPath == null)
        {
            _logger.LogInformation("Station melody not found, notifying ModeManager");
            _onStationMelodyNotFound?.Invoke();
            return;
        }

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

        // 駅アナウンスが見つからない場合は車両アナウンスを再生
        if (announcementPath == null)
        {
            _logger.LogInformation("Station door closing not found, using vehicle door closing");
            var vehicleAnnouncementPath = _audioRepository.GetVehicleDoorClosing();
            _logger.LogInformation($"Playing vehicle door closing: {vehicleAnnouncementPath}");
            _audioPlayer.Play("station", vehicleAnnouncementPath, loop: false);
            _playbackState = PlaybackState.PlayingAnnouncement;
            return;
        }

        _logger.LogInformation($"Playing station door closing: {announcementPath}");
        _audioPlayer.Play("station", announcementPath, loop: false);
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
        PlayingAnnouncement
    }
    #endregion
}