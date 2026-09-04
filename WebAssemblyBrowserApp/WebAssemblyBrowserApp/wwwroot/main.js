import { dotnet } from './_framework/dotnet.js'

const canvas = document.getElementById('screen');
const ctx = canvas.getContext('2d', { alpha: false });
const panel = document.getElementById('panel');
const loading = document.getElementById('loading');

/** key -> HTMLCanvasElement（每张贴图的离屏源） */
const textures = new Map();

/** 离屏画布（DXControl 控件缓存 / RenderTarget） */
const offscreens = new Map();
let nextOffId = 1000;
const mainTarget = { canvas, ctx };
let cur = mainTarget;

/** url -> Uint8Array，C# 启动前由本文件预下载 */
const assets = new Map();

const toCss = (argb) => {
    const a = (argb >>> 24) & 0xFF;
    const r = (argb >>> 16) & 0xFF;
    const g = (argb >>> 8) & 0xFF;
    const b = argb & 0xFF;
    return `rgba(${r},${g},${b},${(a / 255).toFixed(3)})`;
};

const { setModuleImports, getAssemblyExports, getConfig } = await dotnet.create();

setModuleImports('main.js', {
    mir: {
        createImage: (key, rgba, w, h) => {
            const cv = document.createElement('canvas');
            cv.width = w;
            cv.height = h;
            const c = cv.getContext('2d');
            c.putImageData(new ImageData(new Uint8ClampedArray(rgba), w, h), 0, 0);
            textures.set(key, cv);
        },

        drawImage: (key, sx, sy, sw, sh, dx, dy, dw, dh) => {
            const tex = textures.get(key);
            if (tex === undefined || sw <= 0 || sh <= 0) return;
            ctx.drawImage(tex, sx, sy, sw, sh, dx, dy, dw, dh);
        },

        // 批命令模式：cmd 为 Int32Array，每条 9 个 int
        drawBatch: (cmd, count) => {
            for (let i = 0; i < count; i++) {
                const o = i * 9;
                const tex = textures.get(cmd[o]);
                if (tex === undefined) continue;
                ctx.drawImage(tex, cmd[o + 1], cmd[o + 2], cmd[o + 3], cmd[o + 4],
                    cmd[o + 5], cmd[o + 6], cmd[o + 7], cmd[o + 8]);
            }
        },

        fillRect: (x, y, w, h, argb) => {
            ctx.fillStyle = toCss(argb);
            ctx.fillRect(x, y, w, h);
        },

        drawText: (text, x, y, argb, font) => {
            ctx.font = font;
            ctx.fillStyle = toCss(argb);
            ctx.fillText(text, x, y);
        },

        measureText: (text, font) => {
            ctx.font = font;
            return Math.ceil(ctx.measureText(text).width);
        },

        clear: (argb) => {
            ctx.fillStyle = toCss(argb);
            ctx.fillRect(0, 0, canvas.width, canvas.height);
        },

        setStatus: (html) => { panel.innerHTML = html; },

        log: (message) => { console.log(message); },

        // 资源已在启动时下载完毕，这里同步返回给 C#
        getBytes: (url) => {
            const data = assets.get(url);
            if (!data) throw new Error(`资源未预加载: ${url}`);
            return data;
        },

        // ---- Canvas 引擎（DXControl 控件树后端）----
        crCreateOffscreen: (w, h) => {
            const cv = document.createElement('canvas');
            cv.width = w; cv.height = h;
            const id = nextOffId++;
            offscreens.set(id, { canvas: cv, ctx: cv.getContext('2d') });
            return id;
        },
        crSetTarget: (id) => { cur = id === 0 ? mainTarget : offscreens.get(id); },
        crClear: (r, g, b, a) => {
            cur.ctx.clearRect(0, 0, cur.canvas.width, cur.canvas.height);
            if (a > 0) {
                cur.ctx.fillStyle = `rgba(${r},${g},${b},${(a / 255).toFixed(3)})`;
                cur.ctx.fillRect(0, 0, cur.canvas.width, cur.canvas.height);
            }
        },
        crDraw: (tex, sx, sy, sw, sh, dx, dy, dw, dh, colorArgb) => {
            const src = textures.get(tex) || offscreens.get(tex)?.canvas;
            if (!src || sw <= 0 || sh <= 0) return;
            cur.ctx.globalAlpha = ((colorArgb >>> 24) & 0xFF) / 255;
            cur.ctx.drawImage(src, sx, sy, sw, sh, dx, dy, dw, dh);
            cur.ctx.globalAlpha = 1;
        },
        crMeasureText: (text, fontCss, maxWidth) => {
            ctx.font = fontCss;
            const m = ctx.measureText(text);
            const fs = parseInt((fontCss.match(/(\d+)(px|pt)/) || [])[1] || 12);
            return `${Math.ceil(m.width)},${Math.ceil(fs * 1.3)}`;
        },
        crFillRect: (x, y, w, h, colorArgb) => {
            cur.ctx.fillStyle = toCss(colorArgb);
            cur.ctx.fillRect(x, y, w, h);
        },
        crDrawLine: (x1, y1, x2, y2, w, colorArgb) => {
            cur.ctx.strokeStyle = toCss(colorArgb);
            cur.ctx.lineWidth = w;
            cur.ctx.beginPath();
            cur.ctx.moveTo(x1, y1);
            cur.ctx.lineTo(x2, y2);
            cur.ctx.stroke();
        },
        crFlush: () => {},
    },
});

