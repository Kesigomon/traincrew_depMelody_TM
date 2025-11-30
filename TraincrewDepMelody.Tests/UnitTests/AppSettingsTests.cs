using FluentAssertions;
using Newtonsoft.Json;
using TraincrewDepMelody.Models;
using Xunit;

namespace TraincrewDepMelody.Tests.UnitTests;

/// <summary>
/// AppSettings 単体テスト
/// </summary>
public class AppSettingsTests : IDisposable
{
    private readonly List<string> _tempFiles = new();

    public void Dispose()
    {
        // テンポラリファイルをクリーンアップ
        foreach (var file in _tempFiles)
        {
            if (File.Exists(file))
            {
                File.Delete(file);
            }
        }
    }

    #region ヘルパーメソッド

    private string CreateTempJsonFile(string content)
    {
        var tempFile = Path.GetTempFileName();
        _tempFiles.Add(tempFile);
        File.WriteAllText(tempFile, content);
        return tempFile;
    }

    #endregion

    #region UT-AS-001: デフォルト設定値

    [Fact]
    [Trait("Category", "Unit")]
    public void UT_AS_001_デフォルト設定値()
    {
        // Act
        var settings = new AppSettings();

        // Assert
        settings.InputKey.Should().Be("Space");
        settings.Volume.Should().Be(0.8);
        settings.EnableKeyboard.Should().BeTrue();
        settings.Topmost.Should().Be(TopmostMode.Always);
        settings.ShowOnPause.Should().BeTrue();
    }

    #endregion

    #region UT-AS-002: InputKeyのシリアライズ

    [Fact]
    [Trait("Category", "Unit")]
    public void UT_AS_002_InputKeyのシリアライズ()
    {
        // Arrange
        var settings = new AppSettings
        {
            InputKey = "F5"
        };

        // Act
        var json = JsonConvert.SerializeObject(settings);
        var deserialized = JsonConvert.DeserializeObject<AppSettings>(json);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.InputKey.Should().Be("F5");
    }

    #endregion

    #region UT-AS-003: 各種キー設定のシリアライズ・デシリアライズ

    [Theory]
    [Trait("Category", "Unit")]
    [InlineData("Space")]
    [InlineData("End")]
    [InlineData("Insert")]
    [InlineData("F1")]
    [InlineData("F12")]
    [InlineData("Enter")]
    [InlineData("Tab")]
    [InlineData("Escape")]
    public void UT_AS_003_各種キー設定のシリアライズデシリアライズ(string inputKey)
    {
        // Arrange
        var settings = new AppSettings
        {
            InputKey = inputKey
        };

        // Act
        var json = JsonConvert.SerializeObject(settings);
        var deserialized = JsonConvert.DeserializeObject<AppSettings>(json);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.InputKey.Should().Be(inputKey);
    }

    #endregion

    #region UT-AS-004: JSONファイルからの読み込み

    [Fact]
    [Trait("Category", "Unit")]
    public void UT_AS_004_JSONファイルからの読み込み()
    {
        // Arrange
        var jsonContent = @"{
            ""InputKey"": ""F10"",
            ""Volume"": 0.5,
            ""EnableKeyboard"": true,
            ""Topmost"": ""PlayingOnly""
        }";
        var tempFile = CreateTempJsonFile(jsonContent);

        // Act
        var json = File.ReadAllText(tempFile);
        var settings = JsonConvert.DeserializeObject<AppSettings>(json);

