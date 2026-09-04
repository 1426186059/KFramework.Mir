// jsengine/render/canvas/canvas-engine.js
// Canvas 控件树引擎（DXControl 后端）。对应 JSBind/BrowserCanvas.cs（mir.cr* 函数）。
// 离屏画布 / RenderTarget 经 gfx.offscreens 管理；精灵纹理由 canvas/canvas2d.js 写入 gfx.textures 后此处读取。
import { dom, gfx, toCss } from '../../shared.js';

export const crCreateOffscreen = (w, h) => {
    const cv = document.createElement('canvas');
    cv.width = w; cv.height = h;
    const id = gfx.nextOffId++;
    gfx.offscreens.set(id, { canvas: cv, ctx: cv.getContext('2d') });
    return id;
};

export const crSetTarget = (id) => { gfx.cur = id === 0 ? gfx.mainTarget : gfx.offscreens.get(id); };

export const crClear = (r, g, b, a) => {
    gfx.cur.ctx.clearRect(0, 0, gfx.cur.canvas.width, gfx.cur.canvas.height);
    if (a > 0) {
        gfx.cur.ctx.fillStyle = `rgba(${r},${g},${b},${(a / 255).toFixed(3)})`;
        gfx.cur.ctx.fillRect(0, 0, gfx.cur.canvas.width, gfx.cur.canvas.height);
    }
};

export const crDraw = (tex, sx, sy, sw, sh, dx, dy, dw, dh, colorArgb) => {
    const src = gfx.textures.get(tex) || gfx.offscreens.get(tex)?.canvas;
    if (!src || sw <= 0 || sh <= 0) return;
    gfx.cur.ctx.globalAlpha = ((colorArgb >>> 24) & 0xFF) / 255;
    gfx.cur.ctx.drawImage(src, sx, sy, sw, sh, dx, dy, dw, dh);
    gfx.cur.ctx.globalAlpha = 1;
};

export const crMeasureText = (text, fontCss, maxWidth) => {
    dom.ctx.font = fontCss;
    const m = dom.ctx.measureText(text);
    const fs = parseInt((fontCss.match(/(\d+)(px|pt)/) || [])[1] || 12);
    return `${Math.ceil(m.width)},${Math.ceil(fs * 1.3)}`;
};

export const crFillRect = (x, y, w, h, colorArgb) => {
    gfx.cur.ctx.fillStyle = toCss(colorArgb);
    gfx.cur.ctx.fillRect(x, y, w, h);
};

export const crDrawLine = (x1, y1, x2, y2, w, colorArgb) => {
    gfx.cur.ctx.strokeStyle = toCss(colorArgb);
    gfx.cur.ctx.lineWidth = w;
    gfx.cur.ctx.beginPath();
    gfx.cur.ctx.moveTo(x1, y1);
    gfx.cur.ctx.lineTo(x2, y2);
    gfx.cur.ctx.stroke();
};

export const crFlush = () => {};
