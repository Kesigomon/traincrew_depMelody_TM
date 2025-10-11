using Microsoft.Extensions.Logging;
using TraincrewDepMelody.Application.Audio;
using TraincrewDepMelody.Infrastructure.Repositories;
using TraincrewDepMelody.Models;

namespace TraincrewDepMelody.Application.Modes
{
    /// <summary>
    /// 車両モード
    /// </summary>
    public class VehicleMode : IMode
    {
        #region フィールド
        private readonly AudioPlayer _audioPlayer;
        private readonly AudioRepository _audioRepository;
        private readonly ApplicationState _state;
        private readonly ILogger<VehicleMode> _logger;

        private PlaybackState _playbackState = PlaybackState.Idle;
        #endregion

        #region コンストラクタ
        public VehicleMode(
            AudioPlayer audioPlayer,
            AudioRepository audioRepository,
            ApplicationState state,
            ILogger<VehicleMode> logger)
        {
            _audioPlayer = audioPlayer;
            _audioRepository = audioRepository;
            _state = state;
            _logger = logger;
        }
        #endregion

        #region IMode実装
        public void OnEnter()
        {
            _logger.LogInformation("Enter VehicleMode");
            _playbackState = PlaybackState.Idle;
        }

        public void OnExit()
        {
            _logger.LogInformation("Exit VehicleMode");
            _audioPlayer.Stop("vehicle");
            _playbackState = PlaybackState.Idle;
        }

        public void OnButtonPressed()
        {
            _logger.LogInformation("VehicleMode: Button pressed");

            // アナウンス再生中なら即停止
            if (_playbackState == PlaybackState.PlayingAnnouncement)
            {
                _audioPlayer.Stop("vehicle");
            }

            // メロディーループ再生
            PlayMelodyLoop();
        }

        public void OnButtonReleased()
        {
            _logger.LogInformation("VehicleMode: Button released");

            if (_playbackState == PlaybackState.PlayingMelodyLoop)
            {
                _audioPlayer.Stop("vehicle");
                PlayDoorClosing();
            }
        }

        public void Update()
        {
            // アナウンス再生完了チェック
            if (_playbackState == PlaybackState.PlayingAnnouncement && !_audioPlayer.IsChannelPlaying("vehicle"))
            {
                _playbackState = PlaybackState.Idle;
            }
        }
        #endregion

        #region プライベートメソッド
        /// <summary>
        /// メロディーループ再生
        /// </summary>
        private void PlayMelodyLoop()
        {
            var melodyPath = _audioRepository.GetVehicleMelody(_state.Direction);

            _logger.LogInformation($"Playing vehicle melody loop: {melodyPath}");
            _audioPlayer.Play("vehicle", melodyPath, loop: true);
            _playbackState = PlaybackState.PlayingMelodyLoop;
        }

        /// <summary>
        /// ドア締まりますアナウンス再生
        /// </summary>
        private void PlayDoorClosing()
        {
            var announcementPath = _audioRepository.GetVehicleDoorClosing();

            _logger.LogInformation($"Playing door closing: {announcementPath}");
            _audioPlayer.Play("vehicle", announcementPath, loop: false);
            _playbackState = PlaybackState.PlayingAnnouncement;
        }
        #endregion

        #region 内部列挙型
        private enum PlaybackState
        {
            Idle,
            PlayingMelodyLoop,
            PlayingAnnouncement
        }
        #endregion
    }
}
