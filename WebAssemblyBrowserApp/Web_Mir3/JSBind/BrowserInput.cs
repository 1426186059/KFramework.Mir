using System;
using System.Collections.Generic;
using System.Runtime.InteropServices.JavaScript;
using System.Windows.Forms;

namespace MirEngine;

/// <summary>
/// 浏览器键鼠输入封装（对应 main.js 的 mir.input*）。
/// JS 的 DOM 事件经 mir.inputAttach 注册后，回调本类的 [JSExport] 入口，
/// 翻译成 WinForms 风格的事件参数（System.Windows.Forms.*）并抛出 C# 事件，
/// 由游戏层（MirClientHost）订阅后路由到 DXControl.ActiveScene。
/// 这样裸 DOM 事件处理全部收口到 JS 绑定层，游戏代码只消费强类型的 C# 事件。
/// </summary>
public static partial class BrowserInput
{
    // ---- DOM 监听注册（由 C# 调用，经 JSImport 落到 main.js 的 mir.inputAttach/Detach）----
    [JSImport("mir.inputAttach", "main.js")]
    private static partial void InputAttachImpl();

    [JSImport("mir.inputDetach", "main.js")]
    private static partial void InputDetachImpl();

    public static void Attach() => InputAttachImpl();
    public static void Detach() => InputDetachImpl();

    // ---- C# 事件（游戏层订阅）----
    public static event EventHandler<MouseEventArgs> MouseDown;
    public static event EventHandler<MouseEventArgs> MouseMove;
    public static event EventHandler<MouseEventArgs> MouseUp;
    public static event EventHandler<MouseEventArgs> MouseWheel;
    public static event EventHandler<KeyEventArgs> KeyDown;
    public static event EventHandler<KeyEventArgs> KeyUp;
    public static event EventHandler<KeyPressEventArgs> KeyPress;

    // ---- JS DOM 事件入口（main.js 监听后回调，参数为裸 DOM 值）----
    [JSExport]
    public static void OnMouseDown(int button, int x, int y)
        => MouseDown?.Invoke(null, new MouseEventArgs((System.Windows.Forms.MouseButtons)button, 1, x, y, 0));

    [JSExport]
    public static void OnMouseMove(int x, int y)
        => MouseMove?.Invoke(null, new MouseEventArgs(System.Windows.Forms.MouseButtons.None, 0, x, y, 0));

    [JSExport]
    public static void OnMouseUp(int button, int x, int y)
        => MouseUp?.Invoke(null, new MouseEventArgs((System.Windows.Forms.MouseButtons)button, 1, x, y, 0));

    [JSExport]
    public static void OnMouseWheel(int delta, int x, int y)
        => MouseWheel?.Invoke(null, new MouseEventArgs(System.Windows.Forms.MouseButtons.None, 0, x, y, delta));

    [JSExport]
    public static void OnKeyDown(string key)
    {
        System.Windows.Forms.Keys k = ToKeys(key);
        if (k == System.Windows.Forms.Keys.None) return;
        KeyDown?.Invoke(null, new KeyEventArgs(k));
    }

    [JSExport]
    public static void OnKeyUp(string key)
    {
        System.Windows.Forms.Keys k = ToKeys(key);
        if (k == System.Windows.Forms.Keys.None) return;
        KeyUp?.Invoke(null, new KeyEventArgs(k));
    }

    [JSExport]
    public static void OnKeyPress(string key)
    {
        if (string.IsNullOrEmpty(key)) return;
        KeyPress?.Invoke(null, new KeyPressEventArgs(key[0]));
    }

    // ---- 键名 -> Keys 枚举（复刻原 MirClientHost.ToKeys）----
    private static readonly Dictionary<string, System.Windows.Forms.Keys> SpecialKeys = new Dictionary<string, System.Windows.Forms.Keys>(StringComparer.OrdinalIgnoreCase)
    {
        { "arrowup", System.Windows.Forms.Keys.Up }, { "arrowdown", System.Windows.Forms.Keys.Down },
        { "arrowleft", System.Windows.Forms.Keys.Left }, { "arrowright", System.Windows.Forms.Keys.Right },
        { "escape", System.Windows.Forms.Keys.Escape }, { "enter", System.Windows.Forms.Keys.Return },
        { "return", System.Windows.Forms.Keys.Return }, { "tab", System.Windows.Forms.Keys.Tab },
        { " ", System.Windows.Forms.Keys.Space }, { "spacebar", System.Windows.Forms.Keys.Space },
        { "backspace", System.Windows.Forms.Keys.Back }, { "delete", System.Windows.Forms.Keys.Delete },
        { "shift", System.Windows.Forms.Keys.ShiftKey }, { "control", System.Windows.Forms.Keys.ControlKey },
        { "alt", System.Windows.Forms.Keys.Menu },
        { "f1", System.Windows.Forms.Keys.F1 }, { "f2", System.Windows.Forms.Keys.F2 },
        { "f3", System.Windows.Forms.Keys.F3 }, { "f4", System.Windows.Forms.Keys.F4 },
        { "f5", System.Windows.Forms.Keys.F5 }, { "f6", System.Windows.Forms.Keys.F6 },
        { "f7", System.Windows.Forms.Keys.F7 }, { "f8", System.Windows.Forms.Keys.F8 },
        { "f9", System.Windows.Forms.Keys.F9 }, { "f10", System.Windows.Forms.Keys.F10 },
        { "f11", System.Windows.Forms.Keys.F11 }, { "f12", System.Windows.Forms.Keys.F12 },
    };

    private static System.Windows.Forms.Keys ToKeys(string key)
    {
        if (string.IsNullOrEmpty(key)) return System.Windows.Forms.Keys.None;
        if (SpecialKeys.TryGetValue(key, out System.Windows.Forms.Keys sp)) return sp;
        if (Enum.TryParse<System.Windows.Forms.Keys>(key, true, out System.Windows.Forms.Keys parsed)) return parsed;
        if (key.Length == 1) return (System.Windows.Forms.Keys)char.ToUpperInvariant(key[0]);
        return System.Windows.Forms.Keys.None;
    }
}
