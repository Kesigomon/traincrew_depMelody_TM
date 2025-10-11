using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using TraincrewDepMelody.Infrastructure.Repositories;
using TraincrewDepMelody.Models;
using Xunit;

namespace TraincrewDepMelody.Tests.UnitTests;

/// <summary>
/// AudioRepository 単体テスト
/// </summary>
public class AudioRepositoryTests : IDisposable
{
    private readonly Mock<ILoggerFactory> _loggerFactoryMock;
    private readonly Mock<ILogger<AudioRepository>> _audioRepoLoggerMock;
    private readonly Mock<ILogger<ProfileLoader>> _profileLoaderLoggerMock;
    private readonly List<string> _tempFiles;
    private readonly string _tempSoundsDir;

    public AudioRepositoryTests()
    {
        _loggerFactoryMock = new Mock<ILoggerFactory>();
        _audioRepoLoggerMock = new Mock<ILogger<AudioRepository>>();
        _profileLoaderLoggerMock = new Mock<ILogger<ProfileLoader>>();
        _tempFiles = new List<string>();

        // テンポラリの音声ディレクトリ作成
        _tempSoundsDir = Path.Combine(Path.GetTempPath(), $"test_sounds_{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempSoundsDir);

        // LoggerFactory のセットアップ
        _loggerFactoryMock
            .Setup(x => x.CreateLogger(It.IsAny<string>()))
            .Returns((string categoryName) =>
            {
                if (categoryName.Contains("AudioRepository"))
                    return _audioRepoLoggerMock.Object;
                if (categoryName.Contains("ProfileLoader"))
                    return _profileLoaderLoggerMock.Object;
                return Mock.Of<ILogger>();
            });
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

    private AudioRepository CreateRepository()
    {
        return new AudioRepository(_loggerFactoryMock.Object);
    }

    #endregion

    #region UT-AR-001: プロファイルCSV正常読み込み

    [Fact]
    [Trait("Category", "Unit")]
    public void UT_AR_001_プロファイルCSV正常読み込み()
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
        var repository = CreateRepository();

        // Act
        repository.LoadProfile(tempFile);

        // Assert - 正常にロードされ、各ファイルが取得できる
        var stationMelody = repository.GetStationMelody("渋谷", 1, Direction.Up);
        stationMelody.Should().Be(shibuyaMelody);

        var vehicleMelodyUp = repository.GetVehicleMelody(Direction.Up);
        vehicleMelodyUp.Should().Be(melodyUp);

        var vehicleMelodyDown = repository.GetVehicleMelody(Direction.Down);
        vehicleMelodyDown.Should().Be(melodyDown);

        var doorClosing = repository.GetVehicleDoorClosing();
        doorClosing.Should().Be(doorTrain);
    }

    #endregion

    #region UT-AR-002: 駅メロディー取得

    [Fact]
    [Trait("Category", "Unit")]
    public void UT_AR_002_駅メロディー取得()
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
        var repository = CreateRepository();
        repository.LoadProfile(tempFile);

        // Act
        var filePath = repository.GetStationMelody("渋谷", 1, Direction.Up);

        // Assert
        filePath.Should().Be(shibuyaMelody);
    }

    #endregion

    #region UT-AR-003: 駅メロディー未定義時のフォールバック

    [Fact]
    [Trait("Category", "Unit")]
    public void UT_AR_003_駅メロディー未定義時のフォールバック()
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
        var repository = CreateRepository();
        repository.LoadProfile(tempFile);

        // Act - 未定義の駅メロディーを取得
        var filePath = repository.GetStationMelody("未定義駅", 1, Direction.Up);

