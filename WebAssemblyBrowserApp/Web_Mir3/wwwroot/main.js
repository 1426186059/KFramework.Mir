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

// ---- Web Audio 音效后端 ----
let audioCtx = null;
let nextAudioId = 1;
const audioActive = new Map();

function ensureAudio() {
    if (!audioCtx) {
        const AC = window.AudioContext || window.webkitAudioContext;
        if (!AC) return null;
        audioCtx = new AC();
    }
    if (audioCtx.state === 'suspended') audioCtx.resume();
    return audioCtx;
}

// 同步解析 16-bit PCM WAV -> AudioBuffer（传奇3音效均为 16 位 PCM）
function decodeWav(bytes) {
    const dv = new DataView(bytes.buffer, bytes.byteOffset, bytes.byteLength);
    let offset = 12;
    let numChannels = 1, sampleRate = 44100, bitsPerSample = 16;
    let dataOffset = -1, dataLength = 0;
    while (offset + 8 <= bytes.length) {
        const id = String.fromCharCode(bytes[offset], bytes[offset + 1], bytes[offset + 2], bytes[offset + 3]);
        const size = dv.getUint32(offset + 4, true);
        if (id === 'fmt ') {
            numChannels = dv.getUint16(offset + 10, true);
            sampleRate = dv.getUint32(offset + 12, true);
            bitsPerSample = dv.getUint16(offset + 22, true);
        } else if (id === 'data') {
            dataOffset = offset + 8;
            dataLength = size;
            break;
        }
        offset += 8 + size + (size & 1);
    }
    if (dataOffset < 0 || bitsPerSample !== 16) return null;
    const numFrames = (dataLength / (numChannels * 2)) | 0;
    if (numFrames <= 0) return null;
    const buffer = audioCtx.createBuffer(numChannels, numFrames, sampleRate);
    const int16 = new Int16Array(bytes.buffer, bytes.byteOffset + dataOffset, numFrames * numChannels);
    for (let ch = 0; ch < numChannels; ch++) {
        const chData = new Float32Array(numFrames);
        for (let i = 0; i < numFrames; i++) {
            const s = int16[i * numChannels + ch];
            chData[i] = s >= 0 ? s / 32767 : s / 32768;
        }
        buffer.copyToChannel(chData, ch);
    }
    return buffer;
}

function fetchBytes(url) {
    const cached = assets.get(url);
    if (cached) return cached;
    try {
        const xhr = new XMLHttpRequest();
        xhr.open('GET', url.replace(/ /g, '%20'), false);
        xhr.responseType = 'arraybuffer';
        xhr.send();
        if (xhr.status !== 200) return null;
        const data = new Uint8Array(xhr.response);
        assets.set(url, data);
        return data;
    } catch (e) { return null; }
}

