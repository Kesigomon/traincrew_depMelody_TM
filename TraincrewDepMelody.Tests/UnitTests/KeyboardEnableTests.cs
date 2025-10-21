using FluentAssertions;
using TraincrewDepMelody.Models;
using Xunit;

namespace TraincrewDepMelody.Tests.UnitTests;

/// <summary>
/// キーボード有効化/無効化機能のテスト
/// </summary>
public class KeyboardEnableTests
{
    #region UT-KE-001: EnableKeyboard設定のデフォルト値

    [Fact]
    [Trait("Category", "Unit")]
    public void UT_KE_001_EnableKeyboard設定のデフォルト値()
    {
        // Arrange & Act
        var settings = new AppSettings();

        // Assert
        settings.EnableKeyboard.Should().BeTrue("デフォルトではキーボード入力が有効化されている");
    }

    #endregion

    #region UT-KE-002: EnableKeyboardをfalseに設定

    [Fact]
    [Trait("Category", "Unit")]
    public void UT_KE_002_EnableKeyboardをfalseに設定()
    {
        // Arrange
        var settings = new AppSettings
        {
            EnableKeyboard = false
        };

        // Act & Assert
        settings.EnableKeyboard.Should().BeFalse();
    }

    #endregion

    #region UT-KE-003: EnableKeyboardをtrueに設定

    [Fact]
    [Trait("Category", "Unit")]
    public void UT_KE_003_EnableKeyboardをtrueに設定()
    {
        // Arrange
        var settings = new AppSettings
        {
            EnableKeyboard = true
        };

        // Act & Assert
        settings.EnableKeyboard.Should().BeTrue();
    }

    #endregion

    #region UT-KE-004: EnableKeyboardとInputKeyの組み合わせ

    [Theory]
    [Trait("Category", "Unit")]
    [InlineData(true, "Space")]
    [InlineData(true, "F1")]
    [InlineData(false, "Space")]
    [InlineData(false, "F1")]
    public void UT_KE_004_EnableKeyboardとInputKeyの組み合わせ(bool enableKeyboard, string inputKey)
    {
        // Arrange
        var settings = new AppSettings
        {
            EnableKeyboard = enableKeyboard,
            InputKey = inputKey
        };

        // Act & Assert
        settings.EnableKeyboard.Should().Be(enableKeyboard);
        settings.InputKey.Should().Be(inputKey);
    }

    #endregion

    #region UT-KE-005: EnableKeyboardの切り替え

    [Fact]
    [Trait("Category", "Unit")]
    public void UT_KE_005_EnableKeyboardの切り替え()
    {
        // Arrange
        var settings = new AppSettings
        {
            EnableKeyboard = true
        };

        // Act - falseに切り替え
        settings.EnableKeyboard = false;

        // Assert
        settings.EnableKeyboard.Should().BeFalse();

        // Act - trueに戻す
        settings.EnableKeyboard = true;

        // Assert
        settings.EnableKeyboard.Should().BeTrue();
    }

    #endregion

    #region UT-KE-006: EnableKeyboardが無効の時、InputKeyは保持される

    [Fact]
    [Trait("Category", "Unit")]
    public void UT_KE_006_EnableKeyboardが無効の時InputKeyは保持される()
    {
        // Arrange
        var settings = new AppSettings
        {
            EnableKeyboard = true,
            InputKey = "F5"
        };

        // Act - キーボードを無効化
        settings.EnableKeyboard = false;

        // Assert - InputKeyは保持される
        settings.InputKey.Should().Be("F5", "キーボードを無効化してもInputKeyの値は保持される");
    }

    #endregion

    #region UT-KE-007: 複数の設定変更シナリオ

    [Fact]
    [Trait("Category", "Unit")]
    public void UT_KE_007_複数の設定変更シナリオ()
    {
        // Arrange
        var settings = new AppSettings();

        // 初期状態
        settings.EnableKeyboard.Should().BeTrue();
        settings.InputKey.Should().Be("Space");

        // Act & Assert - シナリオ1: キーを変更
        settings.InputKey = "F1";
        settings.EnableKeyboard.Should().BeTrue();
        settings.InputKey.Should().Be("F1");

        // Act & Assert - シナリオ2: キーボードを無効化
        settings.EnableKeyboard = false;
        settings.EnableKeyboard.Should().BeFalse();
        settings.InputKey.Should().Be("F1", "無効化してもキー設定は保持");

        // Act & Assert - シナリオ3: 無効化中にキーを変更
        settings.InputKey = "F12";
        settings.EnableKeyboard.Should().BeFalse();
        settings.InputKey.Should().Be("F12");

        // Act & Assert - シナリオ4: 再度有効化
        settings.EnableKeyboard = true;
        settings.EnableKeyboard.Should().BeTrue();
        settings.InputKey.Should().Be("F12", "有効化後も最後のキー設定が保持される");
    }

