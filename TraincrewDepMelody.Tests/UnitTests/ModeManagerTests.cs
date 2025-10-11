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
    private readonly Mock<AudioPlayer> _audioPlayerMock;
    private readonly Mock<AudioRepository> _audioRepositoryMock;
    private readonly MockTraincrewApi _mockApi;
    private readonly TraincrewApiClient _apiClient;
    private readonly Mock<StationRepository> _stationRepositoryMock;
    private readonly ApplicationState _state;
    private readonly Mock<ILoggerFactory> _loggerFactoryMock;
    private readonly Mock<ILogger<ModeManager>> _modeManagerLoggerMock;
    private readonly Mock<ILogger<StationMode>> _stationModeLoggerMock;
    private readonly Mock<ILogger<VehicleMode>> _vehicleModeLoggerMock;

    public ModeManagerTests()
    {
        _audioPlayerMock = new Mock<AudioPlayer>(MockBehavior.Loose, Mock.Of<ILogger<AudioPlayer>>());
        _audioRepositoryMock = new Mock<AudioRepository>(MockBehavior.Loose, Mock.Of<ILoggerFactory>());

        // MockTraincrewApiを直接使用
        _mockApi = new MockTraincrewApi();
        _apiClient = new TraincrewApiClient(_mockApi, Mock.Of<ILogger<TraincrewApiClient>>());

        _stationRepositoryMock = new Mock<StationRepository>(MockBehavior.Loose, Mock.Of<ILogger<StationRepository>>());
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
}
