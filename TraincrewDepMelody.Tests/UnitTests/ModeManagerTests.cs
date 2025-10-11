using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using TraincrewDepMelody.Application.Audio;
using TraincrewDepMelody.Application.Modes;
using TraincrewDepMelody.Infrastructure.Api;
using TraincrewDepMelody.Infrastructure.Repositories;
using TraincrewDepMelody.Models;
using Xunit;

namespace TraincrewDepMelody.Tests.UnitTests;

/// <summary>
/// ModeManager 単体テスト (上下線判定)
/// </summary>
public class ModeManagerTests
{
    private readonly Mock<IAudioPlayer> _audioPlayerMock;
    private readonly Mock<IAudioRepository> _audioRepositoryMock;
    private readonly MockTraincrewApi _mockApi;
    private readonly TraincrewApiClient _apiClient;
    private readonly Mock<IStationRepository> _stationRepositoryMock;
    private readonly ApplicationState _state;
    private readonly Mock<ILoggerFactory> _loggerFactoryMock;
    private readonly Mock<ILogger<ModeManager>> _modeManagerLoggerMock;
    private readonly Mock<ILogger<StationMode>> _stationModeLoggerMock;
    private readonly Mock<ILogger<VehicleMode>> _vehicleModeLoggerMock;

    public ModeManagerTests()
    {
        _audioPlayerMock = new Mock<IAudioPlayer>();
        _audioRepositoryMock = new Mock<IAudioRepository>();

        // MockTraincrewApiを直接使用
        _mockApi = new MockTraincrewApi();
        _apiClient = new TraincrewApiClient(_mockApi, Mock.Of<ILogger<TraincrewApiClient>>());

        _stationRepositoryMock = new Mock<IStationRepository>();
        _state = new ApplicationState();

        _loggerFactoryMock = new Mock<ILoggerFactory>();
        _modeManagerLoggerMock = new Mock<ILogger<ModeManager>>();
        _stationModeLoggerMock = new Mock<ILogger<StationMode>>();
        _vehicleModeLoggerMock = new Mock<ILogger<VehicleMode>>();

        // LoggerFactory のセットアップ
        _loggerFactoryMock
            .Setup(x => x.CreateLogger(It.IsAny<string>()))
            .Returns((string categoryName) =>
            {
                if (categoryName.Contains("ModeManager"))
                    return _modeManagerLoggerMock.Object;
                if (categoryName.Contains("StationMode"))
                    return _stationModeLoggerMock.Object;
                if (categoryName.Contains("VehicleMode"))
                    return _vehicleModeLoggerMock.Object;
                return Mock.Of<ILogger>();
            });
    }

    private ModeManager CreateModeManager()
    {
        return new ModeManager(
            _audioPlayerMock.Object,
            _audioRepositoryMock.Object,
            _apiClient,
            _stationRepositoryMock.Object,
            _state,
            _loggerFactoryMock.Object
        );
    }

    #region UT-MM-001: 偶数列番の判定

    [Fact]
    [Trait("Category", "Unit")]
    public async Task UT_MM_001_偶数列番の判定()
    {
        // Arrange
        var modeManager = CreateModeManager();
        _mockApi.SetTrainNumber("1262");
        await _apiClient.FetchData();

        // Act
        modeManager.Update();
        await Task.Delay(50); // Update()の非同期処理を待つ

        // Assert - 偶数なので上り
        _state.Direction.Should().Be(Direction.Up);
    }

    #endregion

    #region UT-MM-002: 奇数列番の判定

    [Fact]
    [Trait("Category", "Unit")]
    public async Task UT_MM_002_奇数列番の判定()
    {
        // Arrange
        var modeManager = CreateModeManager();
        _mockApi.SetTrainNumber("1261");
        await _apiClient.FetchData();

        // Act
        modeManager.Update();
        await Task.Delay(50);

        // Assert - 奇数なので下り
        _state.Direction.Should().Be(Direction.Down);
    }

    #endregion

    #region UT-MM-003: 回送番号(偶数)の判定

    [Fact]
    [Trait("Category", "Unit")]
    public async Task UT_MM_003_回送番号_偶数_の判定()
    {
        // Arrange
        var modeManager = CreateModeManager();
        _mockApi.SetTrainNumber("回1302A");
        await _apiClient.FetchData();

        // Act
        modeManager.Update();
        await Task.Delay(50);

        // Assert - 最後の数字が2(偶数)なので上り
        _state.Direction.Should().Be(Direction.Up);
    }

