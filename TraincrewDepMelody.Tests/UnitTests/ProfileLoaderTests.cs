using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using TraincrewDepMelody.Infrastructure.Repositories;
using TraincrewDepMelody.Models;
using Xunit;

namespace TraincrewDepMelody.Tests.UnitTests;

/// <summary>
/// ProfileLoader 単体テスト
/// </summary>
public class ProfileLoaderTests : IDisposable
{
    private readonly Mock<ILogger<ProfileLoader>> _loggerMock;
    private readonly List<string> _tempFiles;
    private readonly string _tempSoundsDir;

    public ProfileLoaderTests()
    {
        _loggerMock = new Mock<ILogger<ProfileLoader>>();
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

    private string CreateTempCsvFile(string content)
    {
        var tempFile = Path.GetTempFileName();
        _tempFiles.Add(tempFile);
        File.WriteAllText(tempFile, content, Encoding.UTF8);
        return tempFile;
    }

    private string CreateTempSoundFile(string fileName)
    {
        var filePath = Path.Combine(_tempSoundsDir, fileName);
        File.WriteAllText(filePath, "dummy audio data");
        return filePath;
    }

    private ProfileLoader CreateLoader()
    {
        return new ProfileLoader(_loggerMock.Object);
    }

    #endregion

    #region UT-PL-001: 必須エントリーチェック(全て存在)

    [Fact]
    [Trait("Category", "Unit")]
    public void UT_PL_001_必須エントリーチェック_全て存在()
    {
        // Arrange
        var melodyUp = CreateTempSoundFile("melody_up.mp3");
        var melodyDown = CreateTempSoundFile("melody_down.mp3");
        var doorTrain = CreateTempSoundFile("door_train.mp3");

        var csvContent = $@"種別,駅名,番線,上下,ファイルパス
車両メロディー,,,上り,{melodyUp}
車両メロディー,,,下り,{melodyDown}
車両ドア締まります,,,,{doorTrain}";

        var tempFile = CreateTempCsvFile(csvContent);
        var loader = CreateLoader();

        // Act
        var audioFiles = loader.LoadFromCsv(tempFile);
        var validation = loader.Validate(audioFiles);

        // Assert
        validation.IsValid.Should().BeTrue();
        validation.MissingEntries.Should().BeEmpty();
        validation.MissingFiles.Should().BeEmpty();
    }

    #endregion

    #region UT-PL-002: 必須エントリーチェック(車両メロディー上り不足)

    [Fact]
    [Trait("Category", "Unit")]
    public void UT_PL_002_必須エントリーチェック_車両メロディー上り不足()
    {
        // Arrange
        var melodyDown = CreateTempSoundFile("melody_down.mp3");
        var doorTrain = CreateTempSoundFile("door_train.mp3");

        var csvContent = $@"種別,駅名,番線,上下,ファイルパス
車両メロディー,,,下り,{melodyDown}
車両ドア締まります,,,,{doorTrain}";

        var tempFile = CreateTempCsvFile(csvContent);
        var loader = CreateLoader();

        // Act
        var audioFiles = loader.LoadFromCsv(tempFile);
        var validation = loader.Validate(audioFiles);

        // Assert
        validation.IsValid.Should().BeFalse();
        validation.MissingEntries.Should().Contain(entry => entry.Contains("車両メロディー") && entry.Contains("Up"));
    }

    #endregion

    #region UT-PL-003: 必須エントリーチェック(車両メロディー下り不足)

    [Fact]
    [Trait("Category", "Unit")]
    public void UT_PL_003_必須エントリーチェック_車両メロディー下り不足()
    {
        // Arrange
        var melodyUp = CreateTempSoundFile("melody_up.mp3");
        var doorTrain = CreateTempSoundFile("door_train.mp3");

        var csvContent = $@"種別,駅名,番線,上下,ファイルパス
車両メロディー,,,上り,{melodyUp}
車両ドア締まります,,,,{doorTrain}";

        var tempFile = CreateTempCsvFile(csvContent);
        var loader = CreateLoader();

        // Act
        var audioFiles = loader.LoadFromCsv(tempFile);
        var validation = loader.Validate(audioFiles);

        // Assert
        validation.IsValid.Should().BeFalse();
        validation.MissingEntries.Should().Contain(entry => entry.Contains("車両メロディー") && entry.Contains("Down"));
    }

    #endregion

    #region UT-PL-004: 必須エントリーチェック(車両ドア締まります不足)

    [Fact]
    [Trait("Category", "Unit")]
    public void UT_PL_004_必須エントリーチェック_車両ドア締まります不足()
    {
        // Arrange
        var melodyUp = CreateTempSoundFile("melody_up.mp3");
        var melodyDown = CreateTempSoundFile("melody_down.mp3");

        var csvContent = $@"種別,駅名,番線,上下,ファイルパス
車両メロディー,,,上り,{melodyUp}
車両メロディー,,,下り,{melodyDown}";

        var tempFile = CreateTempCsvFile(csvContent);
        var loader = CreateLoader();

        // Act
        var audioFiles = loader.LoadFromCsv(tempFile);
        var validation = loader.Validate(audioFiles);

        // Assert
        validation.IsValid.Should().BeFalse();
        validation.MissingEntries.Should().Contain(entry => entry.Contains("車両ドア締まります"));
    }

    #endregion

    #region UT-PL-005: ファイル存在チェック(全て存在)

    [Fact]
    [Trait("Category", "Unit")]
    public void UT_PL_005_ファイル存在チェック_全て存在()
    {
        // Arrange
        var melodyUp = CreateTempSoundFile("melody_up.mp3");
        var melodyDown = CreateTempSoundFile("melody_down.mp3");
        var doorTrain = CreateTempSoundFile("door_train.mp3");

        var csvContent = $@"種別,駅名,番線,上下,ファイルパス
車両メロディー,,,上り,{melodyUp}
車両メロディー,,,下り,{melodyDown}
車両ドア締まります,,,,{doorTrain}";

        var tempFile = CreateTempCsvFile(csvContent);
        var loader = CreateLoader();

        // Act
        var audioFiles = loader.LoadFromCsv(tempFile);
        var validation = loader.Validate(audioFiles);

        // Assert
        validation.IsValid.Should().BeTrue();
        validation.MissingFiles.Should().BeEmpty();
    }

    #endregion

    #region UT-PL-006: ファイル存在チェック(1ファイル不足)

    [Fact]
    [Trait("Category", "Unit")]
    public void UT_PL_006_ファイル存在チェック_1ファイル不足()
    {
        // Arrange
        var melodyUp = CreateTempSoundFile("melody_up.mp3");
        var melodyDown = CreateTempSoundFile("melody_down.mp3");

        var csvContent = $@"種別,駅名,番線,上下,ファイルパス
車両メロディー,,,上り,{melodyUp}
車両メロディー,,,下り,{melodyDown}
車両ドア締まります,,,,nonexistent.mp3";

        var tempFile = CreateTempCsvFile(csvContent);
        var loader = CreateLoader();

        // Act
        var audioFiles = loader.LoadFromCsv(tempFile);
        var validation = loader.Validate(audioFiles);

        // Assert
        validation.IsValid.Should().BeFalse();
        validation.MissingFiles.Should().Contain("nonexistent.mp3");
    }

    #endregion

    #region 追加テスト: 駅メロディーの読み込み

    [Fact]
    [Trait("Category", "Unit")]
    public void LoadFromCsv_駅メロディーを正常に読み込む()
    {
        // Arrange
        var melodyUp = CreateTempSoundFile("melody_up.mp3");
        var melodyDown = CreateTempSoundFile("melody_down.mp3");
        var doorTrain = CreateTempSoundFile("door_train.mp3");
        var shibuyaMelody = CreateTempSoundFile("shibuya_1.mp3");

        var csvContent = $@"種別,駅名,番線,上下,ファイルパス
駅メロディー,渋谷,1,,{shibuyaMelody}
車両メロディー,,,上り,{melodyUp}
車両メロディー,,,下り,{melodyDown}
車両ドア締まります,,,,{doorTrain}";

        var tempFile = CreateTempCsvFile(csvContent);
        var loader = CreateLoader();

        // Act
        var audioFiles = loader.LoadFromCsv(tempFile);

        // Assert
        audioFiles.Should().HaveCount(4);

        var stationMelodyKey = new AudioKey
        {
            Type = AudioType.StationMelody,
            StationName = "渋谷",
            Platform = 1
        };
        audioFiles.Should().ContainKey(stationMelodyKey);
        audioFiles[stationMelodyKey].Should().Be(shibuyaMelody);
    }

    #endregion

    #region 追加テスト: 駅ドア締まりますの読み込み

    [Fact]
    [Trait("Category", "Unit")]
    public void LoadFromCsv_駅ドア締まりますを正常に読み込む()
    {
        // Arrange
        var melodyUp = CreateTempSoundFile("melody_up.mp3");
        var melodyDown = CreateTempSoundFile("melody_down.mp3");
        var doorTrain = CreateTempSoundFile("door_train.mp3");
        var doorOdd = CreateTempSoundFile("door_odd.mp3");
        var doorEven = CreateTempSoundFile("door_even.mp3");

        var csvContent = $@"種別,駅名,番線,上下,ファイルパス
車両メロディー,,,上り,{melodyUp}
車両メロディー,,,下り,{melodyDown}
車両ドア締まります,,,,{doorTrain}
駅ドア締まります,,,奇数,{doorOdd}
駅ドア締まります,,,偶数,{doorEven}";

        var tempFile = CreateTempCsvFile(csvContent);
        var loader = CreateLoader();

        // Act
        var audioFiles = loader.LoadFromCsv(tempFile);

        // Assert
        var oddKey = new AudioKey
        {
            Type = AudioType.StationDoorClosing,
            IsOdd = true
        };
        audioFiles.Should().ContainKey(oddKey);
        audioFiles[oddKey].Should().Be(doorOdd);

        var evenKey = new AudioKey
        {
            Type = AudioType.StationDoorClosing,
            IsOdd = false
        };
        audioFiles.Should().ContainKey(evenKey);
        audioFiles[evenKey].Should().Be(doorEven);
    }

    #endregion

    #region 追加テスト: 空のファイルパスはスキップ

    [Fact]
    [Trait("Category", "Unit")]
    public void LoadFromCsv_空のファイルパスはスキップ()
    {
        // Arrange
        var melodyUp = CreateTempSoundFile("melody_up.mp3");
        var melodyDown = CreateTempSoundFile("melody_down.mp3");
        var doorTrain = CreateTempSoundFile("door_train.mp3");

        var csvContent = $@"種別,駅名,番線,上下,ファイルパス
車両メロディー,,,上り,{melodyUp}
車両メロディー,,,下り,{melodyDown}
車両ドア締まります,,,,{doorTrain}
駅メロディー,スキップ,1,,";

        var tempFile = CreateTempCsvFile(csvContent);
        var loader = CreateLoader();

        // Act
        var audioFiles = loader.LoadFromCsv(tempFile);

        // Assert - 空のファイルパスはスキップされる
        audioFiles.Should().HaveCount(3);
    }

    #endregion

    #region 追加テスト: 複数の必須エントリー不足

    [Fact]
    [Trait("Category", "Unit")]
    public void Validate_複数の必須エントリー不足()
    {
        // Arrange
        var melodyUp = CreateTempSoundFile("melody_up.mp3");

        var csvContent = $@"種別,駅名,番線,上下,ファイルパス
車両メロディー,,,上り,{melodyUp}";

        var tempFile = CreateTempCsvFile(csvContent);
        var loader = CreateLoader();

        // Act
        var audioFiles = loader.LoadFromCsv(tempFile);
        var validation = loader.Validate(audioFiles);

        // Assert
        validation.IsValid.Should().BeFalse();
        validation.MissingEntries.Should().HaveCountGreaterOrEqualTo(2);
    }

    #endregion
}
