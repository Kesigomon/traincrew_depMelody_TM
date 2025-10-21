using System.Windows.Input;
using FluentAssertions;
using Xunit;

namespace TraincrewDepMelody.Tests.UnitTests;

/// <summary>
/// キーボード入力関連 単体テスト
/// </summary>
public class KeyboardInputTests
{
    #region ヘルパーメソッド

    /// <summary>
    /// MainWindow のキー解析ロジックを再現
    /// </summary>
    private Key ParseKeyFromString(string keyString)
    {
        if (string.IsNullOrWhiteSpace(keyString))
        {
            return Key.End;
        }

        // Enum.TryParseを使ってキー名を変換
        if (Enum.TryParse<Key>(keyString, true, out var key))
        {
            return key;
        }

        // パースに失敗した場合はデフォルト
        return Key.End;
    }

    #endregion

    #region UT-KI-001: 有効なキー名の解析

    [Theory]
    [Trait("Category", "Unit")]
    [InlineData("Space", Key.Space)]
    [InlineData("End", Key.End)]
    [InlineData("Insert", Key.Insert)]
    [InlineData("Enter", Key.Enter)]
    [InlineData("Tab", Key.Tab)]
    [InlineData("Escape", Key.Escape)]
    [InlineData("F1", Key.F1)]
    [InlineData("F2", Key.F2)]
    [InlineData("F3", Key.F3)]
    [InlineData("F4", Key.F4)]
    [InlineData("F5", Key.F5)]
    [InlineData("F6", Key.F6)]
    [InlineData("F7", Key.F7)]
    [InlineData("F8", Key.F8)]
    [InlineData("F9", Key.F9)]
    [InlineData("F10", Key.F10)]
    [InlineData("F11", Key.F11)]
    [InlineData("F12", Key.F12)]
    public void UT_KI_001_有効なキー名の解析(string keyString, Key expectedKey)
    {
        // Act
        var result = ParseKeyFromString(keyString);

        // Assert
        result.Should().Be(expectedKey);
    }

    #endregion

    #region UT-KI-002: 大文字小文字を区別しない解析

    [Theory]
    [Trait("Category", "Unit")]
    [InlineData("space", Key.Space)]
    [InlineData("SPACE", Key.Space)]
    [InlineData("Space", Key.Space)]
    [InlineData("SpAcE", Key.Space)]
    [InlineData("end", Key.End)]
    [InlineData("END", Key.End)]
    [InlineData("f1", Key.F1)]
    [InlineData("F1", Key.F1)]
    public void UT_KI_002_大文字小文字を区別しない解析(string keyString, Key expectedKey)
    {
        // Act
        var result = ParseKeyFromString(keyString);

        // Assert
        result.Should().Be(expectedKey);
    }

    #endregion

    #region UT-KI-003: 無効なキー名のデフォルト値

