using System.Globalization;
using System.IO;
using System.Text;
using CsvHelper;
using Microsoft.Extensions.Logging;
using TraincrewDepMelody.Models;

namespace TraincrewDepMelody.Infrastructure.Repositories;

/// <summary>
/// 駅・番線リポジトリ
/// </summary>
public class StationRepository
{
    #region フィールド
    private Dictionary<StationPlatform, HashSet<string>> _stationTracks;
    private readonly ILogger<StationRepository> _logger;
    #endregion

    #region コンストラクタ
    public StationRepository(ILogger<StationRepository> logger)
    {
        _logger = logger;
        _stationTracks = new Dictionary<StationPlatform, HashSet<string>>();
    }
    #endregion

    #region パブリックメソッド
    /// <summary>
    /// CSVから駅定義読み込み
    /// </summary>
    public void LoadFromCsv(string filePath)
    {
        try
        {
            _logger.LogInformation($"Loading station definition: {filePath}");

            _stationTracks.Clear();

            using var reader = new StreamReader(filePath, Encoding.UTF8);
            using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

            csv.Read();
            csv.ReadHeader();

            while (csv.Read())
            {
                var stationName = csv.GetField<string>("駅名");
                var platform = csv.GetField<int>("番線");

                var tracks = new HashSet<string>();

                // 軌道回路列を可変長で読み込み
                for (int i = 1; ; i++)
                {
                    var columnName = $"軌道回路{i}";

                    if (csv.HeaderRecord == null || !csv.HeaderRecord.Contains(columnName))
                    {
                        break;
                    }

                    var track = csv.GetField<string>(columnName);

                    if (!string.IsNullOrWhiteSpace(track))
                    {
                        tracks.Add(track);
                    }
                }

                if (tracks.Count == 0)
                {
                    _logger.LogWarning($"Skipping station {stationName} platform {platform}: no track circuits");
                    continue;
                }

                var key = new StationPlatform
                {
                    StationName = stationName ?? string.Empty,
                    Platform = platform
                };

                _stationTracks[key] = tracks;

                _logger.LogInformation($"  {stationName} {platform}番線: {string.Join(", ", tracks)}");
            }

            _logger.LogInformation($"Loaded {_stationTracks.Count} station platforms");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to load station definition: {filePath}");
            throw;
        }
    }

    /// <summary>
    /// 在線駅判定
    /// </summary>
    public StationInfo? FindStation(List<string> occupiedTracks)
    {
        if (occupiedTracks == null || occupiedTracks.Count == 0)
        {
            return null;
        }

        var occupiedSet = new HashSet<string>(occupiedTracks);

        foreach (var (stationPlatform, trackSet) in _stationTracks)
        {
            if (occupiedSet.SetEquals(trackSet))
            {
                return new StationInfo
                {
                    StationName = stationPlatform.StationName,
                    Platform = stationPlatform.Platform
                };
            }
        }

        return null;
    }

    /// <summary>
    /// 駅在線判定
    /// </summary>
    public bool IsAtStation(List<string> occupiedTracks)
    {
        return FindStation(occupiedTracks) != null;
    }
    #endregion
}