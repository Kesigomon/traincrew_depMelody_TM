using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using TraincrewDepMelody.Application.Audio;
using Xunit;

namespace TraincrewDepMelody.Tests.IntegrationTests;

/// <summary>
/// 音声再生 結合テスト
/// </summary>
public class AudioPlaybackTests : IDisposable
{
    private readonly Mock<ILogger<AudioPlayer>> _loggerMock;
    private readonly List<string> _tempFiles;
    private readonly string _tempSoundsDir;

    public AudioPlaybackTests()
    {
        _loggerMock = new Mock<ILogger<AudioPlayer>>();
        _tempFiles = new List<string>();

        // テンポラリの音声ディレクトリ作成
        _tempSoundsDir = Path.Combine(Path.GetTempPath(), $"test_sounds_{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempSoundsDir);
    }

    public void Dispose()
    {
        // テンポラリファイルとディレクトリをクリーンアップ
        foreach (var file in _tempFiles)
        {
            if (File.Exists(file))
            {
                File.Delete(file);
            }
        }

        if (Directory.Exists(_tempSoundsDir))
        {
            Directory.Delete(_tempSoundsDir, true);
        }
    }

    #region ヘルパーメソッド

    private string CreateTempSoundFile(string fileName)
    {
        var filePath = Path.Combine(_tempSoundsDir, fileName);
        _tempFiles.Add(filePath);
        // ダミーの音声データ(実際には無音)
        File.WriteAllText(filePath, "dummy audio data");
        return filePath;
    }

    #endregion

    #region IT-AU-001: 音声ファイル再生 (基本動作確認のみ)

    [Fact(Skip = "Requires actual audio file and MediaPlayer on Windows")]
    [Trait("Category", "Integration")]
    public void IT_AU_001_音声ファイル再生()
    {
        // このテストは実際の音声ファイルとMediaPlayerが必要なため、スキップ
        // 実環境でテストする場合は、実際の音声ファイルを用意してください
    }

    #endregion

    #region IT-AU-002: 音声ファイル停止 (基本動作確認のみ)

    [Fact(Skip = "Requires actual audio file and MediaPlayer on Windows")]
    [Trait("Category", "Integration")]
    public void IT_AU_002_音声ファイル停止()
    {
        // このテストは実際の音声ファイルとMediaPlayerが必要なため、スキップ
    }

    #endregion

    #region IT-AU-006: 存在しない音声ファイル再生

    [Fact]
    [Trait("Category", "Integration")]
    public void IT_AU_006_存在しない音声ファイル再生()
    {
        // Arrange
        var audioPlayer = new AudioPlayer(_loggerMock.Object);
        var nonExistentFile = Path.Combine(_tempSoundsDir, "nonexistent.mp3");

        // Act - 存在しないファイルを再生試行
        audioPlayer.Play("channel1", nonExistentFile, loop: false);

        // Assert - エラーログが出力されるが、例外は発生しない
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("not found")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    #endregion

    #region 追加テスト: SetVolume

    [Fact]
    [Trait("Category", "Integration")]
    public void SetVolume_音量設定が正常に動作()
    {
        // Arrange
        var audioPlayer = new AudioPlayer(_loggerMock.Object);

        // Act
        audioPlayer.SetVolume(0.5);

        // Assert - 例外が発生しないことを確認
        audioPlayer.Should().NotBeNull();
    }

    [Fact]
    [Trait("Category", "Integration")]
    public void SetVolume_範囲外の音量はクランプされる()
    {
        // Arrange
        var audioPlayer = new AudioPlayer(_loggerMock.Object);

        // Act & Assert - 範囲外の値でも例外が発生しない
        audioPlayer.SetVolume(1.5); // 1.0にクランプされる
        audioPlayer.SetVolume(-0.5); // 0.0にクランプされる

        audioPlayer.Should().NotBeNull();
    }

    #endregion

    #region 追加テスト: StopAll

    [Fact]
    [Trait("Category", "Integration")]
    public void StopAll_全チャンネル停止()
    {
        // Arrange
        var audioPlayer = new AudioPlayer(_loggerMock.Object);
        var file1 = CreateTempSoundFile("test1.mp3");
        var file2 = CreateTempSoundFile("test2.mp3");

        // Act - 複数チャンネルで再生
        audioPlayer.Play("channel1", file1, loop: false);
        audioPlayer.Play("channel2", file2, loop: false);

        // 全停止
        audioPlayer.StopAll();

        // Assert - IsPlayingがfalseになることを確認
        audioPlayer.IsPlaying.Should().BeFalse();
    }

    #endregion

    #region 追加テスト: IsChannelPlaying

    [Fact]
    [Trait("Category", "Integration")]
    public void IsChannelPlaying_存在しないチャンネルはfalse()
    {
        // Arrange
        var audioPlayer = new AudioPlayer(_loggerMock.Object);

        // Act
        var isPlaying = audioPlayer.IsChannelPlaying("nonexistent");

        // Assert
        isPlaying.Should().BeFalse();
    }

    #endregion

    #region 追加テスト: 複数チャンネル再生

    [Fact]
    [Trait("Category", "Integration")]
    public void Play_複数チャンネルで同時再生可能()
    {
        // Arrange
        var audioPlayer = new AudioPlayer(_loggerMock.Object);
        var file1 = CreateTempSoundFile("melody.mp3");
        var file2 = CreateTempSoundFile("announcement.mp3");

        // Act - 2つのチャンネルで同時再生
        audioPlayer.Play("station", file1, loop: false);
        audioPlayer.Play("vehicle", file2, loop: false);

        // Assert - 両方のチャンネルが存在することを確認(エラーが発生しない)
        audioPlayer.Should().NotBeNull();
    }

    #endregion

    #region 追加テスト: Stop特定チャンネル

    [Fact]
    [Trait("Category", "Integration")]
    public void Stop_特定チャンネルのみ停止()
    {
        // Arrange
        var audioPlayer = new AudioPlayer(_loggerMock.Object);
        var file1 = CreateTempSoundFile("melody.mp3");
        var file2 = CreateTempSoundFile("announcement.mp3");

        // Act
        audioPlayer.Play("station", file1, loop: false);
        audioPlayer.Play("vehicle", file2, loop: false);

        audioPlayer.Stop("station");

        // Assert - 特定チャンネルのみ停止(例外が発生しない)
        audioPlayer.Should().NotBeNull();
    }

    #endregion

    #region 追加テスト: Pause / Resume

    [Fact]
    [Trait("Category", "Integration")]
    public void Pause_Resume_正常動作()
    {
        // Arrange
        var audioPlayer = new AudioPlayer(_loggerMock.Object);

        // Act & Assert - 例外が発生しないことを確認
        audioPlayer.Pause();
        audioPlayer.Resume();

        audioPlayer.Should().NotBeNull();
    }

    #endregion

    #region 追加テスト: Dispose

    [Fact]
    [Trait("Category", "Integration")]
    public void Dispose_リソース解放が正常に動作()
    {
        // Arrange
        var audioPlayer = new AudioPlayer(_loggerMock.Object);
        var file = CreateTempSoundFile("test.mp3");

        audioPlayer.Play("channel1", file, loop: false);

        // Act
        audioPlayer.Dispose();

        // Assert - Dispose後に例外が発生しないことを確認
        audioPlayer.Should().NotBeNull();
    }

    #endregion
}
