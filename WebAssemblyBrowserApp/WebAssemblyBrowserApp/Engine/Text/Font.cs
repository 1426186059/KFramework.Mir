using System;

namespace MirEngine;

/// <summary>
/// 跨平台字体抽象，替代 System.Drawing.Font。
/// 浏览器端没有 Tahoma 等字体，绘制时回退到 sans-serif；Style 仅影响 CSS font 字符串。
/// </summary>
public class Font : IDisposable
{
    public string Name { get; }
    public float Size { get; }
    public FontStyle Style { get; }

    public bool Bold => (Style & FontStyle.Bold) != 0;
    public bool Italic => (Style & FontStyle.Italic) != 0;
    public bool Underline => (Style & FontStyle.Underline) != 0;
    public bool Strikeout => (Style & FontStyle.Strikeout) != 0;

    public Font(string name, float size) : this(name, size, FontStyle.Regular)
    {
    }

    public Font(string name, float size, FontStyle style)
    {
        Name = string.IsNullOrEmpty(name) ? "Tahoma" : name;
        Size = size;
        Style = style;
    }

    /// <summary>转成 canvas 2D 的 CSS font 串，如 "bold 12px Tahoma"。</summary>
    public string ToCss()
    {
        string weight = Bold ? "bold " : string.Empty;
        string slant = Italic ? "italic " : string.Empty;
        return $"{weight}{slant}{Size}px {Name}";
    }

    public void Dispose()
    {
    }

    public override string ToString() => $"{Name}, {Size}";
}
