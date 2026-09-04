using System;
using System.Drawing;
using System.Runtime.InteropServices.JavaScript;

namespace MirEngine;

/// <summary>
/// HTML5 Canvas 渲染后端（JS interop）。每个绘制目标/纹理用 int 句柄标识：
/// 0 = 主画布，&gt;0 = JS 端的 Image（精灵）或离屏 canvas（RenderTarget）。
/// 对应 main.js 的 mir.cr* 函数。
/// </summary>
internal static partial class CanvasRenderer
{
    public const int MainTarget = 0;

    [JSImport("mir.crCreateOffscreen", "main.js")]
    private static partial int CreateOffscreenImpl(int w, int h);

    [JSImport("mir.crSetTarget", "main.js")]
    private static partial void SetTargetImpl(int id);

    [JSImport("mir.crClear", "main.js")]
    private static partial void ClearImpl(int r, int g, int b, int a);

    [JSImport("mir.crDraw", "main.js")]
    private static partial void DrawImpl(int tex, int sx, int sy, int sw, int sh, float dx, float dy, float dw, float dh, int colorArgb);

    [JSImport("mir.crMeasureText", "main.js")]
    private static partial string MeasureTextImpl(string text, string fontCss, int maxWidth);

    [JSImport("mir.crFillRect", "main.js")]
    private static partial void FillRectImpl(int x, int y, int w, int h, int colorArgb);

    [JSImport("mir.crDrawLine", "main.js")]
    private static partial void DrawLineImpl(float x1, float y1, float x2, float y2, float w, int colorArgb);

    [JSImport("mir.crFlush", "main.js")]
    private static partial void FlushImpl();

    // 上传已解码的 RGBA 像素为一个纹理句柄（真实 Zircon 客户端 MirImage 用此把 DXT 解码结果送上 canvas）。
    // 复用 main.js 的 mir.createImage（与 demo 的 MirCanvas.CreateImage 同一 JS 函数，按 id 存入 textures Map）。
    [JSImport("mir.createImage", "main.js")]
    private static partial void UploadImageImpl(int id, byte[] rgba, int w, int h);

    [JSImport("mir.disposeImage", "main.js")]
    private static partial void DisposeImageImpl(int id);

    public static int CreateOffscreen(int w, int h) => CreateOffscreenImpl(w, h);
    public static void UploadImage(int id, byte[] rgba, int w, int h) => UploadImageImpl(id, rgba, w, h);
    public static void DisposeImage(int id) => DisposeImageImpl(id);
    public static void SetTarget(int id) => SetTargetImpl(id);
    public static void Clear(int r, int g, int b, int a) => ClearImpl(r, g, b, a);
    public static void Clear(Color c) => ClearImpl(c.R, c.G, c.B, c.A);
    public static void DrawImage(int tex, int sx, int sy, int sw, int sh, float dx, float dy, float dw, float dh, int colorArgb)
        => DrawImpl(tex, sx, sy, sw, sh, dx, dy, dw, dh, colorArgb);
    public static void FillRect(int x, int y, int w, int h, int colorArgb) => FillRectImpl(x, y, w, h, colorArgb);
    public static void DrawLine(float x1, float y1, float x2, float y2, float w, int colorArgb) => DrawLineImpl(x1, y1, x2, y2, w, colorArgb);
    public static void Flush() => FlushImpl();

    /// <summary>用 canvas 2D 的文本测量（fontCss 形如 "bold 12px Tahoma"）。</summary>
    public static Size MeasureText(string text, string fontCss, int maxWidth)
    {
        if (string.IsNullOrEmpty(text)) return Size.Empty;

        string s = MeasureTextImpl(text, fontCss, maxWidth);
        int i = s.IndexOf(',');
        if (i < 0) return new Size(0, 12);

        int w = int.Parse(s.AsSpan(0, i));
        int h = int.Parse(s.AsSpan(i + 1));
        return new Size(w, h);
    }
}
