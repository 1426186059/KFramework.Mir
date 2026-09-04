// jsengine/render/canvas/canvas2d.js
// HTML5 Canvas 2D 绘制后端。对应 MirClient/MirCanvas.cs
// （mir.createImage / disposeImage / drawImage / drawBatch / fillRect / drawText / measureText / clear / setStatus）。
// createImage / disposeImage 维护 gfx.textures，供 canvas/canvas-engine.js 的 crDraw 共用。
import { dom, gfx, toCss } from '../../shared.js';

export const createImage = (key, rgba, w, h) => {
    const cv = document.createElement('canvas');
    cv.width = w; cv.height = h;
    const c = cv.getContext('2d');
    c.putImageData(new ImageData(new Uint8ClampedArray(rgba), w, h), 0, 0);
    gfx.textures.set(key, cv);
};

export const disposeImage = (key) => {
    gfx.textures.delete(key);
};

export const drawImage = (key, sx, sy, sw, sh, dx, dy, dw, dh) => {
    const tex = gfx.textures.get(key);
    if (tex === undefined || sw <= 0 || sh <= 0) return;
    dom.ctx.drawImage(tex, sx, sy, sw, sh, dx, dy, dw, dh);
};

// 批命令模式：cmd 为 Int32Array，每条 9 个 int
export const drawBatch = (cmd, count) => {
    for (let i = 0; i < count; i++) {
        const o = i * 9;
        const tex = gfx.textures.get(cmd[o]);
        if (tex === undefined) continue;
        dom.ctx.drawImage(tex, cmd[o + 1], cmd[o + 2], cmd[o + 3], cmd[o + 4],
            cmd[o + 5], cmd[o + 6], cmd[o + 7], cmd[o + 8]);
    }
};

export const fillRect = (x, y, w, h, argb) => {
    dom.ctx.fillStyle = toCss(argb);
    dom.ctx.fillRect(x, y, w, h);
};

export const drawText = (text, x, y, argb, font) => {
    dom.ctx.font = font;
    dom.ctx.fillStyle = toCss(argb);
    dom.ctx.fillText(text, x, y);
};

export const measureText = (text, font) => {
    dom.ctx.font = font;
    return Math.ceil(dom.ctx.measureText(text).width);
};

export const clear = (argb) => {
    dom.ctx.fillStyle = toCss(argb);
    dom.ctx.fillRect(0, 0, dom.canvas.width, dom.canvas.height);
};

export const setStatus = (html) => { dom.panel.innerHTML = html; };
