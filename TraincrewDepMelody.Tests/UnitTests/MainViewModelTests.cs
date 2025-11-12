using FluentAssertions;
using TraincrewDepMelody.Models;
using Xunit;

namespace TraincrewDepMelody.Tests.UnitTests;

/// <summary>
/// MainViewModel および AppSettings.Clone() の単体テスト
/// </summary>
public class MainViewModelTests
{
    #region UT-MVM-001: AppSettings.Clone() - 基本的なコピー

    [Fact]
    [Trait("Category", "Unit")]
    public void UT_MVM_001_AppSettings_Clone_基本的なコピー()
    {
        // Arrange
        var original = new AppSettings
        {
            Volume = 0.5,
            ProfileFile = "test_profile.csv",
            Topmost = TopmostMode.PlayingOnly,
            EnableKeyboard = false,
            InputKey = "F10"
        };

        // Act
        var cloned = original.Clone();

        // Assert
        cloned.Should().NotBeNull();
        cloned.Should().NotBeSameAs(original, "クローンは異なるインスタンスである必要がある");
        cloned.Volume.Should().Be(original.Volume);
        cloned.ProfileFile.Should().Be(original.ProfileFile);
        cloned.Topmost.Should().Be(original.Topmost);
        cloned.EnableKeyboard.Should().Be(original.EnableKeyboard);
        cloned.InputKey.Should().Be(original.InputKey);
    }

    #endregion

    #region UT-MVM-002: AppSettings.Clone() - すべてのプロパティがコピーされる

    [Fact]
    [Trait("Category", "Unit")]
    public void UT_MVM_002_AppSettings_Clone_すべてのプロパティがコピーされる()
    {
        // Arrange
        var original = new AppSettings
        {
            ProfileFile = "profile_test.csv",
            StationDefinition = "stations/test_stations.csv",
            Topmost = TopmostMode.AtStationOnly,
            ShowOnPause = false,
            Volume = 0.6,
            EnableKeyboard = true,
            WindowPosition = new WindowPosition { X = 200, Y = 300 },
            WindowSize = new WindowSize { Width = 400, Height = 500 },
            InputKey = "F8",
            LogLevel = "Debug"
        };

        // Act
        var cloned = original.Clone();

        // Assert
        cloned.Should().NotBeNull();
        cloned.ProfileFile.Should().Be(original.ProfileFile);
        cloned.StationDefinition.Should().Be(original.StationDefinition);
        cloned.Topmost.Should().Be(original.Topmost);
        cloned.ShowOnPause.Should().Be(original.ShowOnPause);
        cloned.Volume.Should().Be(original.Volume);
        cloned.EnableKeyboard.Should().Be(original.EnableKeyboard);
        cloned.WindowPosition.Should().NotBeNull();
        cloned.WindowPosition.X.Should().Be(original.WindowPosition.X);
        cloned.WindowPosition.Y.Should().Be(original.WindowPosition.Y);
        cloned.WindowSize.Should().NotBeNull();
        cloned.WindowSize.Width.Should().Be(original.WindowSize.Width);
        cloned.WindowSize.Height.Should().Be(original.WindowSize.Height);
        cloned.InputKey.Should().Be(original.InputKey);
        cloned.LogLevel.Should().Be(original.LogLevel);
    }

    #endregion

    #region UT-MVM-003: AppSettings.Clone() - ディープコピーの検証（WindowPosition）

    [Fact]
    [Trait("Category", "Unit")]
    public void UT_MVM_003_AppSettings_Clone_ディープコピーの検証_WindowPosition()
    {
        // Arrange
        var original = new AppSettings
        {
            WindowPosition = new WindowPosition { X = 100, Y = 200 }
        };

        // Act
        var cloned = original.Clone();
        cloned.WindowPosition.X = 999;
        cloned.WindowPosition.Y = 888;

        // Assert
        cloned.WindowPosition.Should().NotBeSameAs(original.WindowPosition, "WindowPositionは新しいインスタンスである必要がある");
        original.WindowPosition.X.Should().Be(100, "元のオブジェクトは変更されない");
        original.WindowPosition.Y.Should().Be(200, "元のオブジェクトは変更されない");
        cloned.WindowPosition.X.Should().Be(999);
        cloned.WindowPosition.Y.Should().Be(888);
    }

    #endregion

    #region UT-MVM-004: AppSettings.Clone() - ディープコピーの検証（WindowSize）

