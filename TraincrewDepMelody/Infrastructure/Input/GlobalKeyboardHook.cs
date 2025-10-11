using System.Runtime.InteropServices;
using System.Windows.Input;

namespace TraincrewDepMelody.Infrastructure.Input;

/// <summary>
/// グローバルキーイベント引数
/// </summary>
public class GlobalKeyEventArgs : EventArgs
{
    public Key Key { get; }
    public bool IsRepeat { get; set; }

    public GlobalKeyEventArgs(Key key, bool isRepeat = false)
    {
        Key = key;
        IsRepeat = isRepeat;
    }
}

/// <summary>
/// グローバルキーボードフック
/// ウィンドウにフォーカスがなくてもキーボード入力を検知する
/// </summary>
public class GlobalKeyboardHook : IDisposable
{
    #region Win32 API
    private const int WH_KEYBOARD_LL = 13;
    private const int WM_KEYDOWN = 0x0100;
    private const int WM_KEYUP = 0x0101;

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string lpModuleName);

    private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);
    #endregion

    #region フィールド
    private readonly LowLevelKeyboardProc _proc;
    private IntPtr _hookID = IntPtr.Zero;
    private bool _disposed = false;
    private readonly HashSet<Key> _pressedKeys = new();
    #endregion

    #region イベント
    public event EventHandler<GlobalKeyEventArgs>? KeyDown;
    public event EventHandler<GlobalKeyEventArgs>? KeyUp;
    #endregion

    #region コンストラクタ
    public GlobalKeyboardHook()
    {
        _proc = HookCallback;
    }
    #endregion

    #region パブリックメソッド
    /// <summary>
    /// フックを開始
    /// </summary>
    public void Start()
    {
        if (_hookID != IntPtr.Zero) return;

        using (var curProcess = System.Diagnostics.Process.GetCurrentProcess())
        using (var curModule = curProcess.MainModule)
        {
            if (curModule != null)
            {
                _hookID = SetWindowsHookEx(WH_KEYBOARD_LL, _proc,
                    GetModuleHandle(curModule.ModuleName), 0);
            }
        }
    }

    /// <summary>
    /// フックを停止
    /// </summary>
    public void Stop()
    {
        if (_hookID != IntPtr.Zero)
        {
            UnhookWindowsHookEx(_hookID);
            _hookID = IntPtr.Zero;
        }
    }
    #endregion

    #region プライベートメソッド
    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            int vkCode = Marshal.ReadInt32(lParam);
            Key key = KeyInterop.KeyFromVirtualKey(vkCode);

            if (wParam == WM_KEYDOWN)
            {
                bool isRepeat = _pressedKeys.Contains(key);
                if (!isRepeat)
                {
                    _pressedKeys.Add(key);
                }
                KeyDown?.Invoke(this, new GlobalKeyEventArgs(key, isRepeat));
            }
            else if (wParam == WM_KEYUP)
            {
                _pressedKeys.Remove(key);
                KeyUp?.Invoke(this, new GlobalKeyEventArgs(key));
            }
        }

        return CallNextHookEx(_hookID, nCode, wParam, lParam);
    }
    #endregion

    #region IDisposable
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            Stop();
            _disposed = true;
        }
    }

    ~GlobalKeyboardHook()
    {
        Dispose(false);
    }
    #endregion
}
