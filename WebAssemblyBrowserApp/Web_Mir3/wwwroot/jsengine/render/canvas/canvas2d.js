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

// 在离屏 canvas 上渲染文字（描边/渐变/对齐），取 RGBA 后注册为纹理句柄。
// 对应 C# 端 MirEngine.BrowserCanvas.DrawLabel（替代 DXLabel/DXTextBox 的 GDI 文本烘焙）。
// format 位：HorizontalCenter=1, Right=2, VerticalCenter=4, Bottom=8。
export const drawLabel = (handle, w, h, text, fontCss, foreArgb, outlineArgb, format, backArgb, gradTopArgb, gradBottomArgb, gradient) => {
    w = Math.max(1, w | 0); h = Math.max(1, h | 0);
    const cv = document.createElement('canvas');
    cv.width = w; cv.height = h;
    const c = cv.getContext('2d');
    c.clearRect(0, 0, w, h);
    if (backArgb >= 0) { c.fillStyle = toCss(backArgb); c.fillRect(0, 0, w, h); }
    if (!text) { createImage(handle, new Uint8ClampedArray(c.getImageData(0, 0, w, h).data), w, h); return; }

    c.font = fontCss;
    c.textBaseline = 'top';
    const horizontalCenter = (format & 1) !== 0;
    const right = (format & 2) !== 0;
    const verticalCenter = (format & 4) !== 0;
    const bottom = (format & 8) !== 0;
    const fs = parseInt((fontCss.match(/(\d+)(px|pt)/) || [])[1] || 12);
    const lineHeight = fs * 1.3;
    const textWidth = c.measureText(text).width;

    let x = 1;
    if (horizontalCenter) x = Math.max(0, (w - textWidth) / 2);
    else if (right) x = Math.max(0, w - textWidth - 1);
    let y = 0;
    if (verticalCenter) y = Math.max(0, (h - lineHeight) / 2);
    else if (bottom) y = Math.max(0, h - lineHeight);

    const drawText = (argb, ox, oy) => { c.fillStyle = toCss(argb); c.fillText(text, x + ox, y + oy); };
    const ox = outlineArgb >= 0 ? 1 : 0;
    const oy = outlineArgb >= 0 ? 1 : 0;

    if (outlineArgb >= 0) {
        drawText(outlineArgb, 1, 0); drawText(outlineArgb, 0, 1); drawText(outlineArgb, 2, 1); drawText(outlineArgb, 1, 2);
    }
    if (gradient) {
        const g = c.createLinearGradient(0, 0, 0, h);
        g.addColorStop(0, toCss(gradTopArgb));
        g.addColorStop(1, toCss(gradBottomArgb));
        c.fillStyle = g;
        c.fillText(text, x + ox, y + oy);
    } else {
        drawText(foreArgb, ox, oy);
    }

    createImage(handle, new Uint8ClampedArray(c.getImageData(0, 0, w, h).data), w, h);
};
