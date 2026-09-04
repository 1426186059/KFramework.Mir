namespace MirEngine;

/// <summary>
/// 替代 System.Windows.Forms.TextFormatFlags 的常用子集（值与原版保持一致，便于 Client 代码原样复用）。
/// 实际文本测量/绘制时只解释 HorizontalCenter/VerticalCenter/Right/Bottom/WordBreak/SingleLine。
/// </summary>
[Flags]
public enum TextFormatFlags
{
    Top = 0,
    Left = 0,
    HorizontalCenter = 0x1,
    Right = 0x2,
    VerticalCenter = 0x4,
    Bottom = 0x8,
    WordBreak = 0x10,
    SingleLine = 0x20,
    NoPrefix = 0x800,
    EndEllipsis = 0x4000,
    WordEllipsis = 0x10000,
    NoPadding = 0x1000000,
    LeftAndRightPadding = 0x2000000,
    HidePrefix = 0x200000,
}