        // Assert
        settings.Should().NotBeNull();
        settings!.InputKey.Should().Be("F10");
        settings.Volume.Should().Be(0.5);
        settings.EnableKeyboard.Should().BeTrue();
        settings.Topmost.Should().Be(TopmostMode.PlayingOnly);
    }

    #endregion

    #region UT-AS-005: InputKeyが未設定の場合のデフォルト値

    [Fact]
    [Trait("Category", "Unit")]
    public void UT_AS_005_InputKeyが未設定の場合のデフォルト値()
    {
        // Arrange
        var jsonContent = @"{
            ""Volume"": 0.8
        }";
        var tempFile = CreateTempJsonFile(jsonContent);

        // Act
        var json = File.ReadAllText(tempFile);
        var settings = JsonConvert.DeserializeObject<AppSettings>(json);

        // Assert
        settings.Should().NotBeNull();
        settings!.InputKey.Should().Be("Space", "デフォルト値が使用される");
    }

    #endregion

    #region UT-AS-006: 完全な設定ファイルの読み書き

    [Fact]
    [Trait("Category", "Unit")]
    public void UT_AS_006_完全な設定ファイルの読み書き()
    {
        // Arrange
        var originalSettings = new AppSettings
        {
            ProfileFile = "profile_test.csv",
            StationDefinition = "stations/stations_test.csv",
            Topmost = TopmostMode.AtStationOnly,
            ShowOnPause = false,
            Volume = 0.6,
            EnableKeyboard = true,
            WindowPosition = new WindowPosition { X = 200, Y = 300 },
            WindowSize = new WindowSize { Width = 400, Height = 500 },
            InputKey = "F8",
            LogLevel = "Debug"
        };

        // Act - シリアライズ
        var json = JsonConvert.SerializeObject(originalSettings, Formatting.Indented);
        var tempFile = CreateTempJsonFile(json);

        // デシリアライズ
        var loadedJson = File.ReadAllText(tempFile);
        var loadedSettings = JsonConvert.DeserializeObject<AppSettings>(loadedJson);

        // Assert
        loadedSettings.Should().NotBeNull();
        loadedSettings!.ProfileFile.Should().Be(originalSettings.ProfileFile);
        loadedSettings.StationDefinition.Should().Be(originalSettings.StationDefinition);
        loadedSettings.Topmost.Should().Be(originalSettings.Topmost);
        loadedSettings.ShowOnPause.Should().Be(originalSettings.ShowOnPause);
        loadedSettings.Volume.Should().Be(originalSettings.Volume);
        loadedSettings.EnableKeyboard.Should().Be(originalSettings.EnableKeyboard);
        loadedSettings.WindowPosition.X.Should().Be(originalSettings.WindowPosition.X);
        loadedSettings.WindowPosition.Y.Should().Be(originalSettings.WindowPosition.Y);
        loadedSettings.WindowSize.Width.Should().Be(originalSettings.WindowSize.Width);
        loadedSettings.WindowSize.Height.Should().Be(originalSettings.WindowSize.Height);
        loadedSettings.InputKey.Should().Be(originalSettings.InputKey);
        loadedSettings.LogLevel.Should().Be(originalSettings.LogLevel);
    }

    #endregion

    #region UT-AS-007: InputKeyにnullが設定された場合

    [Fact]
    [Trait("Category", "Unit")]
    public void UT_AS_007_InputKeyにnullが設定された場合()
    {
        // Arrange
        var jsonContent = @"{
            ""InputKey"": null,
            ""Volume"": 0.8
        }";
        var tempFile = CreateTempJsonFile(jsonContent);

        // Act
        var json = File.ReadAllText(tempFile);
        var settings = JsonConvert.DeserializeObject<AppSettings>(json);

        // Assert
        settings.Should().NotBeNull();
        // デシリアライズ時にnullになるため、nullチェックが必要
        // MainWindow側でnullチェックしてデフォルト値を使用する
        settings!.InputKey.Should().BeNull();
    }

    #endregion

    #region UT-AS-008: InputKeyに空文字列が設定された場合

    [Fact]
    [Trait("Category", "Unit")]
    public void UT_AS_008_InputKeyに空文字列が設定された場合()
    {
        // Arrange
        var jsonContent = @"{
            ""InputKey"": """",
            ""Volume"": 0.8
        }";
        var tempFile = CreateTempJsonFile(jsonContent);

        // Act
        var json = File.ReadAllText(tempFile);
        var settings = JsonConvert.DeserializeObject<AppSettings>(json);

        // Assert
        settings.Should().NotBeNull();
        settings!.InputKey.Should().BeEmpty();
    }

    #endregion

    #region UT-AS-009: 特殊文字を含むキー名

    [Theory]
    [Trait("Category", "Unit")]
    [InlineData("OemPlus")]
    [InlineData("OemMinus")]
    [InlineData("OemQuestion")]
    [InlineData("OemPipe")]
    [InlineData("OemBackslash")]
    public void UT_AS_009_特殊文字を含むキー名(string inputKey)
    {
        // Arrange
        var settings = new AppSettings
        {
            InputKey = inputKey
        };

        // Act
        var json = JsonConvert.SerializeObject(settings);
        var deserialized = JsonConvert.DeserializeObject<AppSettings>(json);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.InputKey.Should().Be(inputKey);
    }

    #endregion

    #region UT-AS-010: JSON形式が不正な場合

    [Fact]
    [Trait("Category", "Unit")]
    public void UT_AS_010_JSON形式が不正な場合()
    {
        // Arrange - カンマ抜けなど明らかに不正な形式
        var invalidJson = @"{
            ""InputKey"": ""F5""
            ""Volume"": 0.8
        }";
        var tempFile = CreateTempJsonFile(invalidJson);

        // Act & Assert
        var json = File.ReadAllText(tempFile);
        Action act = () => JsonConvert.DeserializeObject<AppSettings>(json);
        act.Should().Throw<JsonReaderException>();
    }

    #endregion

    #region UT-AS-011: EnableKeyboardフラグとInputKeyの組み合わせ

    [Theory]
    [Trait("Category", "Unit")]
    [InlineData(true, "F1")]
    [InlineData(true, "Space")]
    [InlineData(false, "F1")]
    [InlineData(false, "Space")]
    public void UT_AS_011_EnableKeyboardフラグとInputKeyの組み合わせ(bool enableKeyboard, string inputKey)
    {
        // Arrange
        var settings = new AppSettings
        {
            EnableKeyboard = enableKeyboard,
            InputKey = inputKey
        };

        // Act
        var json = JsonConvert.SerializeObject(settings);
        var deserialized = JsonConvert.DeserializeObject<AppSettings>(json);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.EnableKeyboard.Should().Be(enableKeyboard);
        deserialized.InputKey.Should().Be(inputKey);
    }

    #endregion

    #region UT-AS-012: 実際のappsettings.jsonフォーマット

    [Fact]
    [Trait("Category", "Unit")]
    public void UT_AS_012_実際のappsettings_jsonフォーマット()
    {
        // Arrange - 実際のappsettings.jsonの内容を再現
        var jsonContent = @"{
  ""ProfileFile"": ""profile_default.csv"",
  ""StationDefinition"": ""stations/stations.csv"",
  ""Topmost"": ""Always"",
  ""ShowOnPause"": true,
  ""Volume"": 0.8,
  ""EnableKeyboard"": true,
  ""WindowPosition"": {
    ""X"": 100,
    ""Y"": 100
  },
  ""WindowSize"": {
    ""Width"": 300,
    ""Height"": 300
  },
  ""InputKey"": ""End"",
  ""LogLevel"": ""Info""
}";
        var tempFile = CreateTempJsonFile(jsonContent);

        // Act
        var json = File.ReadAllText(tempFile);
        var settings = JsonConvert.DeserializeObject<AppSettings>(json);

        // Assert
        settings.Should().NotBeNull();
        settings!.InputKey.Should().Be("End");
        settings.Volume.Should().Be(0.8);
        settings.EnableKeyboard.Should().BeTrue();
        settings.WindowPosition.X.Should().Be(100);
        settings.WindowPosition.Y.Should().Be(100);
    }

    #endregion
}