    #endregion

    #region UT-MM-004: 回送番号(奇数)の判定

    [Fact]
    [Trait("Category", "Unit")]
    public async Task UT_MM_004_回送番号_奇数_の判定()
    {
        // Arrange
        var modeManager = CreateModeManager();
        _mockApi.SetTrainNumber("回1301A");
        await _apiClient.FetchData();

        // Act
        modeManager.Update();
        await Task.Delay(50);

        // Assert - 最後の数字が1(奇数)なので下り
        _state.Direction.Should().Be(Direction.Down);
    }

    #endregion

    #region UT-MM-005: 数字なしの列番の判定

    [Fact]
    [Trait("Category", "Unit")]
    public async Task UT_MM_005_数字なしの列番の判定()
    {
        // Arrange
        var modeManager = CreateModeManager();
        _mockApi.SetTrainNumber("試運転");
        await _apiClient.FetchData();

        // Act
        modeManager.Update();
        await Task.Delay(50);

        // Assert - 数字なしなので上り(デフォルト)
        _state.Direction.Should().Be(Direction.Up);
    }

    #endregion

    #region UT-MM-006: 空文字列の列番の判定

    [Fact]
    [Trait("Category", "Unit")]
    public async Task UT_MM_006_空文字列の列番の判定()
    {
        // Arrange
        var modeManager = CreateModeManager();
        _mockApi.SetTrainNumber(string.Empty);
        await _apiClient.FetchData();

        // Act
        modeManager.Update();
        await Task.Delay(50);

        // Assert - 空文字列なので上り(デフォルト)
        _state.Direction.Should().Be(Direction.Up);
    }

    #endregion

    #region UT-MM-007: nullの列番の判定

    [Fact]
    [Trait("Category", "Unit")]
    public async Task UT_MM_007_nullの列番の判定()
    {
        // Arrange
        var modeManager = CreateModeManager();
        _mockApi.SetTrainNumber(null!);
        await _apiClient.FetchData();

        // Act
        modeManager.Update();
        await Task.Delay(50);

        // Assert - nullなので上り(デフォルト)
        _state.Direction.Should().Be(Direction.Up);
    }

    #endregion

    #region 追加テスト: 複数の数字を含む列番

