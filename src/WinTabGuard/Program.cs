using System.Diagnostics;
using System.Runtime.InteropServices;

namespace WinTabGuard;

internal static class Program
{
    private const int WH_KEYBOARD_LL = 13;
    private const int WM_KEYDOWN = 0x0100;
    private const int WM_KEYUP = 0x0101;
    private const int WM_SYSKEYDOWN = 0x0104;
    private const int WM_SYSKEYUP = 0x0105;
    private const int VK_TAB = 0x09;
    private const int VK_LWIN = 0x5B;
    private const int VK_RWIN = 0x5C;

    private static readonly string AppDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "WinTabGuard");

    private static readonly string LogPath = Path.Combine(AppDirectory, "WinTabGuard.log");

    private static LowLevelKeyboardProc? _keyboardProc;
    private static IntPtr _hookId = IntPtr.Zero;
    private static bool _leftWinDown;
    private static bool _rightWinDown;
    private static bool _suppressWinRelease;

    private static void Main()
    {
        using var currentProcess = Process.GetCurrentProcess();
        var duplicateProcess = Process
            .GetProcessesByName(currentProcess.ProcessName)
            .Any(process => process.Id != currentProcess.Id);

        if (duplicateProcess)
        {
            return;
        }

        Log("Starting.");

        _keyboardProc = KeyboardHookCallback;
        _hookId = InstallKeyboardHook(_keyboardProc);
        if (_hookId == IntPtr.Zero)
        {
            Log($"SetWindowsHookEx failed. Win32Error={Marshal.GetLastWin32Error()}");
            return;
        }

        Log("Keyboard hook installed.");

        try
        {
            while (GetMessage(out var message, IntPtr.Zero, 0, 0) > 0)
            {
                TranslateMessage(ref message);
                DispatchMessage(ref message);
            }
        }
        finally
        {
            if (_hookId != IntPtr.Zero)
            {
                UnhookWindowsHookEx(_hookId);
                Log("Keyboard hook removed.");
            }
        }
    }

    private static IntPtr InstallKeyboardHook(LowLevelKeyboardProc keyboardProc)
    {
        using var currentProcess = Process.GetCurrentProcess();
        using var currentModule = currentProcess.MainModule!;

        return SetWindowsHookEx(
            WH_KEYBOARD_LL,
            keyboardProc,
            GetModuleHandle(currentModule.ModuleName),
            0);
    }

    private static IntPtr KeyboardHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            var virtualKeyCode = Marshal.ReadInt32(lParam);
            var isKeyDown = wParam == WM_KEYDOWN || wParam == WM_SYSKEYDOWN;
            var isKeyUp = wParam == WM_KEYUP || wParam == WM_SYSKEYUP;

            TrackWindowsKeyState(virtualKeyCode, isKeyDown, isKeyUp);

            if (ShouldBlockWinTab(virtualKeyCode, isKeyDown))
            {
                return 1;
            }

            if (ShouldSuppressWindowsKeyRelease(virtualKeyCode, isKeyUp))
            {
                return 1;
            }
        }

        return CallNextHookEx(_hookId, nCode, wParam, lParam);
    }

    private static void TrackWindowsKeyState(int virtualKeyCode, bool isKeyDown, bool isKeyUp)
    {
        if (virtualKeyCode == VK_LWIN)
        {
            _leftWinDown = isKeyDown || (!isKeyUp && _leftWinDown);
        }
        else if (virtualKeyCode == VK_RWIN)
        {
            _rightWinDown = isKeyDown || (!isKeyUp && _rightWinDown);
        }
    }

    private static bool ShouldBlockWinTab(int virtualKeyCode, bool isKeyDown)
    {
        if (virtualKeyCode != VK_TAB || !IsWindowsKeyPressed())
        {
            return false;
        }

        if (isKeyDown)
        {
            _suppressWinRelease = true;
        }

        return true;
    }

    private static bool ShouldSuppressWindowsKeyRelease(int virtualKeyCode, bool isKeyUp)
    {
        if (!isKeyUp || !_suppressWinRelease || (virtualKeyCode != VK_LWIN && virtualKeyCode != VK_RWIN))
        {
            return false;
        }

        if (!_leftWinDown && !_rightWinDown)
        {
            _suppressWinRelease = false;
        }

        return true;
    }

    private static bool IsWindowsKeyPressed()
    {
        return _leftWinDown
            || _rightWinDown
            || (GetAsyncKeyState(VK_LWIN) & 0x8000) != 0
            || (GetAsyncKeyState(VK_RWIN) & 0x8000) != 0;
    }

    private static void Log(string message)
    {
        try
        {
            Directory.CreateDirectory(AppDirectory);
            File.AppendAllText(
                LogPath,
                $"{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss zzz} {message}{Environment.NewLine}");
        }
        catch
        {
            // Logging must not interfere with the keyboard hook.
        }
    }

    private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential)]
    private struct Msg
    {
        public IntPtr hwnd;
        public uint message;
        public UIntPtr wParam;
        public IntPtr lParam;
        public uint time;
        public Point pt;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Point
    {
        public int x;
        public int y;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(
        int idHook,
        LowLevelKeyboardProc lpfn,
        IntPtr hMod,
        uint dwThreadId);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll")]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int vKey);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string? lpModuleName);

    [DllImport("user32.dll")]
    private static extern sbyte GetMessage(out Msg lpMsg, IntPtr hWnd, uint wMsgFilterMin, uint wMsgFilterMax);

    [DllImport("user32.dll")]
    private static extern bool TranslateMessage(ref Msg lpMsg);

    [DllImport("user32.dll")]
    private static extern IntPtr DispatchMessage(ref Msg lpMsg);
}
