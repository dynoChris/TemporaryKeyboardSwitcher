using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Timer = System.Windows.Forms.Timer;

internal static class Program
{
    [STAThread]
    static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetHighDpiMode(HighDpiMode.SystemAware);
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new TrayApp());
    }
}

public class TrayApp : ApplicationContext
{
    // ---- WinAPI consts ----
    private const int WH_KEYBOARD_LL = 13;
    private const int WM_KEYDOWN = 0x0100;
    private const int WM_KEYUP = 0x0101;
    private const int WM_SYSKEYDOWN = 0x0104;
    private const int WM_SYSKEYUP = 0x0105;

    private const uint MOD_CONTROL = 0x0002;
    private const uint MOD_SHIFT = 0x0004;
    private const int WM_HOTKEY = 0x0312;

    private const uint KEYEVENTF_UNICODE = 0x0004;
    private const uint KEYEVENTF_KEYUP = 0x0002;

    private const int VK_SHIFT = 0x10;
    private const int VK_CAPITAL = 0x14;

    private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential)]
    private struct KBDLLHOOKSTRUCT
    {
        public uint vkCode;
        public uint scanCode;
        public uint flags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    private const uint WM_CHAR = 0x0102;

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);
    [DllImport("user32.dll")]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);
    [DllImport("user32.dll")]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);
    [DllImport("kernel32.dll")]
    private static extern IntPtr GetModuleHandle(string? lpModuleName);

    [DllImport("user32.dll")]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);
    [DllImport("user32.dll")]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    [StructLayout(LayoutKind.Sequential)]
    private struct INPUT { public uint type; public InputUnion U; }
    [StructLayout(LayoutKind.Explicit)]
    private struct InputUnion { [FieldOffset(0)] public KEYBDINPUT ki; }
    [StructLayout(LayoutKind.Sequential)]
    private struct KEYBDINPUT
    {
        public ushort wVk; public ushort wScan; public uint dwFlags; public uint time; public IntPtr dwExtraInfo;
    }
    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);
    [DllImport("user32.dll")]
    private static extern short GetKeyState(int nVirtKey);

    // ---- fields ----
    private readonly NotifyIcon _tray;
    private readonly ContextMenuStrip _menu;
    private readonly Timer _dummy;
    private IntPtr _hook = IntPtr.Zero;
    private LowLevelKeyboardProc? _proc;
    private bool _overlayEnabled = false;
    private readonly int _hotkeyId = 1;
    private readonly MessageWindow _msgWindow;

    // VK -> Ukrainian letter (lowercase)
    private static readonly Dictionary<Keys, string> MapUaLower = new()
    {
        { Keys.Q, "й" }, { Keys.W, "ц" }, { Keys.E, "у" }, { Keys.R, "к" }, { Keys.T, "е" },
        { Keys.Y, "н" }, { Keys.U, "г" }, { Keys.I, "ш" }, { Keys.O, "щ" }, { Keys.P, "з" },
        { Keys.OemOpenBrackets, "х" }, { Keys.OemCloseBrackets, "ї" },
        { Keys.A, "ф" }, { Keys.S, "і" }, { Keys.D, "в" }, { Keys.F, "а" }, { Keys.G, "п" },
        { Keys.H, "р" }, { Keys.J, "о" }, { Keys.K, "л" }, { Keys.L, "д" },
        { Keys.Oem1, "ж" }, { Keys.Oem7, "є" },
        { Keys.Z, "я" }, { Keys.X, "ч" }, { Keys.C, "с" }, { Keys.V, "м" }, { Keys.B, "и" },
        { Keys.N, "т" }, { Keys.M, "ь" }, { Keys.Oemcomma, "б" }, { Keys.OemPeriod, "ю" },
        { Keys.OemQuestion, "." },
        { Keys.Oemtilde, "ґ" },
    };

    public TrayApp()
    {
        // Tray menu
        _menu = new ContextMenuStrip();
        var toggleItem = new ToolStripMenuItem("Enable Ukrainian overlay") { Checked = _overlayEnabled, CheckOnClick = true };
        toggleItem.CheckedChanged += (s, e) => SetOverlayEnabled(toggleItem.Checked, userToggle: true);
        _menu.Items.Add(toggleItem);
        _menu.Items.Add(new ToolStripSeparator());
        _menu.Items.Add("Exit", null, (s, e) => ExitThread());

        _tray = new NotifyIcon
        {
            Icon = SystemIcons.Information,
            Visible = true,
            Text = "UA Overlay (Ctrl+Shift+U)",
            ContextMenuStrip = _menu
        };
        _tray.MouseClick += (s, e) => { if (e.Button == MouseButtons.Left) SetOverlayEnabled(!_overlayEnabled, userToggle: true); };

        // Hidden window for WM_HOTKEY
        _msgWindow = new MessageWindow();
        _msgWindow.ProcessHotkey = OnHotkeyMessage;
        if (!RegisterHotKey(_msgWindow.Handle, _hotkeyId, MOD_CONTROL | MOD_SHIFT, (uint)Keys.U))
            MessageBox.Show("Failed to register hotkey Ctrl+Shift+U", "UA Overlay", MessageBoxButtons.OK, MessageBoxIcon.Error);

        // Low-level keyboard hook
        _proc = HookProc;
        using var curProcess = Process.GetCurrentProcess();
        using var curModule = curProcess.MainModule!;
        _hook = SetWindowsHookEx(WH_KEYBOARD_LL, _proc, IntPtr.Zero, 0);
        if (_hook == IntPtr.Zero)
            MessageBox.Show("Failed to install keyboard hook", "UA Overlay", MessageBoxButtons.OK, MessageBoxIcon.Error);

        // Keep context alive
        _dummy = new Timer { Interval = 60000 };
        _dummy.Tick += (_, __) => { };
        _dummy.Start();
    }

    protected override void ExitThreadCore()
    {
        try
        {
            if (_hook != IntPtr.Zero) UnhookWindowsHookEx(_hook);
            UnregisterHotKey(_msgWindow.Handle, _hotkeyId);
            _tray.Visible = false; _tray.Dispose(); _menu.Dispose(); _dummy.Dispose();
            _msgWindow.DestroyHandle();
        }
        catch { }
        base.ExitThreadCore();
    }

    private void OnHotkeyMessage() => SetOverlayEnabled(!_overlayEnabled, userToggle: true);

    private void SetOverlayEnabled(bool enabled, bool userToggle)
    {
        _overlayEnabled = enabled;
        var mi = (ToolStripMenuItem)_menu.Items[0];
        mi.Checked = enabled;
        _tray.Icon = enabled ? SystemIcons.Shield : SystemIcons.Information;
        _tray.Text = enabled ? "UA Overlay: ON (Ctrl+Shift+U)" : "UA Overlay: OFF (Ctrl+Shift+U)";
        if (userToggle)
        {
            _tray.BalloonTipTitle = "UA Overlay";
            _tray.BalloonTipText = enabled ? "Ukrainian overlay enabled" : "Overlay disabled";
            _tray.ShowBalloonTip(1000);
        }
    }

    private static bool IsInjected(uint flags) => (flags & 0x10) != 0;

    // Send text directly to the foreground window via WM_CHAR (no SendInput, no clipboard)
    private static bool InjectStringToForeground(string s)
    {
        var hWnd = GetForegroundWindow();
        if (hWnd == IntPtr.Zero) return false;

        foreach (var ch in s)
            PostMessage(hWnd, WM_CHAR, (IntPtr)ch, IntPtr.Zero);

        return true;
    }

    private IntPtr HookProc(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 && _overlayEnabled)
        {
            int msg = (int)wParam;
            bool isDown = msg == WM_KEYDOWN || msg == WM_SYSKEYDOWN;
            bool isUp = msg == WM_KEYUP || msg == WM_SYSKEYUP;

            var data = Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam);
            if (IsInjected(data.flags)) return CallNextHookEx(_hook, nCode, wParam, lParam);

            var vk = (Keys)data.vkCode;

            // Let the hotkey Ctrl+Shift+U pass through
            if ((Control.ModifierKeys & Keys.Control) == Keys.Control &&
                (Control.ModifierKeys & Keys.Shift) == Keys.Shift && vk == Keys.U)
                return CallNextHookEx(_hook, nCode, wParam, lParam);

            // Do not interfere with other shortcuts
            bool ctrl = (Control.ModifierKeys & Keys.Control) == Keys.Control;
            bool alt = (Control.ModifierKeys & Keys.Alt) == Keys.Alt;
            if (ctrl || alt)
                return CallNextHookEx(_hook, nCode, wParam, lParam);

            if ((isDown || isUp) && MapUaLower.TryGetValue(vk, out var lower))
            {
                if (isDown)
                {
                    bool shift = (GetKeyState(VK_SHIFT) & 0x8000) != 0;
                    bool caps = (GetKeyState(VK_CAPITAL) & 0x0001) != 0;
                    bool upper = shift ^ caps;

                    string text = upper ? ToUpperUa(lower) : lower;

                    bool ok = InjectStringToForeground(text);
                    if (ok) return (IntPtr)1; // suppress the physical key if we injected
                }
            }
        }
        return CallNextHookEx(_hook, nCode, wParam, lParam);
    }

    private static string ToUpperUa(string s) =>
        s.ToUpper(new System.Globalization.CultureInfo("uk-UA"));

    // Hidden message window for WM_HOTKEY
    private class MessageWindow : NativeWindow
    {
        public Action? ProcessHotkey;
        public MessageWindow() { CreateHandle(new CreateParams()); }
        protected override void WndProc(ref Message m)
        { if (m.Msg == WM_HOTKEY) ProcessHotkey?.Invoke(); base.WndProc(ref m); }
    }
}
