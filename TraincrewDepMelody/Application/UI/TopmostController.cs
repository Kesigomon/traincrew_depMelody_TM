using System.Windows;
using TraincrewDepMelody.Models;

namespace TraincrewDepMelody.Application.UI;

/// <summary>
/// Topmost制御クラス
/// </summary>
public class TopmostController
{
    private readonly Window _window;
    private TopmostMode _mode;
    private bool _showOnPause;

    /// <summary>
    /// コンストラクタ
    /// </summary>
    /// <param name="window">制御対象のウィンドウ</param>
    public TopmostController(Window window)
    {
        _window = window;
        _mode = TopmostMode.Always;
        _showOnPause = true;
    }

    /// <summary>
    /// Topmostモードを設定
    /// </summary>
    /// <param name="mode">Topmostモード</param>
    public void SetMode(TopmostMode mode)
    {
        _mode = mode;
    }

    /// <summary>
    /// ポーズ時の表示設定
    /// </summary>
    /// <param name="showOnPause">ポーズ時に表示するか</param>
    public void SetShowOnPause(bool showOnPause)
    {
        _showOnPause = showOnPause;
    }

    /// <summary>
    /// 状態に応じてTopmost属性を更新
    /// </summary>
    /// <param name="state">アプリケーション状態</param>
    public void Update(ApplicationState state)
    {
        bool shouldBeTopmost = _mode switch
        {
            TopmostMode.Always => true,
            TopmostMode.PlayingOnly => state.GameStatus == GameStatus.Running ||
                                       (_showOnPause && state.GameStatus == GameStatus.Paused),
            TopmostMode.AtStationOnly => state.IsAtStation &&
                                         (state.GameStatus == GameStatus.Running ||
                                          (_showOnPause && state.GameStatus == GameStatus.Paused)),
            TopmostMode.None => false,
            _ => false
        };

        _window.Topmost = shouldBeTopmost;
    }
}
