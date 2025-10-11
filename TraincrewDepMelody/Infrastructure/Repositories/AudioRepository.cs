using System.IO;
using System.Text;
using Microsoft.Extensions.Logging;
using TraincrewDepMelody.Models;

namespace TraincrewDepMelody.Infrastructure.Repositories;

/// <summary>
/// 音声ファイルリポジトリ
/// </summary>
public class AudioRepository : IAudioRepository
{
    #region フィールド
    private Dictionary<AudioKey, string> _audioFiles;
    private readonly ILogger<AudioRepository> _logger;
    private readonly ILoggerFactory _loggerFactory;
    #endregion

    #region コンストラクタ
    public AudioRepository(ILoggerFactory loggerFactory)
    {
        _loggerFactory = loggerFactory;
        _logger = loggerFactory.CreateLogger<AudioRepository>();
        _audioFiles = new Dictionary<AudioKey, string>();
    }
    #endregion

    #region パブリックメソッド
    /// <summary>
    /// プロファイル読み込み
    /// </summary>
    public void LoadProfile(string profileCsvPath)
    {
        try
        {
            _logger.LogInformation($"Loading audio profile: {profileCsvPath}");

            var loader = new ProfileLoader(_loggerFactory.CreateLogger<ProfileLoader>());
            _audioFiles = loader.LoadFromCsv(profileCsvPath);

            // バリデーション
            var validation = loader.Validate(_audioFiles);

            if (!validation.IsValid)
            {
                var errorMessage = GenerateValidationErrorMessage(validation);
                _logger.LogError(errorMessage);
                throw new InvalidOperationException(errorMessage);
            }

            _logger.LogInformation($"Loaded {_audioFiles.Count} audio entries");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to load profile: {profileCsvPath}");
            throw;
        }
    }

    /// <summary>
    /// 駅メロディー取得
    /// </summary>
    public string? GetStationMelody(string stationName, int platform, Direction direction)
    {
        var key = new AudioKey
        {
            Type = AudioType.StationMelody,
            StationName = stationName,
            Platform = platform
        };

        if (_audioFiles.TryGetValue(key, out var path))
        {
            return path;
        }

        // 見つからない場合はnullを返す
        _logger.LogWarning($"Station melody not found: {stationName} {platform}番線");
        return null;
    }

    /// <summary>
    /// 駅ドア締まりますアナウンス取得
    /// </summary>
    public string? GetStationDoorClosing(bool isOddPlatform)
    {
        var key = new AudioKey
        {
            Type = AudioType.StationDoorClosing,
            IsOdd = isOddPlatform
        };

        if (_audioFiles.TryGetValue(key, out var path))
        {
            return path;
        }

        // 見つからない場合はnullを返す
        _logger.LogWarning($"Station door closing not found: {(isOddPlatform ? "奇数" : "偶数")}番線");
        return null;
    }

    /// <summary>
    /// 車両メロディー取得
    /// </summary>
    public string GetVehicleMelody(Direction direction)
    {
        var key = new AudioKey
        {
            Type = AudioType.VehicleMelody,
            Direction = direction
        };

        if (_audioFiles.TryGetValue(key, out var path))
        {
            return path;
        }

        _logger.LogError($"Vehicle melody not found: {direction}");
        throw new FileNotFoundException($"Required vehicle melody not found: {direction}");
    }

    /// <summary>
    /// 車両ドア締まりますアナウンス取得
    /// </summary>
    public string GetVehicleDoorClosing()
    {
        var key = new AudioKey
        {
            Type = AudioType.VehicleDoorClosing
        };

        if (_audioFiles.TryGetValue(key, out var path))
        {
            return path;
        }

        _logger.LogError("Vehicle door closing not found");
        throw new FileNotFoundException("Required vehicle door closing not found");
    }
    #endregion

    #region プライベートメソッド
    /// <summary>
    /// 検証エラーメッセージ生成
    /// </summary>
    private string GenerateValidationErrorMessage(ValidationResult result)
    {
        var sb = new StringBuilder();
        sb.AppendLine("エラー: プロファイルの読み込みに失敗しました");
        sb.AppendLine();

        if (result.MissingEntries.Any())
        {
            sb.AppendLine("必須エントリー不足:");
            foreach (var entry in result.MissingEntries)
            {
                sb.AppendLine($"- {entry}");
            }
            sb.AppendLine();
        }

        if (result.MissingFiles.Any())
        {
            sb.AppendLine("ファイルが見つかりません:");
            foreach (var file in result.MissingFiles)
            {
                sb.AppendLine($"- {file}");
            }
            sb.AppendLine();
        }

        sb.AppendLine("プロファイルを修正してから再度読み込んでください。");

        return sb.ToString();
    }
    #endregion
}