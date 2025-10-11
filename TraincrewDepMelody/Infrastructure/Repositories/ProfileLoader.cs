using System.Globalization;
using System.IO;
using System.Text;
using CsvHelper;
using Microsoft.Extensions.Logging;
using TraincrewDepMelody.Models;

namespace TraincrewDepMelody.Infrastructure.Repositories;

/// <summary>
/// プロファイルローダー
/// </summary>
public class ProfileLoader
{
    #region フィールド
    private readonly ILogger<ProfileLoader> _logger;
    #endregion

    #region コンストラクタ
    public ProfileLoader(ILogger<ProfileLoader> logger)
    {
        _logger = logger;
    }
    #endregion

    #region パブリックメソッド
    /// <summary>
    /// CSVから読み込み
    /// </summary>
    public Dictionary<AudioKey, string> LoadFromCsv(string csvPath)
    {
        var audioFiles = new Dictionary<AudioKey, string>();

        using var reader = new StreamReader(csvPath, Encoding.UTF8);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

        csv.Read();
        csv.ReadHeader();

        while (csv.Read())
        {
            var type = csv.GetField<string>("種別");
            var stationName = csv.GetField<string>("駅名");
            var platform = csv.GetField<string>("番線");
            var directionOrParity = csv.GetField<string>("上下");
            var filePath = csv.GetField<string>("ファイルパス");

            if (string.IsNullOrWhiteSpace(filePath))
            {
                continue;
            }

            var key = CreateAudioKey(type ?? string.Empty, stationName, platform, directionOrParity);
            audioFiles[key] = filePath;

            _logger.LogInformation($"  {type}: {filePath}");
        }

        return audioFiles;
    }

    /// <summary>
    /// バリデーション
    /// </summary>
    public ValidationResult Validate(Dictionary<AudioKey, string> audioFiles)
    {
        var result = new ValidationResult
        {
            IsValid = true,
            MissingEntries = new List<string>(),
            MissingFiles = new List<string>()
        };

        // 必須エントリーチェック
        var requiredEntries = GetRequiredEntries();

        foreach (var entry in requiredEntries)
        {
            if (!audioFiles.ContainsKey(entry))
            {
                result.IsValid = false;
                result.MissingEntries.Add(entry.ToString());
            }
        }

        // ファイル存在チェック
        foreach (var (key, filePath) in audioFiles)
        {
            if (!File.Exists(filePath))
            {
                result.IsValid = false;
                result.MissingFiles.Add(filePath);
            }
        }

        return result;
    }
    #endregion

    #region プライベートメソッド
    /// <summary>
    /// AudioKey作成
    /// </summary>
    private AudioKey CreateAudioKey(string type, string? stationName, string? platform, string? directionOrParity)
    {
        var key = new AudioKey();

        switch (type)
        {
            case "駅メロディー":
                key.Type = AudioType.StationMelody;
                key.StationName = stationName;
                if (!string.IsNullOrWhiteSpace(platform) && int.TryParse(platform, out var p))
                {
                    key.Platform = p;
                }
                break;

            case "駅ドア締まります":
                key.Type = AudioType.StationDoorClosing;
                key.IsOdd = directionOrParity == "奇数";
                break;

            case "車両メロディー":
                key.Type = AudioType.VehicleMelody;
                key.Direction = directionOrParity == "上り" ? Direction.Up : Direction.Down;
                break;

            case "車両ドア締まります":
                key.Type = AudioType.VehicleDoorClosing;
                break;
        }

        return key;
    }

    /// <summary>
    /// 必須エントリー一覧取得
    /// </summary>
    private List<AudioKey> GetRequiredEntries()
    {
        return new List<AudioKey>
        {
            new AudioKey { Type = AudioType.VehicleMelody, Direction = Direction.Up },
            new AudioKey { Type = AudioType.VehicleMelody, Direction = Direction.Down },
            new AudioKey { Type = AudioType.VehicleDoorClosing }
        };
    }
    #endregion
}