    [Fact]
    [Trait("Category", "Unit")]
    public void UT_MVM_004_AppSettings_Clone_ディープコピーの検証_WindowSize()
    {
        // Arrange
        var original = new AppSettings
        {
            WindowSize = new WindowSize { Width = 300, Height = 400 }
        };

        // Act
        var cloned = original.Clone();
        cloned.WindowSize.Width = 777;
        cloned.WindowSize.Height = 666;

        // Assert
        cloned.WindowSize.Should().NotBeSameAs(original.WindowSize, "WindowSizeは新しいインスタンスである必要がある");
        original.WindowSize.Width.Should().Be(300, "元のオブジェクトは変更されない");
        original.WindowSize.Height.Should().Be(400, "元のオブジェクトは変更されない");
        cloned.WindowSize.Width.Should().Be(777);
        cloned.WindowSize.Height.Should().Be(666);
    }

    #endregion

    #region UT-MVM-005: AppSettings.Clone() - クローン変更が元に影響しない

    [Fact]
    [Trait("Category", "Unit")]
    public void UT_MVM_005_AppSettings_Clone_クローン変更が元に影響しない()
    {
        // Arrange
        var original = new AppSettings
        {
            Volume = 0.8,
            ProfileFile = "original.csv",
            Topmost = TopmostMode.Always,
            EnableKeyboard = true,
            InputKey = "Space"
        };

        // Act
        var cloned = original.Clone();
        cloned.Volume = 0.3;
        cloned.ProfileFile = "modified.csv";
        cloned.Topmost = TopmostMode.None;
        cloned.EnableKeyboard = false;
        cloned.InputKey = "End";

        // Assert - 元のオブジェクトは変更されていない
        original.Volume.Should().Be(0.8);
        original.ProfileFile.Should().Be("original.csv");
        original.Topmost.Should().Be(TopmostMode.Always);
        original.EnableKeyboard.Should().BeTrue();
        original.InputKey.Should().Be("Space");

        // クローンは変更されている
        cloned.Volume.Should().Be(0.3);
        cloned.ProfileFile.Should().Be("modified.csv");
        cloned.Topmost.Should().Be(TopmostMode.None);
        cloned.EnableKeyboard.Should().BeFalse();
        cloned.InputKey.Should().Be("End");
    }

    #endregion

    #region UT-MVM-006: AppSettings.Clone() - null InputKeyの処理

    [Fact]
    [Trait("Category", "Unit")]
    public void UT_MVM_006_AppSettings_Clone_null_InputKeyの処理()
    {
        // Arrange
        var original = new AppSettings
        {
            InputKey = null
        };

        // Act
        var cloned = original.Clone();

        // Assert
        cloned.Should().NotBeNull();
        cloned.InputKey.Should().BeNull();
    }

    #endregion

    #region UT-MVM-007: AppSettings.Clone() - 連続したクローン

    [Fact]
    [Trait("Category", "Unit")]
    public void UT_MVM_007_AppSettings_Clone_連続したクローン()
    {
        // Arrange
        var original = new AppSettings
        {
            Volume = 0.5,
            InputKey = "F1"
        };

        // Act
        var clone1 = original.Clone();
        var clone2 = clone1.Clone();
        var clone3 = clone2.Clone();

        clone3.Volume = 0.9;
        clone3.InputKey = "F12";

        // Assert
        original.Volume.Should().Be(0.5);
        original.InputKey.Should().Be("F1");
        clone1.Volume.Should().Be(0.5);
        clone1.InputKey.Should().Be("F1");
        clone2.Volume.Should().Be(0.5);
        clone2.InputKey.Should().Be("F1");
        clone3.Volume.Should().Be(0.9);
        clone3.InputKey.Should().Be("F12");
    }

    #endregion

    #region UT-MVM-008: AppSettings.Clone() - デフォルト値のクローン

    [Fact]
    [Trait("Category", "Unit")]
    public void UT_MVM_008_AppSettings_Clone_デフォルト値のクローン()
    {
        // Arrange
        var original = new AppSettings(); // デフォルト値

        // Act
        var cloned = original.Clone();

        // Assert
        cloned.Should().NotBeNull();
        cloned.Should().NotBeSameAs(original);
        cloned.ProfileFile.Should().Be(original.ProfileFile);
        cloned.StationDefinition.Should().Be(original.StationDefinition);
        cloned.Topmost.Should().Be(original.Topmost);
        cloned.ShowOnPause.Should().Be(original.ShowOnPause);
        cloned.Volume.Should().Be(original.Volume);
        cloned.EnableKeyboard.Should().Be(original.EnableKeyboard);
        cloned.InputKey.Should().Be(original.InputKey);
        cloned.LogLevel.Should().Be(original.LogLevel);
    }

