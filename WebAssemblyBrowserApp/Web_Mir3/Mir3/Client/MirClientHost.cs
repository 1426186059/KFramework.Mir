using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices.JavaScript;
using System.Windows.Forms;

using Client.Controls;
using Client.Envir;
using Client.Scenes;
using Client.Scenes.Views;
using Library;
using Shared.Envir;
using Shared.Rendering;

namespace Client;

/// <summary>
/// WASM 版 CMain：等价于 Unity 移植版的 Mir3UnityAdapter/Unity/CMain.cs，
/// 把真实 Zircon 客户端的启动 / 主循环 / 输入桥接进浏览器。
/// 与 Unity 的 MonoBehaviour 生命周期不同，这里由 main.js 的 requestAnimationFrame
/// 每帧调 Frame()，并把鼠标/键盘事件路由到 DXControl.ActiveScene。
/// </summary>
public static partial class MirClientHost
{
    private static bool _ready;

    [JSImport("mir.getBytes", "main.js")]
    private static partial byte[] GetBytesImpl(string url);

    [JSImport("mir.log", "main.js")]
    private static partial void LogImpl(string message);

    [JSImport("mir.initAudio", "main.js")]
    private static partial void InitAudioImpl();

    [JSImport("mir.playSound", "main.js")]
    private static partial int PlaySoundImpl(string url, int volume, bool loop);

    [JSImport("mir.stopSound", "main.js")]
    private static partial void StopSoundImpl(int id);

    [JSImport("mir.stopAllSounds", "main.js")]
    private static partial void StopAllSoundsImpl();

    [JSImport("mir.setSoundVolume", "main.js")]
    private static partial void SetSoundVolumeImpl(int id, int volume);

    private static byte[] GetBytes(string url) => GetBytesImpl(url);
    private static void MirLog(string message) => LogImpl(message);

    public static void InitAudio() => InitAudioImpl();
    public static int PlaySound(string url, int volume, bool loop) => PlaySoundImpl(url, volume, loop);
    public static void StopSound(int id) => StopSoundImpl(id);
    public static void StopAllSounds() => StopAllSoundsImpl();
    public static void SetSoundVolume(int id, int volume) => SetSoundVolumeImpl(id, volume);

    // ===================== 生命周期 =====================

    /// <summary>
    /// 预下载清单：直接复用 Libraries.GetUrl 把 Libraries.LibraryList 全量规范化为
    /// Web URL（MyRes/Data/MapData/Tilesc.Zl 等，已处理 "Map Data"→"MapData" 空格与 Data/ 前缀）。
    /// 缺失的文件由 main.js 容错跳过，因此把真实 Data\ 全套 .Zl 丢进 wwwroot/MyRes/Data/ 即可自动全加载。
    /// </summary>
    [JSExport]
    public static string[] GetAssetList()
    {
        var urls = new List<string>();
        foreach (LibraryFile file in Libraries.LibraryList.Keys)
        {
            string url = Libraries.GetUrl(file);
            if (!string.IsNullOrEmpty(url))
                urls.Add(url);
        }
        return urls.ToArray();
    }

    [JSExport]
    public static void Init()
    {
        try
        {
            ConfigReader.Load(Assembly.GetAssembly(typeof(Config)));
            CEnvir.LoadLanguage();

            MirLibrary.GetNow = () => CEnvir.Now;
            MirLibrary.GetCacheDuration = () => Config.CacheDuration;
            MirLibrary.GetUseZlAtlasPages = () => Config.UseZlAtlasPages;
            MirLibrary.DrawCounted = () => CEnvir.DPSCounter++;
            MirLibrary.GetBytesFromUrl = GetBytes;

            LoadLibraries();

            CEnvir.Init(new string[0]);

            CEnvir.Target = new TargetForm();
            CEnvir.Target.ClientSize = new Size(1024, 768);
            Config.GameSize = new Size(1024, 768);
            Config.LimitFPS = false; // 浏览器主线程不可 Sleep
            Config.MapPath = "MyRes/Map/";
            MapControl.MapBytesLoader = f => GetBytes(Config.MapPath + f);

            string requested = RenderingPipelineManager.NormalizePipelineId("Canvas");
            if (!string.Equals(Config.RenderingPipeline, requested, StringComparison.OrdinalIgnoreCase))
                Config.RenderingPipeline = requested;

            string active = RenderingPipelineManager.InitializeWithFallback(
                requested,
                new RenderingPipelineContext(CEnvir.Target, CreateRenderingHostSettings()));
            if (!string.Equals(Config.RenderingPipeline, active, StringComparison.OrdinalIgnoreCase))
                Config.RenderingPipeline = active;

            try
            {
                DXSoundManager.Create();
            }
            catch (Exception ex)
            {
                MirLog($"[Mir] 声音初始化跳过: {ex.Message}");
            }

            DXControl.ActiveScene = new LoginScene(Config.ExtendedLogin ? Config.GameSize : Config.IntroSceneSize);

            _ready = true;
            MirLog("[Mir] 真实 Zircon 客户端初始化完成（CMain 等价驱动器已就绪）。");
        }
        catch (Exception ex)
        {
            MirLog($"[Mir] 初始化异常: {ex}");
        }
    }