        // Assert - 車両メロディー(上り)にフォールバック
        filePath.Should().Be(melodyUp);
    }

    #endregion

    #region UT-AR-004: 車両メロディー取得(上り)

    [Fact]
    [Trait("Category", "Unit")]
    public void UT_AR_004_車両メロディー取得_上り()
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
        var repository = CreateRepository();
        repository.LoadProfile(tempFile);

        // Act
        var filePath = repository.GetVehicleMelody(Direction.Up);

        // Assert
        filePath.Should().Be(melodyUp);
    }

    #endregion

    #region UT-AR-005: 車両メロディー取得(下り)

    [Fact]
    [Trait("Category", "Unit")]
    public void UT_AR_005_車両メロディー取得_下り()
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
        var repository = CreateRepository();
        repository.LoadProfile(tempFile);

        // Act
        var filePath = repository.GetVehicleMelody(Direction.Down);

        // Assert
        filePath.Should().Be(melodyDown);
    }

    #endregion

    #region UT-AR-006: 必須エントリー不足時のバリデーション

    [Fact]
    [Trait("Category", "Unit")]
    public void UT_AR_006_必須エントリー不足時のバリデーション()
    {
        // Arrange - 車両メロディー(下り)なし
        var melodyUp = CreateTempSoundFile("melody_up.mp3");
        var doorTrain = CreateTempSoundFile("door_train.mp3");

        var csvContent = $@"種別,駅名,番線,上下,ファイルパス
車両メロディー,,,上り,{melodyUp}
車両ドア締まります,,,,{doorTrain}";

        var tempFile = CreateTempCsvFile(csvContent);
        var repository = CreateRepository();

        // Act & Assert - バリデーションエラー
        Action act = () => repository.LoadProfile(tempFile);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*必須エントリー不足*");
    }

    #endregion

    #region UT-AR-007: ファイル存在チェック

    [Fact]
    [Trait("Category", "Unit")]
    public void UT_AR_007_ファイル存在チェック()
    {
        // Arrange - 存在しないファイルパス
        var csvContent = @"種別,駅名,番線,上下,ファイルパス
車両メロディー,,,上り,nonexistent_up.mp3
車両メロディー,,,下り,nonexistent_down.mp3
車両ドア締まります,,,,nonexistent_door.mp3";

        var tempFile = CreateTempCsvFile(csvContent);
        var repository = CreateRepository();

        // Act & Assert - バリデーションエラー
        Action act = () => repository.LoadProfile(tempFile);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*ファイルが見つかりません*");
    }

    #endregion

    #region 追加テスト: GetVehicleDoorClosing

    [Fact]
    [Trait("Category", "Unit")]
    public void GetVehicleDoorClosing_正常取得()
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
        var repository = CreateRepository();
        repository.LoadProfile(tempFile);

        // Act
        var filePath = repository.GetVehicleDoorClosing();

        // Assert
        filePath.Should().Be(doorTrain);
    }

    #endregion

    #region 追加テスト: GetStationDoorClosing

    [Fact]
    [Trait("Category", "Unit")]
    public void GetStationDoorClosing_奇数番線()
    {
        // Arrange
        var melodyUp = CreateTempSoundFile("melody_up.mp3");
        var melodyDown = CreateTempSoundFile("melody_down.mp3");
        var doorTrain = CreateTempSoundFile("door_train.mp3");
        var doorOdd = CreateTempSoundFile("door_odd.mp3");

        var csvContent = $@"種別,駅名,番線,上下,ファイルパス
車両メロディー,,,上り,{melodyUp}
車両メロディー,,,下り,{melodyDown}
車両ドア締まります,,,,{doorTrain}
駅ドア締まります,,,奇数,{doorOdd}";

        var tempFile = CreateTempCsvFile(csvContent);
        var repository = CreateRepository();
        repository.LoadProfile(tempFile);

        // Act
        var filePath = repository.GetStationDoorClosing(isOddPlatform: true);

        // Assert
        filePath.Should().Be(doorOdd);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void GetStationDoorClosing_駅ドア未定義時はフォールバック()
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
        var repository = CreateRepository();
        repository.LoadProfile(tempFile);

        // Act - 駅ドア締まります未定義
        var filePath = repository.GetStationDoorClosing(isOddPlatform: true);

        // Assert - 車両ドア締まりますにフォールバック
        filePath.Should().Be(doorTrain);
    }

    #endregion

    #region 追加テスト: 必須エントリー全不足

    [Fact]
    [Trait("Category", "Unit")]
    public void LoadProfile_必須エントリー全不足時はエラー()
    {
        // Arrange - 必須エントリーなし
        var csvContent = @"種別,駅名,番線,上下,ファイルパス
駅メロディー,渋谷,1,,dummy.mp3";

        var tempFile = CreateTempCsvFile(csvContent);
        var repository = CreateRepository();

        // Act & Assert
        Action act = () => repository.LoadProfile(tempFile);
        act.Should().Throw<InvalidOperationException>();
    }

    #endregion
}