// 浏览器自动播放策略：用户首次交互时恢复 AudioContext
const resumeAudio = () => {
    ensureAudio();
    window.removeEventListener('pointerdown', resumeAudio);
    window.removeEventListener('keydown', resumeAudio);
};
window.addEventListener('pointerdown', resumeAudio);
window.addEventListener('keydown', resumeAudio);

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

        disposeImage: (key) => {
            textures.delete(key);
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

        // 浏览器持久化（localStorage），对应 JSBind/BrowserStorage.cs。
        storageAppend: (key, text) => {
            try { localStorage.setItem(key, (localStorage.getItem(key) || '') + text); } catch (e) { }
        },
        storageGet: (key) => {
            try { return localStorage.getItem(key) || ''; } catch (e) { return ''; }
        },
        storageRemove: (key) => {
            try { localStorage.removeItem(key); } catch (e) { }
        },

        // 输入监听注册（对应 JSBind/BrowserInput.cs）。由 C# 在 Init 时调用 Attach()。
        inputAttach: () => {
            if (_inputAttached) return;
            canvas.addEventListener('mousedown', onMouseDown);
            window.addEventListener('mousemove', onMouseMove);
            window.addEventListener('mouseup', onMouseUp);
            canvas.addEventListener('wheel', onWheel, { passive: true });
            window.addEventListener('keydown', onKeyDown);
            window.addEventListener('keyup', onKeyUp);
            _inputAttached = true;
        },
        inputDetach: () => {
            if (!_inputAttached) return;
            canvas.removeEventListener('mousedown', onMouseDown);
            window.removeEventListener('mousemove', onMouseMove);
            window.removeEventListener('mouseup', onMouseUp);
            canvas.removeEventListener('wheel', onWheel);
            window.removeEventListener('keydown', onKeyDown);
            window.removeEventListener('keyup', onKeyUp);
            _inputAttached = false;
        },

        // 资源已在启动时下载完毕，这里同步返回给 C#
        // 按需同步拉取：首次访问某资源时才发起请求（XMLHttpRequest 同步模式），并缓存到 assets。
        // 这样启动期不必下载 GB 级的全部 .Zl / .map，仅在首次绘制对应资源时按需获取。
        getBytes: (url) => {
            const cached = assets.get(url);
            if (cached) return cached;
            try {
                const xhr = new XMLHttpRequest();
                xhr.open('GET', url.replace(/ /g, '%20'), false); // 编码 "Map Data" 中的空格
                xhr.responseType = 'arraybuffer';
                xhr.send();
                if (xhr.status !== 200) return null;
                const data = new Uint8Array(xhr.response);
                assets.set(url, data);
                return data;
            } catch (e) {
                return null; // 资源缺失时返回 null，由 C# 端安全跳过，不中断帧
            }
        },

        // ---- Web Audio 音效后端 ----
        initAudio: () => { ensureAudio(); },
        playSound: (url, volume, loop) => {
            try {
                const ctx = ensureAudio();
                if (!ctx) return 0;
                const data = fetchBytes(url);
                if (!data) return 0;
                const buf = decodeWav(data);
                if (!buf) return 0;
                const src = ctx.createBufferSource();
                src.buffer = buf;
                src.loop = !!loop;
                const gain = ctx.createGain();
                gain.gain.value = Math.max(0, Math.min(1, volume / 100));
                src.connect(gain).connect(ctx.destination);
                src.start(0);
                const id = nextAudioId++;
                audioActive.set(id, { src, gain });
                src.onended = () => audioActive.delete(id);
                return id;
            } catch (e) { return 0; }
        },
        stopSound: (id) => {
            const a = audioActive.get(id);
            if (a) { try { a.src.stop(); } catch (e) {} audioActive.delete(id); }
        },
        stopAllSounds: () => {
            for (const [, a] of audioActive) { try { a.src.stop(); } catch (e) {} }
            audioActive.clear();
        },
        setSoundVolume: (id, volume) => {
            const a = audioActive.get(id);
            if (a) a.gain.gain.value = Math.max(0, Math.min(1, volume / 100));
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

// [JSExport] 的键是完整类型名（含命名空间）。
// 优先使用真实 Zircon 客户端驱动器 MirClientHost；缺失时回退到 demo 版 MirGame。
const game = exports.Client?.MirClientHost ?? exports.MirClient?.MirGame ?? exports.MirGame;
if (!game) {
    console.error('未找到 MirClientHost / MirGame 导出，可用导出：', Object.keys(exports));
    loading.innerHTML = '<div style="color:#e06c75">初始化失败：未找到客户端导出（详见控制台）</div>';
    throw new Error('client export not found');
}
// ---------- 输入（由 JSBind/BrowserInput.cs 经 mir.inputAttach 注册）----------
// 这里只做 DOM 事件 -> BrowserInput 导出方法的转发；键名/按钮的语义翻译在 C# 端完成。
let _inputAttached = false;
function canvasToClient(e) {
    const r = canvas.getBoundingClientRect();
    return [
        Math.round((e.clientX - r.left) * canvas.width / r.width),
        Math.round((e.clientY - r.top) * canvas.height / r.height),
    ];
}
function onMouseDown(e) {
    const [x, y] = canvasToClient(e);
    exports.MirEngine.BrowserInput.OnMouseDown(e.button, x, y);
}
function onMouseMove(e) {
    const [x, y] = canvasToClient(e);
    exports.MirEngine.BrowserInput.OnMouseMove(x, y);
}
function onMouseUp(e) {
    const [x, y] = canvasToClient(e);
    exports.MirEngine.BrowserInput.OnMouseUp(e.button, x, y);
}
function onWheel(e) {
    const [x, y] = canvasToClient(e);
    exports.MirEngine.BrowserInput.OnMouseWheel(e.deltaY, x, y);
}
function onKeyDown(e) {
    exports.MirEngine.BrowserInput.OnKeyDown(e.key);
    if (e.key.length === 1) exports.MirEngine.BrowserInput.OnKeyPress(e.key);
}
function onKeyUp(e) {
    exports.MirEngine.BrowserInput.OnKeyUp(e.key);
}

// ---------- 启动 ----------
const loadText = document.getElementById('loadText');

try {
    loadText.textContent = '正在初始化客户端（资源按需加载）…';
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
