namespace MirEngine;

/// <summary>替代 System.Windows.Forms.MouseButtons（值对齐 WinForms）。</summary>
[Flags]
public enum MouseButtons
{
    None = 0,
    Left = 0x100000,
    Right = 0x200000,
    Middle = 0x400000,
    XButton1 = 0x800000,
    XButton2 = 0x1000000,
}