    [Theory]
    [Trait("Category", "Unit")]
    [InlineData("InvalidKey")]
    [InlineData("NotAKey")]
    [InlineData("あいうえお")]
    [InlineData("")]
    [InlineData("   ")]
    public void UT_KI_003_無効なキー名のデフォルト値(string? keyString)
    {
        // Act
        var result = ParseKeyFromString(keyString ?? "");

        // Assert
        result.Should().Be(Key.End, "無効なキー名の場合はデフォルト値(Key.End)を返す");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void UT_KI_003B_null文字列のデフォルト値()
    {
        // Act
        var result = ParseKeyFromString(null!);

        // Assert
        result.Should().Be(Key.End, "null文字列の場合はデフォルト値(Key.End)を返す");
    }

    #endregion

    #region UT-KI-004: 数字キーの解析

    [Theory]
    [Trait("Category", "Unit")]
    [InlineData("D0", Key.D0)]
    [InlineData("D1", Key.D1)]
    [InlineData("D2", Key.D2)]
    [InlineData("D3", Key.D3)]
    [InlineData("D4", Key.D4)]
    [InlineData("D5", Key.D5)]
    [InlineData("D6", Key.D6)]
    [InlineData("D7", Key.D7)]
    [InlineData("D8", Key.D8)]
    [InlineData("D9", Key.D9)]
    public void UT_KI_004_数字キーの解析(string keyString, Key expectedKey)
    {
        // Act
        var result = ParseKeyFromString(keyString);

        // Assert
        result.Should().Be(expectedKey);
    }

    #endregion

    #region UT-KI-005: アルファベットキーの解析

    [Theory]
    [Trait("Category", "Unit")]
    [InlineData("A", Key.A)]
    [InlineData("B", Key.B)]
    [InlineData("Z", Key.Z)]
    [InlineData("a", Key.A)]
    [InlineData("z", Key.Z)]
    public void UT_KI_005_アルファベットキーの解析(string keyString, Key expectedKey)
    {
        // Act
        var result = ParseKeyFromString(keyString);

        // Assert
        result.Should().Be(expectedKey);
    }

    #endregion

    #region UT-KI-006: テンキーの解析

    [Theory]
    [Trait("Category", "Unit")]
    [InlineData("NumPad0", Key.NumPad0)]
    [InlineData("NumPad1", Key.NumPad1)]
    [InlineData("NumPad2", Key.NumPad2)]
    [InlineData("NumPad3", Key.NumPad3)]
    [InlineData("NumPad4", Key.NumPad4)]
    [InlineData("NumPad5", Key.NumPad5)]
    [InlineData("NumPad6", Key.NumPad6)]
    [InlineData("NumPad7", Key.NumPad7)]
    [InlineData("NumPad8", Key.NumPad8)]
    [InlineData("NumPad9", Key.NumPad9)]
    public void UT_KI_006_テンキーの解析(string keyString, Key expectedKey)
    {
        // Act
        var result = ParseKeyFromString(keyString);

        // Assert
        result.Should().Be(expectedKey);
    }

    #endregion

    #region UT-KI-007: ナビゲーションキーの解析

    [Theory]
    [Trait("Category", "Unit")]
    [InlineData("Home", Key.Home)]
    [InlineData("End", Key.End)]
    [InlineData("PageUp", Key.PageUp)]
    [InlineData("PageDown", Key.PageDown)]
    [InlineData("Left", Key.Left)]
    [InlineData("Right", Key.Right)]
    [InlineData("Up", Key.Up)]
    [InlineData("Down", Key.Down)]
    public void UT_KI_007_ナビゲーションキーの解析(string keyString, Key expectedKey)
    {
        // Act
        var result = ParseKeyFromString(keyString);

        // Assert
        result.Should().Be(expectedKey);
    }

    #endregion

    #region UT-KI-008: 特殊キーの解析

    [Theory]
    [Trait("Category", "Unit")]
    [InlineData("Delete", Key.Delete)]
    [InlineData("Back", Key.Back)] // Backspace
    [InlineData("Insert", Key.Insert)]
    [InlineData("PrintScreen", Key.PrintScreen)]
    [InlineData("Pause", Key.Pause)]
    [InlineData("Scroll", Key.Scroll)] // ScrollLock
    [InlineData("CapsLock", Key.CapsLock)]
    [InlineData("NumLock", Key.NumLock)]
    public void UT_KI_008_特殊キーの解析(string keyString, Key expectedKey)
    {
        // Act
        var result = ParseKeyFromString(keyString);

        // Assert
        result.Should().Be(expectedKey);
    }

    #endregion

    #region UT-KI-009: 修飾キーの解析

    [Theory]
    [Trait("Category", "Unit")]
    [InlineData("LeftShift", Key.LeftShift)]
    [InlineData("RightShift", Key.RightShift)]
    [InlineData("LeftCtrl", Key.LeftCtrl)]
    [InlineData("RightCtrl", Key.RightCtrl)]
    [InlineData("LeftAlt", Key.LeftAlt)]
    [InlineData("RightAlt", Key.RightAlt)]
    [InlineData("LWin", Key.LWin)]
    [InlineData("RWin", Key.RWin)]
    public void UT_KI_009_修飾キーの解析(string keyString, Key expectedKey)
    {
        // Act
        var result = ParseKeyFromString(keyString);

        // Assert
        result.Should().Be(expectedKey);
    }

    #endregion

    #region UT-KI-010: 記号キーの解析

    [Theory]
    [Trait("Category", "Unit")]
    [InlineData("OemPlus", Key.OemPlus)] // +
    [InlineData("OemMinus", Key.OemMinus)] // -
    [InlineData("OemComma", Key.OemComma)] // ,
    [InlineData("OemPeriod", Key.OemPeriod)] // .
    [InlineData("OemQuestion", Key.OemQuestion)] // ?
    [InlineData("OemTilde", Key.OemTilde)] // ~
    [InlineData("OemOpenBrackets", Key.OemOpenBrackets)] // [
    [InlineData("OemCloseBrackets", Key.OemCloseBrackets)] // ]
    [InlineData("OemPipe", Key.OemPipe)] // |
    [InlineData("OemQuotes", Key.OemQuotes)] // '
    [InlineData("OemBackslash", Key.OemBackslash)] // \
    public void UT_KI_010_記号キーの解析(string keyString, Key expectedKey)
    {
        // Act
        var result = ParseKeyFromString(keyString);

        // Assert
        result.Should().Be(expectedKey);
    }

    #endregion

    #region UT-KI-011: メディアキーの解析

    [Theory]
    [Trait("Category", "Unit")]
    [InlineData("MediaPlayPause", Key.MediaPlayPause)]
    [InlineData("MediaStop", Key.MediaStop)]
    [InlineData("MediaNextTrack", Key.MediaNextTrack)]
    [InlineData("MediaPreviousTrack", Key.MediaPreviousTrack)]
    [InlineData("VolumeUp", Key.VolumeUp)]
    [InlineData("VolumeDown", Key.VolumeDown)]
    [InlineData("VolumeMute", Key.VolumeMute)]
    public void UT_KI_011_メディアキーの解析(string keyString, Key expectedKey)
    {
        // Act
        var result = ParseKeyFromString(keyString);

        // Assert
        result.Should().Be(expectedKey);
    }

    #endregion

    #region UT-KI-012: エッジケース - 空白文字のトリム

    [Theory]
    [Trait("Category", "Unit")]
    [InlineData("  Space  ", Key.Space)]
    [InlineData("\tEnd\t", Key.End)]
    [InlineData("\nF1\n", Key.F1)]
    [InlineData(" Insert ", Key.Insert)]
    public void UT_KI_012_エッジケース_空白文字のトリム(string keyString, Key expectedKey)
    {
        // Act
        var result = ParseKeyFromString(keyString);

        // Assert
        result.Should().Be(expectedKey);
    }

    #endregion

    #region UT-KI-013: 境界値テスト - F13以降のファンクションキー

    [Theory]
    [Trait("Category", "Unit")]
    [InlineData("F13", Key.F13)]
    [InlineData("F14", Key.F14)]
    [InlineData("F15", Key.F15)]
    [InlineData("F16", Key.F16)]
    [InlineData("F17", Key.F17)]
    [InlineData("F18", Key.F18)]
    [InlineData("F19", Key.F19)]
    [InlineData("F20", Key.F20)]
    [InlineData("F21", Key.F21)]
    [InlineData("F22", Key.F22)]
    [InlineData("F23", Key.F23)]
    [InlineData("F24", Key.F24)]
    public void UT_KI_013_境界値テスト_F13以降のファンクションキー(string keyString, Key expectedKey)
    {
        // Act
        var result = ParseKeyFromString(keyString);

        // Assert
        result.Should().Be(expectedKey);
    }

    #endregion

    #region UT-KI-014: ブラウザキーの解析

    [Theory]
    [Trait("Category", "Unit")]
    [InlineData("BrowserBack", Key.BrowserBack)]
    [InlineData("BrowserForward", Key.BrowserForward)]
    [InlineData("BrowserRefresh", Key.BrowserRefresh)]
    [InlineData("BrowserStop", Key.BrowserStop)]
    [InlineData("BrowserSearch", Key.BrowserSearch)]
    [InlineData("BrowserFavorites", Key.BrowserFavorites)]
    [InlineData("BrowserHome", Key.BrowserHome)]
    public void UT_KI_014_ブラウザキーの解析(string keyString, Key expectedKey)
    {
        // Act
        var result = ParseKeyFromString(keyString);

        // Assert
        result.Should().Be(expectedKey);
    }

    #endregion

    #region UT-KI-015: 実際の設定ファイル値のシミュレーション

    [Fact]
    [Trait("Category", "Unit")]
    public void UT_KI_015_実際の設定ファイル値のシミュレーション()
    {
        // Arrange - appsettings.jsonで一般的に使われるキー設定
        var testCases = new Dictionary<string, Key>
        {
            { "Space", Key.Space },
            { "End", Key.End },
            { "Insert", Key.Insert },
            { "F1", Key.F1 },
            { "F5", Key.F5 },
            { "F12", Key.F12 },
            { "Enter", Key.Enter }
        };

        foreach (var testCase in testCases)
        {
            // Act
            var result = ParseKeyFromString(testCase.Key);

            // Assert
            result.Should().Be(testCase.Value,
                $"設定ファイルの値 '{testCase.Key}' は正しく {testCase.Value} に変換されるべき");
        }
    }

    #endregion
}
