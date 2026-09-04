using System;
using System.Drawing;
using System.Numerics;
using System.Runtime.InteropServices.JavaScript;

namespace MirEngine;

/// <summary>
/// HTML5 Canvas 渲染后端（JS interop）。每个绘制目标/纹理用 int 句柄标识：
/// 0 = 主画布，&gt;0 = JS 端的 Image（精灵）或离屏 canvas（RenderTarget）。
/// 对应 jsengine/render/canvas/canvas-engine.js（mir.cr* 函数）。
/// </summary>
internal static partial class BrowserCanvas
{
    public const int MainTarget = 0;

    [JSImport("mir.crCreateOffscreen", "main.js")]
    private static partial int CreateOffscreenImpl(int w, int h);

    [JSImport("mir.crSetTarget", "main.js")]
    private static partial void SetTargetImpl(int id);

    [JSImport("mir.crSetBlend", "main.js")]
    private static partial void SetBlendImpl(int mode, float rate, bool enabled);

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
    public static void SetBlendState(int mode, float rate, bool enabled) => SetBlendImpl(mode, rate, enabled);
    public static void Clear(int r, int g, int b, int a) => ClearImpl(r, g, b, a);
    public static void Clear(Color c) => ClearImpl(c.R, c.G, c.B, c.A);
    public static void DrawImage(int tex, int sx, int sy, int sw, int sh, float dx, float dy, float dw, float dh, int colorArgb)
        => DrawImpl(tex, sx, sy, sw, sh, dx, dy, dw, dh, colorArgb);

    /// <summary>矩阵变换绘制：把源矩形当作基础几何 (0,0,sw,sh)，经 2D 仿射矩阵变换后绘制。
    /// 对应 canvas-engine.js 的 mir.crDrawTransform，语义对齐原版 DrawTexture 的 transform/center/translation 契约。</summary>
    [JSImport("mir.crDrawTransform", "main.js")]
    private static partial void DrawTransformImpl(int tex, int sx, int sy, int sw, int sh,
        float m11, float m12, float m21, float m22, float m31, float m32, int colorArgb);

    public static void DrawImageTransform(int tex, int sx, int sy, int sw, int sh, Matrix3x2 transform, int colorArgb)
        => DrawTransformImpl(tex, sx, sy, sw, sh,
            transform.M11, transform.M12, transform.M21, transform.M22, transform.M31, transform.M32, colorArgb);
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

    /// <summary>在离屏 canvas 上把文字渲染成纹理句柄（替代 GDI 的 Bitmap/Graphics/TextRenderer 文本烘焙）。
    /// 对应 jsengine/render/canvas/canvas2d.js 的 mir.drawLabel。</summary>
    [JSImport("mir.drawLabel", "main.js")]
    private static partial void DrawLabelImpl(int handle, int w, int h, string text, string fontCss, int foreArgb, int outlineArgb, int format, int backArgb, int gradTopArgb, int gradBottomArgb, bool gradient);

    public static void DrawLabel(int handle, int w, int h, string text, string fontCss, int foreArgb, int outlineArgb, int format, int backArgb, int gradTopArgb, int gradBottomArgb, bool gradient)
        => DrawLabelImpl(handle, w, h, text, fontCss, foreArgb, outlineArgb, format, backArgb, gradTopArgb, gradBottomArgb, gradient);

    /// <summary>在离屏 canvas 上把文本框内容（背景+选择高亮+文本+光标）渲染成纹理句柄。
    /// 对应 jsengine/render/canvas/canvas2d.js 的 mir.drawTextBox。</summary>
    [JSImport("mir.drawTextBox", "main.js")]
    private static partial void DrawTextBoxImpl(int handle, int w, int h, string text, string fontCss, int foreArgb, int backArgb, int selBackArgb, int caretArgb, int selStart, int selLength, int caretPos, bool caretVisible, bool verticalCenter);

    public static void DrawTextBox(int handle, int w, int h, string text, string fontCss, int foreArgb, int backArgb, int selBackArgb, int caretArgb, int selStart, int selLength, int caretPos, bool caretVisible, bool verticalCenter)
        => DrawTextBoxImpl(handle, w, h, text, fontCss, foreArgb, backArgb, selBackArgb, caretArgb, selStart, selLength, caretPos, caretVisible, verticalCenter);
}
