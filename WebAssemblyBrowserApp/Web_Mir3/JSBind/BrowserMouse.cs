using System;
using System.Runtime.InteropServices.JavaScript;
using System.Windows.Forms;

namespace MirEngine;

/// <summary>
/// 浏览器鼠标输入封装。对应 jsengine/core/mouse.js（mir.mouseAttach / mouseDetach）。
/// JS DOM 的 mousedown / mousemove / mouseup / wheel 经 mir.mouseAttach 注册后，回调本类的 [JSExport] 入口，
/// 翻译成 WinForms 风格 MouseEventArgs 并抛出 C# 事件，由游戏层订阅。
/// </summary>
public static partial class BrowserMouse
{
    [JSImport("mir.mouseAttach", "main.js")]
    private static partial void MouseAttachImpl();

    [JSImport("mir.mouseDetach", "main.js")]
    private static partial void MouseDetachImpl();

    public static void Attach() => MouseAttachImpl();
    public static void Detach() => MouseDetachImpl();

    public static event EventHandler<MouseEventArgs> MouseDown;
    public static event EventHandler<MouseEventArgs> MouseMove;
    public static event EventHandler<MouseEventArgs> MouseUp;
    public static event EventHandler<MouseEventArgs> MouseWheel;

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
}