    /// <summary>
    /// 仅登记懒加载入口（MirLibrary(url, fromUrl:true)），字节在首次绘制对应库时由
    /// MirLibrary.GetBytesFromUrl（= mir.getBytes）按需拉取，避免启动即下载 GB 级资源。
    /// 缺失或无法登记的库自动跳过，保证初始化不因个别资源缺失而中断。
    /// </summary>
    private static void LoadLibraries()
    {
        foreach (KeyValuePair<LibraryFile, string> pair in Libraries.LibraryList)
        {
            string url = Libraries.GetUrl(pair.Key);
            if (string.IsNullOrEmpty(url))
                continue;

            try
            {
                CEnvir.LibraryList[pair.Key] = new MirLibrary(url, true);
            }
            catch (Exception ex)
            {
                MirLog($"[Mir] 库登记失败 {pair.Key}: {ex.Message}");
            }
        }

        MirLog($"[Mir] 已登记库 {CEnvir.LibraryList.Count} / {Libraries.LibraryList.Count}");
    }

    [JSExport]
    public static void Frame(double timeMs)
    {
        if (!_ready) return;
        CEnvir.GameLoop();
    }

    // ===================== 输入 =====================

    [JSExport]
    public static void OnMouseDown(int button, int x, int y)
    {
        if (!_ready) return;
        CEnvir.MouseLocation = new Point(x, y);
        DXControl.ActiveScene?.OnMouseDown(new MouseEventArgs((MouseButtons)button, 1, x, y, 0));
    }

    [JSExport]
    public static void OnMouseMove(int x, int y)
    {
        if (!_ready) return;
        CEnvir.MouseLocation = new Point(x, y);
        DXControl.ActiveScene?.OnMouseMove(new MouseEventArgs(MouseButtons.None, 0, x, y, 0));
    }

    [JSExport]
    public static void OnMouseUp(int button, int x, int y)
    {
        if (!_ready) return;
        DXControl.ActiveScene?.OnMouseUp(new MouseEventArgs((MouseButtons)button, 1, x, y, 0));
        DXControl.ActiveScene?.OnMouseClick(new MouseEventArgs((MouseButtons)button, 1, x, y, 0));
    }

    [JSExport]
    public static void OnMouseWheel(int delta, int x, int y)
    {
        if (!_ready) return;
        CEnvir.MouseLocation = new Point(x, y);
        DXControl.ActiveScene?.OnMouseWheel(new MouseEventArgs(MouseButtons.None, 0, x, y, delta));
    }

    [JSExport]
    public static void OnKeyDown(string key)
    {
        if (!_ready) return;
        Keys k = ToKeys(key);
        if (k == Keys.None) return;
        DXControl.ActiveScene?.OnKeyDown(new KeyEventArgs(k));
    }

    [JSExport]
    public static void OnKeyUp(string key)
    {
        if (!_ready) return;
        Keys k = ToKeys(key);
        if (k == Keys.None) return;
        DXControl.ActiveScene?.OnKeyUp(new KeyEventArgs(k));
    }

    [JSExport]
    public static void OnKeyPress(string key)
    {
        if (!_ready || string.IsNullOrEmpty(key)) return;
        DXControl.ActiveScene?.OnKeyPress(new KeyPressEventArgs(key[0]));
    }

