namespace TraincrewDepMelody.Application.Audio;

/// <summary>
/// 音声再生インターフェース
/// </summary>
public interface IAudioPlayer
{
    /// <summary>
    /// 再生中かどうか
    /// </summary>
    bool IsPlaying { get; }

    /// <summary>
    /// 一時停止中かどうか
    /// </summary>
    bool IsPaused { get; }

    /// <summary>
    /// 現在再生中のファイル
    /// </summary>
    string CurrentFile { get; }

    /// <summary>
    /// 再生終了イベント
    /// </summary>
    event EventHandler? PlaybackFinished;

    /// <summary>
    /// 音声再生(チャンネル指定)
    /// </summary>
    void Play(string channelId, string filePath, bool loop = false);

    /// <summary>
    /// 停止(チャンネル指定)
    /// </summary>
    void Stop(string channelId);

    /// <summary>
    /// 全チャンネル停止
    /// </summary>
    void StopAll();

    /// <summary>
    /// 一時停止(全チャンネル)
    /// </summary>
    void Pause();

    /// <summary>
    /// 再開(全チャンネル)
    /// </summary>
    void Resume();

    /// <summary>
    /// 音量設定 (0.0 ~ 1.0)
    /// </summary>
    void SetVolume(double volume);

    /// <summary>
    /// チャンネルが再生中かチェック
    /// </summary>
    bool IsChannelPlaying(string channelId);
}
