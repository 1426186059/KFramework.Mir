namespace MirEngine;

/// <summary>替代 System.Drawing.FontStyle（跨平台，供 DXControl 文本绘制使用）。</summary>
[Flags]
public enum FontStyle
{
    Regular = 0,
    Bold = 1,
    Italic = 2,
    Underline = 4,
    Strikeout = 8,
}