    #endregion

    #region UT-KE-008: EnableKeyboardと他の設定の独立性

    [Fact]
    [Trait("Category", "Unit")]
    public void UT_KE_008_EnableKeyboardと他の設定の独立性()
    {
        // Arrange
        var settings = new AppSettings
        {
            EnableKeyboard = true,
            Volume = 0.5,
            Topmost = TopmostMode.PlayingOnly,
            ShowOnPause = false
        };

        // Act - キーボードのみ無効化
        settings.EnableKeyboard = false;

        // Assert - 他の設定は影響を受けない
        settings.EnableKeyboard.Should().BeFalse();
        settings.Volume.Should().Be(0.5);
        settings.Topmost.Should().Be(TopmostMode.PlayingOnly);
        settings.ShowOnPause.Should().BeFalse();
    }

    #endregion

    #region UT-KE-009: 設定のシリアライズ時にEnableKeyboardが保持される

    [Theory]
    [Trait("Category", "Unit")]
    [InlineData(true)]
    [InlineData(false)]
    public void UT_KE_009_設定のシリアライズ時にEnableKeyboardが保持される(bool enableKeyboard)
    {
        // Arrange
        var settings = new AppSettings
        {
            EnableKeyboard = enableKeyboard,
            InputKey = "F5"
        };

        // Act - シリアライズ・デシリアライズ
        var json = Newtonsoft.Json.JsonConvert.SerializeObject(settings);
        var deserialized = Newtonsoft.Json.JsonConvert.DeserializeObject<AppSettings>(json);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.EnableKeyboard.Should().Be(enableKeyboard);
        deserialized.InputKey.Should().Be("F5");
    }

    #endregion

    #region UT-KE-010: 実際の使用シナリオ - ユーザーがキーボード入力を一時的に無効化

    [Fact]
    [Trait("Category", "Unit")]
    public void UT_KE_010_実際の使用シナリオ_ユーザーがキーボード入力を一時的に無効化()
    {
        // Arrange - ユーザーがキーボード入力を使用している状態
        var settings = new AppSettings
        {
            EnableKeyboard = true,
            InputKey = "Space"
        };

        settings.EnableKeyboard.Should().BeTrue("初期状態では有効");

        // Act - ユーザーが他の作業をするため一時的に無効化
        settings.EnableKeyboard = false;

        // Assert
        settings.EnableKeyboard.Should().BeFalse("一時的に無効化された");
        settings.InputKey.Should().Be("Space", "キー設定は保持される");

        // Act - 作業が終わって再度有効化
        settings.EnableKeyboard = true;

        // Assert
        settings.EnableKeyboard.Should().BeTrue("再度有効化された");
        settings.InputKey.Should().Be("Space", "以前のキー設定で使用可能");
    }

    #endregion

    #region UT-KE-011: EnableKeyboardがfalseの場合の動作確認

    [Fact]
    [Trait("Category", "Unit")]
    public void UT_KE_011_EnableKeyboardがfalseの場合の動作確認()
    {
        // Arrange
        var settings = new AppSettings
        {
            EnableKeyboard = false,
            InputKey = "F10"
        };

        // Assert - 設定が正しく保存されている
        settings.EnableKeyboard.Should().BeFalse();
        settings.InputKey.Should().Be("F10");

        // この状態では、MainWindowのOnGlobalKeyDown/OnGlobalKeyUpで
        // キーイベントが無視されることを期待
        // (実際のキーイベント処理はMainWindowで行われるため、
        //  ここでは設定値の検証のみ)
    }

    #endregion

    #region UT-KE-012: EnableKeyboardのデフォルト値が変更されていないことを確認

    [Fact]
    [Trait("Category", "Unit")]
    public void UT_KE_012_EnableKeyboardのデフォルト値が変更されていないことを確認()
    {
        // Arrange - デフォルトコンストラクタで作成
        var settings1 = new AppSettings();
        var settings2 = new AppSettings();

        // Assert - 複数のインスタンスで同じデフォルト値
        settings1.EnableKeyboard.Should().BeTrue();
        settings2.EnableKeyboard.Should().BeTrue();
    }

    #endregion
}
