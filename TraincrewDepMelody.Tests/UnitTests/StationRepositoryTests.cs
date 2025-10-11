using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using TraincrewDepMelody.Infrastructure.Repositories;
using Xunit;

namespace TraincrewDepMelody.Tests.UnitTests;

/// <summary>
/// StationRepository 単体テスト
/// </summary>
public class StationRepositoryTests : IDisposable
{
    private readonly Mock<ILogger<StationRepository>> _loggerMock;
    private readonly List<string> _tempFiles;

    public StationRepositoryTests()
    {
        _loggerMock = new Mock<ILogger<StationRepository>>();
        _tempFiles = new List<string>();
    }

    public void Dispose()
    {
        // テスト後にテンポラリファイルをクリーンアップ
        foreach (var file in _tempFiles)
        {
            if (File.Exists(file))
            {
                File.Delete(file);
            }
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

    private StationRepository CreateRepository()
    {
        return new StationRepository(_loggerMock.Object);
    }

    #endregion

    #region UT-SR-001: 駅定義CSV正常読み込み

    [Fact]
    [Trait("Category", "Unit")]
    public void UT_SR_001_駅定義CSV正常読み込み()
    {
        // Arrange
        var csvContent = @"駅名,番線,軌道回路1,軌道回路2,軌道回路3
渋谷,1,SB-01,SB-02,SB-03
渋谷,2,SB-04,SB-05,
新宿,1,SJ-01,SJ-02,SJ-03";
        var tempFile = CreateTempCsvFile(csvContent);
        var repository = CreateRepository();

        // Act
        repository.LoadFromCsv(tempFile);

        // Assert - 正常にロードされることを確認
        var station1 = repository.FindStation(new List<string> { "SB-01", "SB-02", "SB-03" });
        station1.Should().NotBeNull();
        station1!.StationName.Should().Be("渋谷");
        station1.Platform.Should().Be(1);

        var station2 = repository.FindStation(new List<string> { "SB-04", "SB-05" });
        station2.Should().NotBeNull();
        station2!.StationName.Should().Be("渋谷");
        station2.Platform.Should().Be(2);

        var station3 = repository.FindStation(new List<string> { "SJ-01", "SJ-02", "SJ-03" });
        station3.Should().NotBeNull();
        station3!.StationName.Should().Be("新宿");
        station3.Platform.Should().Be(1);
    }

    #endregion

    #region UT-SR-002: 完全一致する駅の判定

    [Fact]
    [Trait("Category", "Unit")]
    public void UT_SR_002_完全一致する駅の判定()
    {
        // Arrange
        var csvContent = @"駅名,番線,軌道回路1,軌道回路2,軌道回路3
渋谷,1,SB-01,SB-02,SB-03
渋谷,2,SB-04,SB-05,";
        var tempFile = CreateTempCsvFile(csvContent);
        var repository = CreateRepository();
        repository.LoadFromCsv(tempFile);

        // Act
        var occupiedTracks = new List<string> { "SB-01", "SB-02", "SB-03" };
        var station = repository.FindStation(occupiedTracks);

        // Assert
        station.Should().NotBeNull();
        station!.StationName.Should().Be("渋谷");
        station.Platform.Should().Be(1);
    }

    #endregion

    #region UT-SR-003: 部分一致(不一致)の駅判定

    [Fact]
    [Trait("Category", "Unit")]
    public void UT_SR_003_部分一致_不一致_の駅判定()
    {
        // Arrange
        var csvContent = @"駅名,番線,軌道回路1,軌道回路2,軌道回路3
渋谷,1,SB-01,SB-02,SB-03";
        var tempFile = CreateTempCsvFile(csvContent);
        var repository = CreateRepository();
        repository.LoadFromCsv(tempFile);

        // Act - 部分一致のみ(SB-03が不足)
        var occupiedTracks = new List<string> { "SB-01", "SB-02" };
        var station = repository.FindStation(occupiedTracks);

        // Assert - 完全一致でないため、駅在線なし
        station.Should().BeNull();
    }

    #endregion

    #region UT-SR-004: 順序が異なる場合の駅判定

    [Fact]
    [Trait("Category", "Unit")]
    public void UT_SR_004_順序が異なる場合の駅判定()
    {
        // Arrange
        var csvContent = @"駅名,番線,軌道回路1,軌道回路2,軌道回路3
渋谷,1,SB-01,SB-02,SB-03";
        var tempFile = CreateTempCsvFile(csvContent);
        var repository = CreateRepository();
        repository.LoadFromCsv(tempFile);

        // Act - 順序が異なる
        var occupiedTracks = new List<string> { "SB-03", "SB-01", "SB-02" };
        var station = repository.FindStation(occupiedTracks);

        // Assert - 順序に関係なく完全一致として判定される
        station.Should().NotBeNull();
        station!.StationName.Should().Be("渋谷");
        station.Platform.Should().Be(1);
    }

    #endregion

    #region UT-SR-005: 空の軌道回路リストの判定

    [Fact]
    [Trait("Category", "Unit")]
    public void UT_SR_005_空の軌道回路リストの判定()
    {
        // Arrange
        var csvContent = @"駅名,番線,軌道回路1,軌道回路2,軌道回路3
渋谷,1,SB-01,SB-02,SB-03";
        var tempFile = CreateTempCsvFile(csvContent);
        var repository = CreateRepository();
        repository.LoadFromCsv(tempFile);

        // Act
        var occupiedTracks = new List<string>();
        var station = repository.FindStation(occupiedTracks);

        // Assert - 空のリストは駅在線なし
        station.Should().BeNull();
    }

    #endregion

    #region UT-SR-006: nullの軌道回路リストの判定

    [Fact]
    [Trait("Category", "Unit")]
    public void UT_SR_006_nullの軌道回路リストの判定()
    {
        // Arrange
        var csvContent = @"駅名,番線,軌道回路1,軌道回路2,軌道回路3
渋谷,1,SB-01,SB-02,SB-03";
        var tempFile = CreateTempCsvFile(csvContent);
        var repository = CreateRepository();
        repository.LoadFromCsv(tempFile);

        // Act
        var station = repository.FindStation(null!);

        // Assert - nullは駅在線なし
        station.Should().BeNull();
    }

    #endregion

    #region UT-SR-007: CSVフォーマットエラー

    [Fact]
    [Trait("Category", "Unit")]
    public void UT_SR_007_CSVフォーマットエラー()
    {
        // Arrange - 存在しないファイル
        var nonExistentFile = "C:\\nonexistent\\file.csv";
        var repository = CreateRepository();

        // Act & Assert - 例外発生
        Action act = () => repository.LoadFromCsv(nonExistentFile);
        act.Should().Throw<Exception>();
    }

    #endregion

    #region 追加テスト: IsAtStation メソッド

    [Fact]
    [Trait("Category", "Unit")]
    public void IsAtStation_駅在線時はtrueを返す()
    {
        // Arrange
        var csvContent = @"駅名,番線,軌道回路1,軌道回路2,軌道回路3
渋谷,1,SB-01,SB-02,SB-03";
        var tempFile = CreateTempCsvFile(csvContent);
        var repository = CreateRepository();
        repository.LoadFromCsv(tempFile);

        // Act
        var occupiedTracks = new List<string> { "SB-01", "SB-02", "SB-03" };
        var isAtStation = repository.IsAtStation(occupiedTracks);

        // Assert
        isAtStation.Should().BeTrue();
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void IsAtStation_駅非在線時はfalseを返す()
    {
        // Arrange
        var csvContent = @"駅名,番線,軌道回路1,軌道回路2,軌道回路3
渋谷,1,SB-01,SB-02,SB-03";
        var tempFile = CreateTempCsvFile(csvContent);
        var repository = CreateRepository();
        repository.LoadFromCsv(tempFile);

        // Act
        var occupiedTracks = new List<string> { "XX-01", "XX-02" };
        var isAtStation = repository.IsAtStation(occupiedTracks);

        // Assert
        isAtStation.Should().BeFalse();
    }

    #endregion

    #region 追加テスト: 重複する軌道回路

    [Fact]
    [Trait("Category", "Unit")]
    public void FindStation_在線軌道回路に重複がある場合でも正常に判定()
    {
        // Arrange
        var csvContent = @"駅名,番線,軌道回路1,軌道回路2,軌道回路3
渋谷,1,SB-01,SB-02,SB-03";
        var tempFile = CreateTempCsvFile(csvContent);
        var repository = CreateRepository();
        repository.LoadFromCsv(tempFile);

        // Act - 重複を含むリスト
        var occupiedTracks = new List<string> { "SB-01", "SB-02", "SB-03", "SB-01" };
        var station = repository.FindStation(occupiedTracks);

        // Assert - 重複は無視されて正常に判定
        station.Should().NotBeNull();
        station!.StationName.Should().Be("渋谷");
    }

    #endregion

    #region 追加テスト: 余分な軌道回路

    [Fact]
    [Trait("Category", "Unit")]
    public void FindStation_余分な軌道回路がある場合は一致しない()
    {
        // Arrange
        var csvContent = @"駅名,番線,軌道回路1,軌道回路2,軌道回路3
渋谷,1,SB-01,SB-02,SB-03";
        var tempFile = CreateTempCsvFile(csvContent);
        var repository = CreateRepository();
        repository.LoadFromCsv(tempFile);

        // Act - 余分な軌道回路を含む
        var occupiedTracks = new List<string> { "SB-01", "SB-02", "SB-03", "SB-04" };
        var station = repository.FindStation(occupiedTracks);

        // Assert - 完全一致でないため、駅在線なし
        station.Should().BeNull();
    }

    #endregion
}
