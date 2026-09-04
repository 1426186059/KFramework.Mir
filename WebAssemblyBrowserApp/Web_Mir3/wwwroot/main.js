import { dotnet } from './_framework/dotnet.js';
import { host, dom, preloadAssets } from './jsengine/shared.js';

// 各输入 / 渲染 / 存储模块（每个对应 JSBind 里的一个 C# 类）。
import * as storage from './jsengine/core/storage.js';
import * as keyboard from './jsengine/core/keyboard.js';
import * as mouse from './jsengine/core/mouse.js';
import * as audio from './jsengine/core/audio.js';
import * as resource from './jsengine/core/resource.js';
import * as websocket from './jsengine/core/websocket.js';
import * as printTool from './jsengine/core/PrintTool.js';
import * as canvas2d from './jsengine/render/canvas/canvas2d.js';
import * as canvasEngine from './jsengine/render/canvas/canvas-engine.js';

const { setModuleImports, getAssemblyExports, getConfig } = await dotnet.create();

// 把各模块导出的 mir.* 函数聚合成一个 mir 对象。
// 模块名仍为 "main.js"，与 C# 侧 [JSImport("mir.xxx", "main.js")] 对齐。
const mir = {
    ...storage,
    ...keyboard,
    ...mouse,
    ...audio,
    ...resource,
    ...websocket,
    ...printTool,
    ...canvas2d,
    ...canvasEngine,
};

setModuleImports('main.js', { mir });

const config = getConfig();
const exports = await getAssemblyExports(config.mainAssemblyName);
host.exports = exports; // 供各输入模块在 DOM 事件回调里调用 BrowserKeyboard/BrowserMouse 的 [JSExport] 入口

// [JSExport] 的键是完整类型名（含命名空间）。
// 优先使用真实 Zircon 客户端驱动器 MirClientHost；缺失时回退到 demo 版 MirGame。
const game = exports.Client?.MirClientHost ?? exports.MirClient?.MirGame ?? exports.MirGame;
if (!game) {
    console.error('未找到 MirClientHost / MirGame 导出，可用导出：', Object.keys(exports));
    dom.loading.innerHTML = '<div style="color:#e06c75">初始化失败：未找到客户端导出（详见控制台）</div>';
    throw new Error('client export not found');
}

const loadText = document.getElementById('loadText');

try {
    loadText.textContent = '正在初始化客户端（资源按需加载）…';
    game.Init();

    // 启动前异步预热数据库（System.db / Users.db）：避免连接后 LoadDatabase 的同步取字节冻结主线程 → 心跳超时。
    // 预取为异步 fetch，不阻塞主线程；完成后同步 getBytes 命中缓存即瞬时返回。
    if (typeof game.GetPreloadUrls === 'function') {
        try {
            const preload = game.GetPreloadUrls();
            if (preload && preload.length) {
                loadText.textContent = '正在预加载数据库…';
                await preloadAssets(preload);
            }
        } catch (e) {
        }
    }

    dom.loading.style.display = 'none';

    requestAnimationFrame(function loop(t) {
        game.Frame(t);
        requestAnimationFrame(loop);
    });
} catch (err) {
    console.error(err);
    dom.loading.innerHTML = `<div style="color:#e06c75">初始化失败：${err.message}</div>`;
}
