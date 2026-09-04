using System;
using System.Collections.Generic;
using System.Drawing;
using System.Numerics;
using MirEngine;
using Shared.Rendering;

namespace Shared.Rendering
{
    /// <summary>
    /// HTML5 Canvas 渲染管线：以 CanvasRenderer（main.js 的 mir.cr*）为后端实现 IRenderingPipeline。
    /// 所有绘制目标/纹理用 int 句柄标识（0=主画布，其余=JS 端 Image 或离屏 canvas）。
    /// </summary>
    public sealed class CanvasRenderingPipeline : IRenderingPipeline
    {
        private RenderingPipelineContext _context;
        private int _currentSurfaceId = CanvasRenderer.MainTarget;
        private Size _backBufferSize = new Size(1024, 768);

        private float _opacity = 1f;
        private bool _blending;
        private float _blendRate = 1f;
        private BlendMode _blendMode = BlendMode.NORMAL;
        private float _lineWidth = 1f;
        private TextureFilterMode _textureFilter = TextureFilterMode.Point;

        public string Id => "Canvas";

        public void Initialize(RenderingPipelineContext context)
        {
            _context = context;
            CanvasRenderer.SetTarget(CanvasRenderer.MainTarget);
        }

        public void RunMessageLoop(Action loop)
        {
            // 浏览器端由 main.js 的 requestAnimationFrame 驱动 game.Frame，无需自启循环
        }

        public bool RenderFrame(Action drawScene)
        {
            CanvasRenderer.SetTarget(CanvasRenderer.MainTarget);
            drawScene();
            CanvasRenderer.Flush();
            return true;
        }

        public bool SupportsCachedRenderTargets => false;
        public bool SupportsAtlasTextures => false;
        public bool SupportsBc7Textures => false;

        public void ToggleFullScreen() { }
        public void SetResolution(Size size) => _backBufferSize = size;
        public void SetTargetMonitor(int monitorIndex) { }
        public void CenterOnSelectedMonitor() { }
        public void ResetDevice() { }
        public void OnSceneChanged(bool isGameScene) { }
        public IReadOnlyList<Size> GetSupportedResolutions() => Array.Empty<Size>();
        public IReadOnlyList<GraphicsAdapterInfo> GetGraphicsAdapters(string pipelineId) => Array.Empty<GraphicsAdapterInfo>();

        public Size MeasureText(string text, MirEngine.Font font) => CanvasRenderer.MeasureText(text, font.ToCss(), 0);
        public Size MeasureText(string text, MirEngine.Font font, Size proposedSize)
            => CanvasRenderer.MeasureText(text, font.ToCss(), proposedSize.Width);

        public float GetHorizontalDpi() => 96;

        public Color ConvertHslToRgb(float h, float s, float l)
        {
            h = ((h % 360f) + 360f) % 360f;
            float c = (1f - Math.Abs(2f * l - 1f)) * s;
            float x = c * (1f - Math.Abs(((h / 60f) % 2f) - 1f));
            float m = l - c / 2f;

            float r = 0, g = 0, b = 0;
            if (h < 60) { r = c; g = x; }
            else if (h < 120) { r = x; g = c; }
            else if (h < 180) { g = c; b = x; }
            else if (h < 240) { g = x; b = c; }
            else if (h < 300) { r = x; b = c; }
            else { r = c; b = x; }

            return Color.FromArgb(
                (int)Math.Round((r + m) * 255),
                (int)Math.Round((g + m) * 255),
                (int)Math.Round((b + m) * 255));
        }

        public void SetOpacity(float opacity) => _opacity = opacity;
        public float GetOpacity() => _opacity;
        public void SetBlend(bool enabled, float rate = 1f, BlendMode mode = BlendMode.NORMAL)
        {
            _blending = enabled;
            _blendRate = rate;
            _blendMode = mode;
        }
        public bool IsBlending() => _blending;
        public float GetBlendRate() => _blendRate;
        public BlendMode GetBlendMode() => _blendMode;

        public float GetLineWidth() => _lineWidth;
        public void SetLineWidth(float width) => _lineWidth = width;

        public void DrawLine(IReadOnlyList<LinePoint> points, Color colour)
        {
            if (points == null || points.Count < 2) return;
            int argb = colour.ToArgb();
            for (int i = 0; i + 1 < points.Count; i++)
                CanvasRenderer.DrawLine(points[i].X, points[i].Y, points[i + 1].X, points[i + 1].Y, _lineWidth, argb);
        }

        public void FlushLines() { }

        public void DrawTexture(RenderTexture texture, Rectangle sourceRectangle, RectangleF destinationRectangle, Color colour)
        {
            if (!texture.IsValid) return;
            int id = (int)texture.NativeHandle;
            CanvasRenderer.DrawImage(id, sourceRectangle.X, sourceRectangle.Y, sourceRectangle.Width, sourceRectangle.Height,
                destinationRectangle.X, destinationRectangle.Y, destinationRectangle.Width, destinationRectangle.Height, colour.ToArgb());
        }

