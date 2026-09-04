// jsengine/shared.js
// 跨模块共享的 DOM 引用、可变状态与工具函数。
// 各模块（core/*、render/*）从这里导入，确保 textures / offscreens / audio 等状态全局唯一。

export const dom = {
    canvas: document.getElementById('screen'),
    panel: document.getElementById('panel'),
    loading: document.getElementById('loading'),
};
dom.ctx = dom.canvas.getContext('2d', { alpha: false });

// 渲染共享状态（精灵纹理 + 离屏画布 / RenderTarget）
export const gfx = {
    textures: new Map(),            // key -> HTMLCanvasElement（每张贴图的离屏源）
    offscreens: new Map(),          // id  -> { canvas, ctx }（DXControl 离屏 / RenderTarget）
    nextOffId: 1000,
    mainTarget: { canvas: dom.canvas, ctx: dom.ctx },
    cur: null,                      // 当前绘制目标
};
gfx.cur = gfx.mainTarget;

// 资源 / 主机共享状态
export const host = {
    assets: new Map(),              // url -> Uint8Array
    exports: null,                  // 由引导入口在 getAssemblyExports 后写入
};

// 音频共享状态
export const audio = {
    ctx: null,
    nextId: 1,
    active: new Map(),              // id -> { src, gain }
};

export const toCss = (argb) => {
    const a = (argb >>> 24) & 0xFF;
    const r = (argb >>> 16) & 0xFF;
    const g = (argb >>> 8) & 0xFF;
    const b = argb & 0xFF;
    return `rgba(${r},${g},${b},${(a / 255).toFixed(3)})`;
};

// 同步按需拉取 URL 字节（XMLHttpRequest 同步模式），带缓存。
export function fetchBytes(url) {
    const cached = host.assets.get(url);
    if (cached) return cached;
    try {
        const xhr = new XMLHttpRequest();
        xhr.open('GET', url.replace(/ /g, '%20'), false);
        xhr.responseType = 'arraybuffer';
        xhr.send();
        if (xhr.status !== 200) return null;
        const data = new Uint8Array(xhr.response);
        host.assets.set(url, data);
        return data;
    } catch (e) { return null; }
}

export function ensureAudio() {
    if (!audio.ctx) {
        const AC = window.AudioContext || window.webkitAudioContext;
        if (!AC) return null;
        audio.ctx = new AC();
    }
    if (audio.ctx.state === 'suspended') audio.ctx.resume();
    return audio.ctx;
}

// 同步解析 16-bit PCM WAV -> AudioBuffer（传奇3音效均为 16 位 PCM）
export function decodeWav(bytes) {
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
    const buffer = audio.ctx.createBuffer(numChannels, numFrames, sampleRate);
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

// 把 DOM 客户端坐标换算成画布内部像素坐标
export function canvasToClient(e) {
    const r = dom.canvas.getBoundingClientRect();
    return [
        Math.round((e.clientX - r.left) * dom.canvas.width / r.width),
        Math.round((e.clientY - r.top) * dom.canvas.height / r.height),
    ];
}
