using System.IO;
using Newtonsoft.Json;
using TraincrewDepMelody.Models;

namespace TraincrewDepMelody.Infrastructure.Settings;

/// <summary>
/// 設定管理クラス
/// </summary>
public class SettingsManager
{
    private const string SettingsFileName = "appsettings.json";
    private AppSettings _settings;

    public AppSettings Settings => _settings;

    public SettingsManager()
    {
        _settings = new AppSettings();
    }

    public void Load()
    {
        try
        {
            if (File.Exists(SettingsFileName))
            {
                var json = File.ReadAllText(SettingsFileName);
                _settings = JsonConvert.DeserializeObject<AppSettings>(json) ?? new AppSettings();
            }
            else
            {
                // デフォルト設定を使用
                _settings = new AppSettings();
                Save(); // 設定ファイルを作成
            }
        }
        catch
        {
            // エラー時はデフォルト設定使用
            _settings = new AppSettings();
        }
    }

    public void Save()
    {
        try
        {
            var json = JsonConvert.SerializeObject(_settings, Formatting.Indented);
            File.WriteAllText(SettingsFileName, json);
        }
        catch
        {
            // エラーログ出力は呼び出し側で行う
        }
    }
}