const config = getConfig();
const exports = await getAssemblyExports(config.mainAssemblyName);

// [JSExport] 的键是完整类型名（含命名空间）
const game = exports.MirClient?.MirGame ?? exports.MirGame;
if (!game) {
    console.error('未找到 MirGame 导出，可用导出：', Object.keys(exports));
    loading.innerHTML = '<div style="color:#e06c75">初始化失败：未找到 MirGame 导出（详见控制台）</div>';
    throw new Error('MirGame export not found');
}

// ---------- 输入 ----------
const toCanvas = (e) => {
    const r = canvas.getBoundingClientRect();
    return [
        Math.round((e.clientX - r.left) * canvas.width / r.width),
        Math.round((e.clientY - r.top) * canvas.height / r.height),
    ];
};

canvas.addEventListener('mousedown', (e) => {
    const [x, y] = toCanvas(e);
    game.OnMouseDown(e.button, x, y);
});
window.addEventListener('mousemove', (e) => {
    const [x, y] = toCanvas(e);
    game.OnMouseMove(x, y);
});
window.addEventListener('mouseup', (e) => {
    const [x, y] = toCanvas(e);
    game.OnMouseUp(e.button, x, y);
});
window.addEventListener('keydown', (e) => {
    game.OnKey(e.key.toLowerCase());
});

// ---------- 启动 ----------
const loadText = document.getElementById('loadText');

try {
    const list = game.GetAssetList();

    for (let i = 0; i < list.length; i++) {
        const url = list[i];
        loadText.textContent = `正在下载资源 ${i + 1}/${list.length}：${url}`;
        const res = await fetch(url);
        if (!res.ok) throw new Error(`HTTP ${res.status} — ${url}`);
        assets.set(url, new Uint8Array(await res.arrayBuffer()));
    }

    loadText.textContent = '正在解码并上传纹理…';
    await new Promise((r) => setTimeout(r, 30)); // 让浏览器有机会刷新提示

    game.Init();
    loading.style.display = 'none';

    requestAnimationFrame(function loop(t) {
        game.Frame(t);
        requestAnimationFrame(loop);
    });
} catch (err) {
    console.error(err);
    loading.innerHTML = `<div style="color:#e06c75">初始化失败：${err.message}</div>`;
}
