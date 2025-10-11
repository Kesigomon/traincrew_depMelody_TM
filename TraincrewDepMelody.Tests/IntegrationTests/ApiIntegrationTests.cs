using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using TraincrewDepMelody.Infrastructure.Api;
using TraincrewDepMelody.Models;
using Xunit;

namespace TraincrewDepMelody.Tests.IntegrationTests;

/// <summary>
/// API連携 結合テスト
/// </summary>
public class ApiIntegrationTests
{
    private readonly Mock<ILogger<TraincrewApiClient>> _loggerMock;

    public ApiIntegrationTests()
    {
        _loggerMock = new Mock<ILogger<TraincrewApiClient>>();
    }

    #region IT-API-001: API接続成功

    [Fact]
    [Trait("Category", "Integration")]
    public async Task IT_API_001_API接続成功()
    {
        // Arrange
        var mockApi = new MockTraincrewApi();
        var apiClient = new TraincrewApiClient(mockApi, _loggerMock.Object);

        // Act
        var connected = apiClient.Connect();
        await apiClient.FetchData();

        // Assert
        connected.Should().BeTrue();
        apiClient.IsConnected().Should().BeTrue();
    }

    #endregion

    #region IT-API-003: ゲーム状態取得

    [Fact]
    [Trait("Category", "Integration")]
    public async Task IT_API_003_ゲーム状態取得()
    {
        // Arrange
        var mockApi = new MockTraincrewApi();
        var apiClient = new TraincrewApiClient(mockApi, _loggerMock.Object);

        // Act
        await apiClient.FetchData();
        var gameStatus = apiClient.GetGameStatus();

        // Assert
        gameStatus.Should().BeOneOf(GameStatus.Running, GameStatus.Paused, GameStatus.Stopped);
    }

    #endregion

    #region IT-API-004: 在線軌道回路取得

    [Fact]
    [Trait("Category", "Integration")]
    public async Task IT_API_004_在線軌道回路取得()
    {
        // Arrange
        var mockApi = new MockTraincrewApi();
        mockApi.SetOccupiedTracks(new List<string> { "SB-01", "SB-02", "SB-03" });

        var apiClient = new TraincrewApiClient(mockApi, _loggerMock.Object);

        // Act
        await apiClient.FetchData();
        var tracks = apiClient.GetTrackCircuits();

        // Assert
        tracks.Should().NotBeNull();
        tracks.Should().HaveCount(3);
        tracks.Should().Contain("SB-01");
        tracks.Should().Contain("SB-02");
        tracks.Should().Contain("SB-03");
    }

    #endregion

    #region IT-API-005: 列番取得

    [Fact]
    [Trait("Category", "Integration")]
    public async Task IT_API_005_列番取得()
    {
        // Arrange
        var mockApi = new MockTraincrewApi();
        mockApi.SetTrainNumber("1262");

        var apiClient = new TraincrewApiClient(mockApi, _loggerMock.Object);

        // Act
        await apiClient.FetchData();
        var trainNumber = apiClient.GetTrainNumber();

        // Assert
        trainNumber.Should().NotBeNull();
        trainNumber.Should().Be("1262");
    }

    #endregion

    #region IT-API-006: API接続タイムアウト (スキップ - MockAPIでは実装不可)

    [Fact(Skip = "MockAPI does not support connection failure simulation")]
    [Trait("Category", "Integration")]
    public async Task IT_API_006_API接続タイムアウト_シミュレーション()
    {
        // このテストはMockAPIでは実装できないため、スキップ
        // 実際のTraincrew API実装が完了したら、実環境でテストしてください
        await Task.CompletedTask;
    }

    #endregion

    #region 追加テスト: 複数回のFetchData呼び出し

    [Fact]
    [Trait("Category", "Integration")]
    public async Task FetchData_複数回呼び出しても正常動作()
    {
        // Arrange
        var mockApi = new MockTraincrewApi();
        var apiClient = new TraincrewApiClient(mockApi, _loggerMock.Object);

        // Act - 複数回FetchDataを呼び出す
        await apiClient.FetchData();
        var tracks1 = apiClient.GetTrackCircuits();

        mockApi.SetOccupiedTracks(new List<string> { "SJ-01", "SJ-02" });
        await apiClient.FetchData();
        var tracks2 = apiClient.GetTrackCircuits();

        // Assert - 2回目の呼び出しで更新された値が取得できる
        tracks1.Should().BeEmpty();
        tracks2.Should().HaveCount(2);
        tracks2.Should().Contain("SJ-01");
        tracks2.Should().Contain("SJ-02");
    }

    #endregion

    #region 追加テスト: GameStatusの各状態

    [Theory]
    [Trait("Category", "Integration")]
    [InlineData(GameStatus.Running)]
    [InlineData(GameStatus.Paused)]
    [InlineData(GameStatus.Stopped)]
    public async Task GetGameStatus_各状態を正常に取得(GameStatus expectedStatus)
    {
        // Arrange
        var mockApi = new MockTraincrewApi();
        mockApi.SetGameStatus(expectedStatus);

        var apiClient = new TraincrewApiClient(mockApi, _loggerMock.Object);

        // Act
        await apiClient.FetchData();
        var actualStatus = apiClient.GetGameStatus();

        // Assert
        actualStatus.Should().Be(expectedStatus);
    }

    #endregion

    #region 追加テスト: 空の在線軌道回路

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetTrackCircuits_在線なし時は空リスト()
    {
        // Arrange
        var mockApi = new MockTraincrewApi();
        mockApi.SetOccupiedTracks(new List<string>());

        var apiClient = new TraincrewApiClient(mockApi, _loggerMock.Object);

        // Act
        await apiClient.FetchData();
        var tracks = apiClient.GetTrackCircuits();

        // Assert
        tracks.Should().NotBeNull();
        tracks.Should().BeEmpty();
    }

    #endregion

    #region 追加テスト: 列番の動的変更

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetTrainNumber_列番が動的に変更される()
    {
        // Arrange
        var mockApi = new MockTraincrewApi();
        mockApi.SetTrainNumber("1234");

        var apiClient = new TraincrewApiClient(mockApi, _loggerMock.Object);

        // Act - 1回目
        await apiClient.FetchData();
        var trainNumber1 = apiClient.GetTrainNumber();

        // 列番を変更
        mockApi.SetTrainNumber("5678");
        await apiClient.FetchData();
        var trainNumber2 = apiClient.GetTrainNumber();

        // Assert
        trainNumber1.Should().Be("1234");
        trainNumber2.Should().Be("5678");
    }

    #endregion

    #region 追加テスト: IsConnected 状態確認

    [Fact]
    [Trait("Category", "Integration")]
    public async Task IsConnected_接続確認()
    {
        // Arrange
        var mockApi = new MockTraincrewApi();
        var apiClient = new TraincrewApiClient(mockApi, _loggerMock.Object);

        // Act
        apiClient.Connect();
        await apiClient.FetchData();

        // Assert - Connect後は接続されている
        apiClient.IsConnected().Should().BeTrue();
    }

    #endregion
}
