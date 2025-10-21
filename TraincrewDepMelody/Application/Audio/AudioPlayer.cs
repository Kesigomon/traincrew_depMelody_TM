using System.IO;
using System.Windows.Media;
using Microsoft.Extensions.Logging;

namespace TraincrewDepMelody.Application.Audio;

/// <summary>
/// 音声再生クラス(複数MediaPlayerインスタンスを管理)
/// </summary>
public class AudioPlayer : IAudioPlayer, IDisposable
{
    #region フィールド
    private readonly Dictionary<string, AudioChannel> _channels;
    private readonly ILogger<AudioPlayer> _logger;
    private double _volume = 0.8;
    #endregion

    #region プロパティ
    public bool IsPlaying => _channels.Values.Any(ch => ch.IsPlaying);
    public bool IsPaused => _channels.Values.Any(ch => ch.IsPaused);
    public string CurrentFile => _channels.Values.FirstOrDefault(ch => ch.IsPlaying)?.CurrentFile ?? string.Empty;
    #endregion

    #region イベント
    public event EventHandler? PlaybackFinished;
    #endregion

    #region コンストラクタ
    public AudioPlayer(ILogger<AudioPlayer> logger)
    {
        _logger = logger;
        _channels = new Dictionary<string, AudioChannel>();
    }
    #endregion

    #region パブリックメソッド
    /// <summary>
    /// 音声再生(チャンネル指定)
    /// </summary>
    public void Play(string channelId, string filePath, bool loop = false)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                _logger.LogError($"Audio file not found: {filePath}");
                return;
            }

            // チャンネルが存在しない場合は作成
            if (!_channels.ContainsKey(channelId))
            {
                _channels[channelId] = new AudioChannel(_logger, _volume);
                _channels[channelId].PlaybackFinished += (s, e) => OnChannelPlaybackFinished(channelId);
            }

            var channel = _channels[channelId];
            channel.Play(filePath, loop);

            _logger.LogInformation($"Playing on channel '{channelId}': {Path.GetFileName(filePath)} (Loop: {loop})");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to play audio on channel '{channelId}': {filePath}");
        }
    }

    /// <summary>
    /// 停止(チャンネル指定)
    /// </summary>
    public void Stop(string channelId)
    {
        if (_channels.TryGetValue(channelId, out var channel))
        {
            channel.Stop();
            _logger.LogInformation($"Stopped channel '{channelId}'");
        }
    }

    /// <summary>
    /// 全チャンネル停止
    /// </summary>
    public void StopAll()
    {
        foreach (var (channelId, channel) in _channels)
        {
            channel.Stop();
        }
        _logger.LogInformation("Stopped all channels");
    }

    /// <summary>
    /// 一時停止(全チャンネル)
    /// </summary>
    public void Pause()
    {
        foreach (var channel in _channels.Values)
        {
            channel.Pause();
        }
        _logger.LogInformation("Paused all channels");
    }

    /// <summary>
    /// 再開(全チャンネル)
    /// </summary>
    public void Resume()
    {
        foreach (var channel in _channels.Values)
        {
            channel.Resume();
        }
        _logger.LogInformation("Resumed all channels");
    }

    /// <summary>
    /// 音量設定 (0.0 ~ 1.0)
    /// </summary>
    public void SetVolume(double volume)
    {
        _volume = Math.Clamp(volume, 0.0, 1.0);

        foreach (var channel in _channels.Values)
        {
            channel.SetVolume(_volume);
        }

        _logger.LogInformation($"Volume set to {_volume:F2}");
    }

    /// <summary>
    /// チャンネルが再生中かチェック
    /// </summary>
    public bool IsChannelPlaying(string channelId)
    {
        return _channels.TryGetValue(channelId, out var channel) && channel.IsPlaying;
    }

    /// <summary>
    /// リソース解放
    /// </summary>
    public void Dispose()
    {
        foreach (var channel in _channels.Values)
        {
            channel.Dispose();
        }
        _channels.Clear();
    }
    #endregion

    #region プライベートメソッド
    /// <summary>
    /// チャンネル再生終了イベント
    /// </summary>
    private void OnChannelPlaybackFinished(string channelId)
    {
        _logger.LogInformation($"Playback finished on channel '{channelId}'");
        PlaybackFinished?.Invoke(this, EventArgs.Empty);
    }
    #endregion
}

/// <summary>
/// 音声チャンネル(個別のMediaPlayerを管理)
/// </summary>
internal class AudioChannel : IDisposable
{
    private MediaPlayer? _player;
    private readonly ILogger<AudioPlayer> _logger;
    private bool _isLooping;
    private string _currentFile = string.Empty;
    private bool _isPaused;

    public bool IsPlaying { get; private set; }
    public bool IsPaused => _isPaused;
    public string CurrentFile => _currentFile;

    public event EventHandler? PlaybackFinished;

    public AudioChannel(ILogger<AudioPlayer> logger, double volume)
    {
        _logger = logger;
        _player = new MediaPlayer();
        _player.Volume = volume;
        _player.MediaEnded += OnMediaEnded;
        _player.MediaFailed += OnMediaFailed;
    }

    public void Play(string filePath, bool loop)
    {
        if (_player == null) return;

        Stop();

        _player.Open(new(filePath, UriKind.RelativeOrAbsolute));
        _player.Play();

        _currentFile = filePath;
        _isLooping = loop;
        _isPaused = false;
        IsPlaying = true;
    }

    public void Stop()
    {
        if (_player == null) return;

        _player.Stop();
        _player.Close();
        _currentFile = string.Empty;
        _isLooping = false;
        _isPaused = false;
        IsPlaying = false;
    }

    public void Pause()
    {
        if (_player == null) return;

        if (IsPlaying && !_isPaused)
        {
            _player.Pause();
            _isPaused = true;
        }
    }

    public void Resume()
    {
        if (_player == null) return;

        if (_isPaused)
        {
            _player.Play();
            _isPaused = false;
        }
    }

    public void SetVolume(double volume)
    {
        if (_player != null)
        {
            _player.Volume = volume;
        }
    }

    public void Dispose()
    {
        Stop();
        _player?.Close();
        _player = null;
    }

    private void OnMediaEnded(object? sender, EventArgs e)
    {
        if (_player == null) return;

        if (_isLooping)
        {
            _player.Position = TimeSpan.Zero;
            _player.Play();
        }
        else
        {
            IsPlaying = false;
            PlaybackFinished?.Invoke(this, EventArgs.Empty);
        }
    }

    private void OnMediaFailed(object? sender, ExceptionEventArgs e)
    {
        _logger.LogError(e.ErrorException, $"Media playback failed: {_currentFile}");
        IsPlaying = false;
    }
}