    /// <summary>供 demo 兼容：单事件入口转发为 KeyDown。</summary>
    [JSExport]
    public static void OnKey(string key) => OnKeyDown(key);

    private static readonly Dictionary<string, Keys> SpecialKeys = new Dictionary<string, Keys>(StringComparer.OrdinalIgnoreCase)
    {
        { "arrowup", Keys.Up }, { "arrowdown", Keys.Down }, { "arrowleft", Keys.Left }, { "arrowright", Keys.Right },
        { "escape", Keys.Escape }, { "enter", Keys.Return }, { "return", Keys.Return }, { "tab", Keys.Tab },
        { " " , Keys.Space }, { "spacebar", Keys.Space }, { "backspace", Keys.Back }, { "delete", Keys.Delete },
        { "shift", Keys.ShiftKey }, { "control", Keys.ControlKey }, { "alt", Keys.Menu },
        { "f1", Keys.F1 }, { "f2", Keys.F2 }, { "f3", Keys.F3 }, { "f4", Keys.F4 }, { "f5", Keys.F5 },
        { "f6", Keys.F6 }, { "f7", Keys.F7 }, { "f8", Keys.F8 }, { "f9", Keys.F9 }, { "f10", Keys.F10 },
        { "f11", Keys.F11 }, { "f12", Keys.F12 },
    };

    private static Keys ToKeys(string key)
    {
        if (string.IsNullOrEmpty(key)) return Keys.None;
        if (SpecialKeys.TryGetValue(key, out Keys sp)) return sp;
        if (Enum.TryParse<Keys>(key, true, out Keys parsed)) return parsed;
        if (key.Length == 1) return (Keys)char.ToUpperInvariant(key[0]);
        return Keys.None;
    }

    // ===================== 渲染宿主设置（复刻 Client/Program.cs 的 CreateRenderingHostSettings）=====================

    private static RenderingHostSettings CreateRenderingHostSettings()
    {
        return new RenderingHostSettings
        {
            Now = () => CEnvir.Now,
            SaveException = CEnvir.SaveException,
            InvalidateRenderCaches = InvalidateRenderCaches,
            FullScreenChanged = fullScreen =>
            {
                if (DXConfigWindow.ActiveConfig?.FullScreenCheckBox != null)
                    DXConfigWindow.ActiveConfig.FullScreenCheckBox.Checked = fullScreen;
            },
            GetActiveSceneSize = () => DXControl.ActiveScene?.Size ?? Config.GameSize,
            GetDefaultMonitor = () => Config.DefaultMonitor,
            SetDefaultMonitor = v => Config.DefaultMonitor = v,
            GetRenderingPipeline = () => Config.RenderingPipeline,
            SetRenderingPipeline = v => Config.RenderingPipeline = v,
            GetGameSize = () => Config.GameSize,
            SetGameSize = v => Config.GameSize = v,
            GetFullScreen = () => Config.FullScreen,
            SetFullScreen = v => Config.FullScreen = v,
            GetBorderless = () => Config.Borderless,
            SetBorderless = v => Config.Borderless = v,
            GetVSync = () => Config.VSync,
            SetVSync = v => Config.VSync = v,
            GetUseD3D11SpriteBatch = () => Config.UseD3D11SpriteBatch,
            SetUseD3D11SpriteBatch = v => Config.UseD3D11SpriteBatch = v,
        };
    }

    private static void InvalidateRenderCaches()
    {
        var visited = new HashSet<DXControl>();
        InvalidateControlTree(DXControl.ActiveScene, visited);

        foreach (DXControl messageBox in DXControl.MessageBoxList.ToArray())
            InvalidateControlTree(messageBox, visited);

        InvalidateControlTree(DXControl.MouseControl, visited);
        InvalidateControlTree(DXControl.FocusControl, visited);

        foreach (MirLibrary library in CEnvir.LibraryList.Values)
            library?.DisposeTextures();
    }

    private static void InvalidateControlTree(DXControl control, HashSet<DXControl> visited)
    {
        if (control == null || !visited.Add(control))
            return;

        control.DisposeTexture();

        foreach (DXControl child in control.Controls.ToArray())
            InvalidateControlTree(child, visited);
    }
}