    [Fact]
    [Trait("Category", "Unit")]
    public async Task DetermineDirection_複数の数字を含む列番は最後の桁で判定()
    {
        // Arrange
        var modeManager = CreateModeManager();
        _mockApi.SetTrainNumber("A1234");
        await _apiClient.FetchData();

        // Act
        modeManager.Update();
        await Task.Delay(50);

        // Assert - 最後の桁が4(偶数)なので上り
        _state.Direction.Should().Be(Direction.Up);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task DetermineDirection_複数の数字を含む列番_奇数()
    {
        // Arrange
        var modeManager = CreateModeManager();
        _mockApi.SetTrainNumber("B9875");
        await _apiClient.FetchData();

        // Act
        modeManager.Update();
        await Task.Delay(50);

        // Assert - 最後の桁が5(奇数)なので下り
        _state.Direction.Should().Be(Direction.Down);
    }

    #endregion

    #region 追加テスト: ゼロで終わる列番

    [Fact]
    [Trait("Category", "Unit")]
    public async Task DetermineDirection_ゼロで終わる列番は上り()
    {
        // Arrange
        var modeManager = CreateModeManager();
        _mockApi.SetTrainNumber("1230");
        await _apiClient.FetchData();

        // Act
        modeManager.Update();
        await Task.Delay(50);

        // Assert - 0は偶数なので上り
        _state.Direction.Should().Be(Direction.Up);
    }

    #endregion

    #region 追加テスト: 1桁の列番

    [Theory]
    [Trait("Category", "Unit")]
    [InlineData("2", Direction.Up)]
    [InlineData("3", Direction.Down)]
    [InlineData("4", Direction.Up)]
    [InlineData("5", Direction.Down)]
    [InlineData("8", Direction.Up)]
    [InlineData("9", Direction.Down)]
    public async Task DetermineDirection_1桁の列番(string trainNumber, Direction expectedDirection)
    {
        // Arrange
        var modeManager = CreateModeManager();
        _mockApi.SetTrainNumber(trainNumber);
        await _apiClient.FetchData();

        // Act
        modeManager.Update();
        await Task.Delay(50);

        // Assert
        _state.Direction.Should().Be(expectedDirection);
    }

    #endregion

    #region ボタン押下時の挙動テスト

    #region UT-MM-B-001: 車両モード中、駅にいない時にボタンを押す

    [Fact]
    [Trait("Category", "Unit")]
    public async Task UT_MM_B_001_車両モード中_駅にいない時にボタンを押す()
    {
        // Arrange
        var modeManager = CreateModeManager();
        _mockApi.SetTrainNumber("1262");
        _mockApi.SetOccupiedTracks(new List<string> { "XX-01" }); // 駅ではない軌道回路
        _stationRepositoryMock.Setup(x => x.FindStation(It.IsAny<List<string>>())).Returns((StationInfo?)null);
        await _apiClient.FetchData();

        // Act
        modeManager.Update();
        await Task.Delay(50);

        // Assert - 車両モードのまま
        _state.CurrentMode.Should().Be(ModeType.Vehicle);
        modeManager.CurrentMode.Should().BeOfType<VehicleMode>();

        // ボタンを押す
        modeManager.OnButtonPressed();

        // Assert - 車両モードのままで、AudioPlayer.Playが呼ばれる
        _audioPlayerMock.Verify(x => x.Play("vehicle", It.IsAny<string>(), true), Times.Once);
    }

    #endregion

    #region UT-MM-B-002: 車両モード中、駅にいる時にボタンを押す

    [Fact]
    [Trait("Category", "Unit")]
    public async Task UT_MM_B_002_車両モード中_駅にいる時にボタンを押す()
    {
        // Arrange
        var modeManager = CreateModeManager();
        var station = new StationInfo { StationName = "渋谷", Platform = 1 };
        _mockApi.SetTrainNumber("1262");
        _mockApi.SetOccupiedTracks(new List<string> { "SB-01", "SB-02" });
        _stationRepositoryMock.Setup(x => x.FindStation(It.IsAny<List<string>>())).Returns(station);
        _audioRepositoryMock.Setup(x => x.GetStationMelody(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<Direction>()))
            .Returns("sounds/station_melody.mp3");
        await _apiClient.FetchData();

        // Act
        modeManager.Update();
        await Task.Delay(50);

        // Assert - 駅にいる
        _state.IsAtStation.Should().BeTrue();
        _state.CurrentStation.Should().Be(station);

        // ボタンを押す → 駅モードに切替
        modeManager.OnButtonPressed();

        // Assert - 駅モードに切り替わっている
        _state.CurrentMode.Should().Be(ModeType.Station);
        modeManager.CurrentMode.Should().BeOfType<StationMode>();
    }

    #endregion

    #region UT-MM-B-003: 駅モードで1回目のボタンを押す

    [Fact]
    [Trait("Category", "Unit")]
    public async Task UT_MM_B_003_駅モードで1回目のボタンを押す()
    {
        // Arrange
        var modeManager = CreateModeManager();
        var station = new StationInfo { StationName = "渋谷", Platform = 1 };
        _mockApi.SetTrainNumber("1262");
        _mockApi.SetOccupiedTracks(new List<string> { "SB-01", "SB-02" });
        _stationRepositoryMock.Setup(x => x.FindStation(It.IsAny<List<string>>())).Returns(station);
        _audioRepositoryMock.Setup(x => x.GetStationMelody(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<Direction>()))
            .Returns("sounds/station_melody.mp3");
        await _apiClient.FetchData();

        modeManager.Update();
        await Task.Delay(50);

        // Act - 1回目のボタン押下
        modeManager.OnButtonPressed();

        // Assert - 駅モードに切り替わり、駅メロディー再生
        _state.CurrentMode.Should().Be(ModeType.Station);
        _audioPlayerMock.Verify(x => x.Play("station", "sounds/station_melody.mp3", false), Times.Once);
    }

    #endregion

    #region UT-MM-B-004: 駅モードで2回目のボタンを押す

    [Fact]
    [Trait("Category", "Unit")]
    public async Task UT_MM_B_004_駅モードで2回目のボタンを押す()
    {
        // Arrange
        var modeManager = CreateModeManager();
        var station = new StationInfo { StationName = "渋谷", Platform = 1 };
        _mockApi.SetTrainNumber("1262");
        _mockApi.SetOccupiedTracks(new List<string> { "SB-01", "SB-02" });
        _stationRepositoryMock.Setup(x => x.FindStation(It.IsAny<List<string>>())).Returns(station);
        _audioRepositoryMock.Setup(x => x.GetStationMelody(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<Direction>()))
            .Returns("sounds/station_melody.mp3");
        _audioRepositoryMock.Setup(x => x.GetVehicleMelody(It.IsAny<Direction>()))
            .Returns("sounds/vehicle_melody.mp3");
        await _apiClient.FetchData();

        modeManager.Update();
        await Task.Delay(50);

        // 1回目のボタン押下 → 駅モードへ
        modeManager.OnButtonPressed();
        _state.CurrentMode.Should().Be(ModeType.Station);

        // Act - 2回目のボタン押下
        modeManager.OnButtonPressed();

        // Assert - 車両モードに切り替わっている
        _state.CurrentMode.Should().Be(ModeType.Vehicle);
        modeManager.CurrentMode.Should().BeOfType<VehicleMode>();
        // 車両メロディーが再生される
        _audioPlayerMock.Verify(x => x.Play("vehicle", "sounds/vehicle_melody.mp3", true), Times.Once);
    }

    #endregion

    #region UT-MM-B-005: 駅モードで駅メロディー再生中にもう一度押下

    [Fact]
    [Trait("Category", "Unit")]
    public async Task UT_MM_B_005_駅モードで駅メロディー再生中にもう一度押下()
    {
        // Arrange
        var modeManager = CreateModeManager();
        var station = new StationInfo { StationName = "渋谷", Platform = 1 };
        _mockApi.SetTrainNumber("1262");
        _mockApi.SetOccupiedTracks(new List<string> { "SB-01", "SB-02" });
        _stationRepositoryMock.Setup(x => x.FindStation(It.IsAny<List<string>>())).Returns(station);
        _audioRepositoryMock.Setup(x => x.GetStationMelody(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<Direction>()))
            .Returns("sounds/station_melody.mp3");
        _audioRepositoryMock.Setup(x => x.GetVehicleMelody(It.IsAny<Direction>()))
            .Returns("sounds/vehicle_melody.mp3");
        await _apiClient.FetchData();

        modeManager.Update();
        await Task.Delay(50);

        // 1回目のボタン押下 → 駅モードへ、駅メロディー再生
        modeManager.OnButtonPressed();
        _audioPlayerMock.Verify(x => x.Play("station", "sounds/station_melody.mp3", false), Times.Once);

        // Act - 駅メロディー再生中にもう一度押下
        modeManager.OnButtonPressed();

        // Assert - 車両モードに切り替わり、車両メロディー再生
        _state.CurrentMode.Should().Be(ModeType.Vehicle);
        _audioPlayerMock.Verify(x => x.Play("vehicle", "sounds/vehicle_melody.mp3", true), Times.Once);
        // 駅側のメロディーは止まらない (StopAllやStop("station")が呼ばれない)
        _audioPlayerMock.Verify(x => x.StopAll(), Times.Never);
        _audioPlayerMock.Verify(x => x.Stop("station"), Times.Never);
    }

    #endregion

    #region UT-MM-B-006: 駅モードでアナウンス再生中にもう一度押下

    [Fact]
    [Trait("Category", "Unit")]
    public async Task UT_MM_B_006_駅モードでアナウンス再生中にもう一度押下()
    {
        // Arrange
        var modeManager = CreateModeManager();
        var station = new StationInfo { StationName = "渋谷", Platform = 1 };
        _mockApi.SetTrainNumber("1262");
        _mockApi.SetOccupiedTracks(new List<string> { "SB-01", "SB-02" });
        _stationRepositoryMock.Setup(x => x.FindStation(It.IsAny<List<string>>())).Returns(station);
        _audioRepositoryMock.Setup(x => x.GetStationMelody(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<Direction>()))
            .Returns("sounds/station_melody.mp3");
        _audioRepositoryMock.Setup(x => x.GetStationDoorClosing(It.IsAny<bool>()))
            .Returns("sounds/station_door.mp3");
        _audioRepositoryMock.Setup(x => x.GetVehicleMelody(It.IsAny<Direction>()))
            .Returns("sounds/vehicle_melody.mp3");
        await _apiClient.FetchData();

        modeManager.Update();
        await Task.Delay(50);

        // 駅モードに切り替え、駅メロディー再生
        modeManager.OnButtonPressed();

        // 駅メロディー終了 → アナウンス再生 (PlaybackFinishedイベントを発火)
        _audioPlayerMock.Raise(x => x.PlaybackFinished += null, EventArgs.Empty);
        _audioPlayerMock.Verify(x => x.Play("station", "sounds/station_door.mp3", false), Times.Once);

        // Act - アナウンス再生中にもう一度押下
        modeManager.OnButtonPressed();

        // Assert - 車両モードに切り替わり、車両メロディー再生
        _state.CurrentMode.Should().Be(ModeType.Vehicle);
        _audioPlayerMock.Verify(x => x.Play("vehicle", "sounds/vehicle_melody.mp3", true), Times.Once);
        // 駅側のアナウンスは止まらない
        _audioPlayerMock.Verify(x => x.StopAll(), Times.Never);
        _audioPlayerMock.Verify(x => x.Stop("station"), Times.Never);
    }

    #endregion

    #region UT-MM-B-007: 車両モードでアナウンス再生中にもう一度押下

    [Fact]
    [Trait("Category", "Unit")]
    public async Task UT_MM_B_007_車両モードでアナウンス再生中にもう一度押下()
    {
        // Arrange
        var modeManager = CreateModeManager();
        _mockApi.SetTrainNumber("1262");
        _mockApi.SetOccupiedTracks(new List<string> { "XX-01" });
        _stationRepositoryMock.Setup(x => x.FindStation(It.IsAny<List<string>>())).Returns((StationInfo?)null);
        _audioRepositoryMock.Setup(x => x.GetVehicleMelody(It.IsAny<Direction>()))
            .Returns("sounds/vehicle_melody.mp3");
        _audioRepositoryMock.Setup(x => x.GetVehicleDoorClosing())
            .Returns("sounds/vehicle_door.mp3");
        _audioPlayerMock.Setup(x => x.IsChannelPlaying("vehicle")).Returns(true);
        await _apiClient.FetchData();

        modeManager.Update();
        await Task.Delay(50);

        // ボタン押下 → メロディー再生
        modeManager.OnButtonPressed();
        _audioPlayerMock.Verify(x => x.Play("vehicle", "sounds/vehicle_melody.mp3", true), Times.Once);

        // ボタンリリース → アナウンス再生
        modeManager.OnButtonReleased();
        _audioPlayerMock.Verify(x => x.Stop("vehicle"), Times.Once);
        _audioPlayerMock.Verify(x => x.Play("vehicle", "sounds/vehicle_door.mp3", false), Times.Once);

        _audioPlayerMock.Invocations.Clear(); // モックのInvocationsをクリア

        // Act - アナウンス再生中にもう一度ボタン押下
        modeManager.OnButtonPressed();

        // Assert - アナウンスが停止され、メロディー再生
        _audioPlayerMock.Verify(x => x.Stop("vehicle"), Times.Once);
        _audioPlayerMock.Verify(x => x.Play("vehicle", "sounds/vehicle_melody.mp3", true), Times.Once);
    }

    #endregion

    #endregion

    #region ボタンリリース時の挙動テスト

    #region UT-MM-R-001: 車両モードでメロディーループ中にボタンをリリース

    [Fact]
    [Trait("Category", "Unit")]
    public async Task UT_MM_R_001_車両モードでメロディーループ中にボタンをリリース()
    {
        // Arrange
        var modeManager = CreateModeManager();
        _mockApi.SetTrainNumber("1262");
        _mockApi.SetOccupiedTracks(new List<string> { "XX-01" });
        _stationRepositoryMock.Setup(x => x.FindStation(It.IsAny<List<string>>())).Returns((StationInfo?)null);
        _audioRepositoryMock.Setup(x => x.GetVehicleMelody(It.IsAny<Direction>()))
            .Returns("sounds/vehicle_melody.mp3");
        _audioRepositoryMock.Setup(x => x.GetVehicleDoorClosing())
            .Returns("sounds/vehicle_door.mp3");
        await _apiClient.FetchData();

        modeManager.Update();
        await Task.Delay(50);

        // ボタン押下 → メロディーループ再生
        modeManager.OnButtonPressed();
        _audioPlayerMock.Verify(x => x.Play("vehicle", "sounds/vehicle_melody.mp3", true), Times.Once);

        // Act - ボタンリリース
        modeManager.OnButtonReleased();

        // Assert - メロディー停止、ドア締まります再生
        _audioPlayerMock.Verify(x => x.Stop("vehicle"), Times.Once);
        _audioPlayerMock.Verify(x => x.Play("vehicle", "sounds/vehicle_door.mp3", false), Times.Once);
    }

    #endregion

    #region UT-MM-R-002: 駅モードではリリースを無視

    [Fact]
    [Trait("Category", "Unit")]
    public async Task UT_MM_R_002_駅モードではリリースを無視()
    {
        // Arrange
        var modeManager = CreateModeManager();
        var station = new StationInfo { StationName = "渋谷", Platform = 1 };
        _mockApi.SetTrainNumber("1262");
        _mockApi.SetOccupiedTracks(new List<string> { "SB-01", "SB-02" });
        _stationRepositoryMock.Setup(x => x.FindStation(It.IsAny<List<string>>())).Returns(station);
        _audioRepositoryMock.Setup(x => x.GetStationMelody(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<Direction>()))
            .Returns("sounds/station_melody.mp3");
        await _apiClient.FetchData();

        modeManager.Update();
        await Task.Delay(50);

        // ボタン押下 → 駅モードへ
        modeManager.OnButtonPressed();
        _state.CurrentMode.Should().Be(ModeType.Station);

        _audioPlayerMock.Invocations.Clear();

        // Act - ボタンリリース
        modeManager.OnButtonReleased();

        // Assert - 何も起こらない（Stopが呼ばれない）
        _audioPlayerMock.Verify(x => x.Stop(It.IsAny<string>()), Times.Never);
        _audioPlayerMock.Verify(x => x.StopAll(), Times.Never);
    }

    #endregion

    #endregion

    #region 駅メロディーが見つからない場合のフォールバック

    #region UT-MM-F-001: 駅メロディーが存在しない場合、車両モードに切り替わり車両メロディー再生

    [Fact]
    [Trait("Category", "Unit")]
    public async Task UT_MM_F_001_駅メロディーが存在しない場合_車両モードに切り替わり車両メロディー再生()
    {
        // Arrange
        var modeManager = CreateModeManager();
        var station = new StationInfo { StationName = "未定義駅", Platform = 1 };
        _mockApi.SetTrainNumber("1262");
        _mockApi.SetOccupiedTracks(new List<string> { "XX-01", "XX-02" });
        _stationRepositoryMock.Setup(x => x.FindStation(It.IsAny<List<string>>())).Returns(station);
        _audioRepositoryMock.Setup(x => x.GetStationMelody(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<Direction>()))
            .Returns((string?)null); // 駅メロディーが見つからない
        _audioRepositoryMock.Setup(x => x.GetVehicleMelody(It.IsAny<Direction>()))
            .Returns("sounds/vehicle_melody.mp3");
        await _apiClient.FetchData();

        modeManager.Update();
        await Task.Delay(50);

        // 駅にいることを確認
        _state.IsAtStation.Should().BeTrue();

        // Act - ボタン押下
        modeManager.OnButtonPressed();

        // Assert - 車両モードに切り替わり、車両メロディー再生
        _state.CurrentMode.Should().Be(ModeType.Vehicle);
        modeManager.CurrentMode.Should().BeOfType<VehicleMode>();
        _audioPlayerMock.Verify(x => x.Play("vehicle", "sounds/vehicle_melody.mp3", true), Times.Once);
    }

    #endregion

    #endregion

    #region 駅到着・発車検知テスト

    #region UT-MM-S-001: 駅到着時にログ出力

    [Fact]
    [Trait("Category", "Unit")]
    public async Task UT_MM_S_001_駅到着時にログ出力()
    {
        // Arrange
        var modeManager = CreateModeManager();
        var station = new StationInfo { StationName = "渋谷", Platform = 1 };
        _mockApi.SetTrainNumber("1262");

        // 最初は駅にいない
        _mockApi.SetOccupiedTracks(new List<string> { "XX-01" });
        _stationRepositoryMock.Setup(x => x.FindStation(new List<string> { "XX-01" })).Returns((StationInfo?)null);
        await _apiClient.FetchData();
        modeManager.Update();
        await Task.Delay(50);

        _state.IsAtStation.Should().BeFalse();

        // Act - 駅に到着
        _mockApi.SetOccupiedTracks(new List<string> { "SB-01", "SB-02" });
        _stationRepositoryMock.Setup(x => x.FindStation(new List<string> { "SB-01", "SB-02" })).Returns(station);
        await _apiClient.FetchData();
        modeManager.Update();
        await Task.Delay(50);

        // Assert - 駅到着が検知される
        _state.IsAtStation.Should().BeTrue();
        _state.CurrentStation.Should().Be(station);

        // ログに「Arrived at」が出力されることを確認
        _modeManagerLoggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Arrived at")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion

    #region UT-MM-S-002: 駅発車時、駅モード中なら車両モードに切替

    [Fact]
    [Trait("Category", "Unit")]
    public async Task UT_MM_S_002_駅発車時_駅モード中なら車両モードに切替()
    {
        // Arrange
        var modeManager = CreateModeManager();
        var station = new StationInfo { StationName = "渋谷", Platform = 1 };
        _mockApi.SetTrainNumber("1262");
        _mockApi.SetOccupiedTracks(new List<string> { "SB-01", "SB-02" });
        _stationRepositoryMock.Setup(x => x.FindStation(new List<string> { "SB-01", "SB-02" })).Returns(station);
        _audioRepositoryMock.Setup(x => x.GetStationMelody(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<Direction>()))
            .Returns("sounds/station_melody.mp3");
        await _apiClient.FetchData();

        modeManager.Update();
        await Task.Delay(50);

        // 駅モードに切り替え
        modeManager.OnButtonPressed();
        _state.CurrentMode.Should().Be(ModeType.Station);

        // Act - 駅から発車
        _mockApi.SetOccupiedTracks(new List<string> { "XX-01" });
        _stationRepositoryMock.Setup(x => x.FindStation(new List<string> { "XX-01" })).Returns((StationInfo?)null);
        await _apiClient.FetchData();
        modeManager.Update();
        await Task.Delay(50);

        // Assert - 車両モードに切り替わっている
        _state.IsAtStation.Should().BeFalse();
        _state.CurrentMode.Should().Be(ModeType.Vehicle);
        modeManager.CurrentMode.Should().BeOfType<VehicleMode>();

        // ログに「Departed from」が出力されることを確認
        _modeManagerLoggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Departed from")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion

    #endregion

    #region ゲーム状態による挙動テスト

    #region UT-MM-G-001: Running → Paused で音声一時停止

    [Fact]
    [Trait("Category", "Unit")]
    public async Task UT_MM_G_001_Running_to_Paused_で音声一時停止()
    {
        // Arrange
        var modeManager = CreateModeManager();
        _mockApi.SetTrainNumber("1262");
        _mockApi.SetOccupiedTracks(new List<string> { "XX-01" });
        _mockApi.SetGameStatus(GameStatus.Running);
        _stationRepositoryMock.Setup(x => x.FindStation(It.IsAny<List<string>>())).Returns((StationInfo?)null);
        _audioPlayerMock.Setup(x => x.IsPlaying).Returns(true);
        _audioPlayerMock.Setup(x => x.IsPaused).Returns(false);
        await _apiClient.FetchData();

        modeManager.Update();
        await Task.Delay(50);

        // Act - ゲームをポーズ
        _mockApi.SetGameStatus(GameStatus.Paused);
        await _apiClient.FetchData();
        modeManager.Update();
        await Task.Delay(50);

        // Assert - 音声が一時停止される
        _audioPlayerMock.Verify(x => x.Pause(), Times.Once);
    }

    #endregion

    #region UT-MM-G-002: Paused → Running で音声再開

    [Fact]
    [Trait("Category", "Unit")]
    public async Task UT_MM_G_002_Paused_to_Running_で音声再開()
    {
        // Arrange
        var modeManager = CreateModeManager();
        _mockApi.SetTrainNumber("1262");
        _mockApi.SetOccupiedTracks(new List<string> { "XX-01" });
        _mockApi.SetGameStatus(GameStatus.Paused);
        _stationRepositoryMock.Setup(x => x.FindStation(It.IsAny<List<string>>())).Returns((StationInfo?)null);
        _audioPlayerMock.Setup(x => x.IsPaused).Returns(true);
        await _apiClient.FetchData();

        modeManager.Update();
        await Task.Delay(50);

        // Act - ゲームを再開
        _mockApi.SetGameStatus(GameStatus.Running);
        await _apiClient.FetchData();
        modeManager.Update();
        await Task.Delay(50);

        // Assert - 音声が再開される
        _audioPlayerMock.Verify(x => x.Resume(), Times.Once);
    }

    #endregion

    #region UT-MM-G-003: Stopped で音声停止、駅モード中なら車両モードに切替

    [Fact]
    [Trait("Category", "Unit")]
    public async Task UT_MM_G_003_Stopped_で音声停止_駅モード中なら車両モードに切替()
    {
        // Arrange
        var modeManager = CreateModeManager();
        var station = new StationInfo { StationName = "渋谷", Platform = 1 };
        _mockApi.SetTrainNumber("1262");
        _mockApi.SetOccupiedTracks(new List<string> { "SB-01", "SB-02" });
        _mockApi.SetGameStatus(GameStatus.Running);
        _stationRepositoryMock.Setup(x => x.FindStation(It.IsAny<List<string>>())).Returns(station);
        _audioRepositoryMock.Setup(x => x.GetStationMelody(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<Direction>()))
            .Returns("sounds/station_melody.mp3");
        await _apiClient.FetchData();

        modeManager.Update();
        await Task.Delay(50);

        // 駅モードに切り替え
        modeManager.OnButtonPressed();
        _state.CurrentMode.Should().Be(ModeType.Station);

        // Act - ゲームを停止
        _mockApi.SetGameStatus(GameStatus.Stopped);
        await _apiClient.FetchData();
        modeManager.Update();
        await Task.Delay(50);

        // Assert - 音声が停止され、車両モードに切り替わる
        _audioPlayerMock.Verify(x => x.StopAll(), Times.Once);
        _state.CurrentMode.Should().Be(ModeType.Vehicle);
        modeManager.CurrentMode.Should().BeOfType<VehicleMode>();
    }

    #endregion

    #endregion

    #region モード切替テスト

    #region UT-MM-M-001: 車両モード → 駅モード切替でApplicationState更新

    [Fact]
    [Trait("Category", "Unit")]
    public async Task UT_MM_M_001_車両モードから駅モード切替でApplicationState更新()
    {
        // Arrange
        var modeManager = CreateModeManager();
        var station = new StationInfo { StationName = "渋谷", Platform = 1 };
        _mockApi.SetTrainNumber("1262");
        _mockApi.SetOccupiedTracks(new List<string> { "SB-01", "SB-02" });
        _stationRepositoryMock.Setup(x => x.FindStation(It.IsAny<List<string>>())).Returns(station);
        _audioRepositoryMock.Setup(x => x.GetStationMelody(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<Direction>()))
            .Returns("sounds/station_melody.mp3");
        await _apiClient.FetchData();

        modeManager.Update();
        await Task.Delay(50);

        // 初期状態は車両モード
        _state.CurrentMode.Should().Be(ModeType.Vehicle);
        modeManager.CurrentMode.Should().BeOfType<VehicleMode>();

        // Act - ボタン押下で駅モードに切替
        modeManager.OnButtonPressed();

        // Assert - ApplicationStateが更新される
        _state.CurrentMode.Should().Be(ModeType.Station);
        modeManager.CurrentMode.Should().BeOfType<StationMode>();

        // ログに「Mode switch」が出力されることを確認
        _modeManagerLoggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Mode switch")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    #endregion

    #region UT-MM-M-002: 駅モード → 車両モード切替でApplicationState更新

    [Fact]
    [Trait("Category", "Unit")]
    public async Task UT_MM_M_002_駅モードから車両モード切替でApplicationState更新()
    {
        // Arrange
        var modeManager = CreateModeManager();
        var station = new StationInfo { StationName = "渋谷", Platform = 1 };
        _mockApi.SetTrainNumber("1262");
        _mockApi.SetOccupiedTracks(new List<string> { "SB-01", "SB-02" });
        _stationRepositoryMock.Setup(x => x.FindStation(It.IsAny<List<string>>())).Returns(station);
        _audioRepositoryMock.Setup(x => x.GetStationMelody(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<Direction>()))
            .Returns("sounds/station_melody.mp3");
        _audioRepositoryMock.Setup(x => x.GetVehicleMelody(It.IsAny<Direction>()))
            .Returns("sounds/vehicle_melody.mp3");
        await _apiClient.FetchData();

        modeManager.Update();
        await Task.Delay(50);

        // 駅モードに切替
        modeManager.OnButtonPressed();
        _state.CurrentMode.Should().Be(ModeType.Station);

        // Act - もう一度ボタン押下で車両モードに切替
        modeManager.OnButtonPressed();

        // Assert - ApplicationStateが更新される
        _state.CurrentMode.Should().Be(ModeType.Vehicle);
        modeManager.CurrentMode.Should().BeOfType<VehicleMode>();

        // ログに「Mode switch」が出力されることを確認
        _modeManagerLoggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Mode switch")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeast(2));
    }

    #endregion

    #endregion
}