        public void DrawTexture(RenderTexture texture, Rectangle? sourceRectangle, Matrix3x2 transform, Vector3 center, Vector3 translation, Color colour)
        {
            if (!texture.IsValid) return;
            int id = (int)texture.NativeHandle;
            Rectangle src = sourceRectangle ?? new Rectangle(0, 0, _backBufferSize.Width, _backBufferSize.Height);
            CanvasRenderer.DrawImage(id, src.X, src.Y, src.Width, src.Height, translation.X, translation.Y, src.Width, src.Height, colour.ToArgb());
        }

        public void BeginSpriteBatch() { }
        public void QueueSprite(RenderTexture texture, Rectangle sourceRectangle, RectangleF destinationRectangle, Color colour)
            => DrawTexture(texture, sourceRectangle, destinationRectangle, colour);
        public void EndSpriteBatch() { }

        public RenderSurface GetCurrentSurface() => RenderSurface.From(_currentSurfaceId);
        public void SetSurface(RenderSurface surface)
        {
            if (!surface.IsValid) return;
            _currentSurfaceId = (int)surface.NativeHandle;
            CanvasRenderer.SetTarget(_currentSurfaceId);
        }
        public RenderSurface GetScratchSurface() => RenderSurface.From(CanvasRenderer.MainTarget);
        public RenderTexture GetScratchTexture() => RenderTexture.From(CanvasRenderer.MainTarget);

        public void ColorFill(RenderSurface surface, Rectangle rectangle, Color colorFill)
        {
            if (!surface.IsValid) return;
            int prev = _currentSurfaceId;
            _currentSurfaceId = (int)surface.NativeHandle;
            CanvasRenderer.SetTarget(_currentSurfaceId);
            CanvasRenderer.FillRect(rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height, colorFill.ToArgb());
            _currentSurfaceId = prev;
            CanvasRenderer.SetTarget(prev);
        }

        public RenderTargetResource CreateRenderTarget(Size size)
        {
            int id = CanvasRenderer.CreateOffscreen(Math.Max(1, size.Width), Math.Max(1, size.Height));
            return RenderTargetResource.From(RenderTexture.From(id), RenderSurface.From(id));
        }
        public void ReleaseRenderTarget(RenderTargetResource renderTarget) { }

        public Size GetBackBufferSize() => _backBufferSize;

        public void Clear(RenderClearFlags flags, Color colour, float z, int stencil, params Rectangle[] regions)
        {
            CanvasRenderer.Clear(colour.R, colour.G, colour.B, colour.A);
        }

        public void FlushSprite() { }

        public void RegisterControlCache(ITextureCacheItem control) { }
        public void UnregisterControlCache(ITextureCacheItem control) { }

        public RenderTexture CreateTexture(Size size, RenderTextureFormat format, RenderTextureUsage usage, RenderTexturePool pool)
        {
            int id = CanvasRenderer.CreateOffscreen(Math.Max(1, size.Width), Math.Max(1, size.Height));
            return RenderTexture.From(id);
        }
        public void ReleaseTexture(RenderTexture texture) { }

        public TextureLock LockTexture(RenderTexture texture, TextureLockMode mode)
        {
            // FillRectangle 已改写为走 ColorFill，不会真正写入像素，这里返回占位锁
            return TextureLock.From(IntPtr.Zero, 0, () => { });
        }

        public void RegisterTextureCache(ITextureCacheItem texture) { }
        public void UnregisterTextureCache(ITextureCacheItem texture) { }

        public void RegisterSoundCache(ISoundCacheItem sound) { }
        public void UnregisterSoundCache(ISoundCacheItem sound) { }
        public IReadOnlyList<ISoundCacheItem> GetRegisteredSoundCaches() => Array.Empty<ISoundCacheItem>();

        public void MemoryClear() { }

        public RenderTexture GetColourPaletteTexture() => RenderTexture.From(CanvasRenderer.MainTarget);
        public byte[] GetColourPaletteData() => Array.Empty<byte>();
        public RenderTexture GetLightTexture() => RenderTexture.From(CanvasRenderer.MainTarget);
        public Size GetLightTextureSize() => new Size(1, 1);
        public RenderTexture GetPoisonTexture() => RenderTexture.From(CanvasRenderer.MainTarget);
        public Size GetPoisonTextureSize() => new Size(1, 1);

        public TextureFilterMode GetTextureFilter() => _textureFilter;
        public void SetTextureFilter(TextureFilterMode mode) => _textureFilter = mode;

        public void Shutdown() { }
    }
}
