using System;
using System.Collections.Generic;
using System.Runtime.InteropServices.JavaScript;
using System.Windows.Forms;

namespace MirEngine;

/// <summary>
/// 浏览器键盘输入封装。对应 jsengine/core/keyboard.js（mir.keyboardAttach / keyboardDetach）。
/// JS DOM 的 keydown / keyup 经 mir.keyboardAttach 注册后，回调本类的 [JSExport] 入口，
/// 翻译成 WinForms 风格 KeyEventArgs / KeyPressEventArgs 并抛出 C# 事件，由游戏层订阅。
/// </summary>
public static partial class BrowserKeyboard
{
    [JSImport("mir.keyboardAttach", "main.js")]
    private static partial void KeyboardAttachImpl();

    [JSImport("mir.keyboardDetach", "main.js")]
    private static partial void KeyboardDetachImpl();

    public static void Attach() => KeyboardAttachImpl();
    public static void Detach() => KeyboardDetachImpl();

    public static event EventHandler<KeyEventArgs> KeyDown;
    public static event EventHandler<KeyEventArgs> KeyUp;
    public static event EventHandler<KeyPressEventArgs> KeyPress;

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

    // ---- 键名 -> Keys 枚举 ----
    private static readonly Dictionary<string, System.Windows.Forms.Keys> SpecialKeys = new Dictionary<string, System.Windows.Forms.Keys>(StringComparer.OrdinalIgnoreCase)
    {
        { "arrowup", System.Windows.Forms.Keys.Up }, { "arrowdown", System.Windows.Forms.Keys.Down },
        { "arrowleft", System.Windows.Forms.Keys.Left }, { "arrowright", System.Windows.Forms.Keys.Right },
        { "escape", System.Windows.Forms.Keys.Escape }, { "enter", System.Windows.Forms.Keys.Return },
        { "return", System.Windows.Forms.Keys.Return }, { "tab", System.Windows.Forms.Keys.Tab },
        { " " , System.Windows.Forms.Keys.Space }, { "spacebar", System.Windows.Forms.Keys.Space },
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