    #endregion

    #region UT-MVM-009: AppSettings.Clone() - 各TopmostMode値

    [Theory]
    [Trait("Category", "Unit")]
    [InlineData(TopmostMode.Always)]
    [InlineData(TopmostMode.PlayingOnly)]
    [InlineData(TopmostMode.AtStationOnly)]
    [InlineData(TopmostMode.None)]
    public void UT_MVM_009_AppSettings_Clone_各TopmostMode値(TopmostMode mode)
    {
        // Arrange
        var original = new AppSettings
        {
            Topmost = mode
        };

        // Act
        var cloned = original.Clone();

        // Assert
        cloned.Topmost.Should().Be(mode);
    }

    #endregion

    #region UT-MVM-010: AppSettings.Clone() - 音量の境界値

    [Theory]
    [Trait("Category", "Unit")]
    [InlineData(0.0)]
    [InlineData(0.5)]
    [InlineData(1.0)]
    [InlineData(0.01)]
    [InlineData(0.99)]
    public void UT_MVM_010_AppSettings_Clone_音量の境界値(double volume)
    {
        // Arrange
        var original = new AppSettings
        {
            Volume = volume
        };

        // Act
        var cloned = original.Clone();

        // Assert
        cloned.Volume.Should().Be(volume);
    }

    #endregion

    #region UT-MVM-011: SettingsWindow シナリオ - 設定変更のシミュレーション

    [Fact]
    [Trait("Category", "Unit")]
    public void UT_MVM_011_SettingsWindow_シナリオ_設定変更のシミュレーション()
    {
        // Arrange - MainViewModel が持つ元の設定
        var originalSettings = new AppSettings
        {
            Volume = 0.8,
            ProfileFile = "original.csv",
            Topmost = TopmostMode.Always,
            EnableKeyboard = true,
            InputKey = "Space"
        };

        // Act - SettingsWindow でクローンを作成して変更
        var settingsWindowCopy = originalSettings.Clone();
        settingsWindowCopy.Volume = 0.5;
        settingsWindowCopy.ProfileFile = "new_profile.csv";
        settingsWindowCopy.Topmost = TopmostMode.None;
        settingsWindowCopy.EnableKeyboard = false;
        settingsWindowCopy.InputKey = "F10";

        // Assert - 元の設定は変更されていない
        originalSettings.Volume.Should().Be(0.8, "SettingsWindowの変更が元の設定に影響しない");
        originalSettings.ProfileFile.Should().Be("original.csv");
        originalSettings.Topmost.Should().Be(TopmostMode.Always);
        originalSettings.EnableKeyboard.Should().BeTrue();
        originalSettings.InputKey.Should().Be("Space");

        // SettingsWindowのコピーは変更されている
        settingsWindowCopy.Volume.Should().Be(0.5);
        settingsWindowCopy.ProfileFile.Should().Be("new_profile.csv");
        settingsWindowCopy.Topmost.Should().Be(TopmostMode.None);
        settingsWindowCopy.EnableKeyboard.Should().BeFalse();
        settingsWindowCopy.InputKey.Should().Be("F10");

        // シミュレーション: ApplySettings で変更を検知できる
        var volumeChanged = Math.Abs(settingsWindowCopy.Volume - originalSettings.Volume) > 0.01;
        var profileChanged = settingsWindowCopy.ProfileFile != originalSettings.ProfileFile;

        volumeChanged.Should().BeTrue("音量の変更を検知できる");
        profileChanged.Should().BeTrue("プロファイルの変更を検知できる");
    }

    #endregion

    #region UT-MVM-012: SettingsWindow シナリオ - キャンセル時の挙動

    [Fact]
    [Trait("Category", "Unit")]
    public void UT_MVM_012_SettingsWindow_シナリオ_キャンセル時の挙動()
    {
        // Arrange - MainViewModel が持つ元の設定
        var originalSettings = new AppSettings
        {
            Volume = 0.8,
            InputKey = "Space"
        };

        // Act - SettingsWindow でクローンを作成して変更
        var settingsWindowCopy = originalSettings.Clone();
        settingsWindowCopy.Volume = 0.3;
        settingsWindowCopy.InputKey = "F5";

        // ユーザーがキャンセルした場合、settingsWindowCopyを破棄

        // Assert - 元の設定は変更されていない
        originalSettings.Volume.Should().Be(0.8, "キャンセル時は元の設定が保持される");
        originalSettings.InputKey.Should().Be("Space");
    }

    #endregion
}
