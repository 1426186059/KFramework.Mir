// jsengine/render/canvas/canvas2d.js
// HTML5 Canvas 2D 绘制后端。对应 MirClient/MirCanvas.cs
// （mir.createImage / disposeImage / drawImage / drawBatch / fillRect / drawText / measureText / clear / setStatus）。
// createImage / disposeImage 维护 gfx.textures，供 canvas/canvas-engine.js 的 crDraw 共用。
import { dom, gfx, toCss } from '../../shared.js';

export const createImage = (key, rgba, w, h) => {
    if (gfx._upCount === undefined) gfx._upCount = 0;
    if (gfx._upCount < 80) {
        gfx._upCount++;
        // 统计前 4096 字节里 alpha>0 的像素数，用于判断图像是否整体透明（即使 RGB 有值）
        let alphaNonZero = 0;
        const lim = Math.min(rgba ? rgba.length : 0, 4096);
        for (let i = 3; i < lim; i += 4) if (rgba[i] > 0) alphaNonZero++;
        console.log(`[createImage] #${gfx._upCount} key=${key} w=${w} h=${h} len=${rgba?.length} alphaNonZero=${alphaNonZero}/${lim / 4}`);
    }
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

// 按宽度折行：英文按空格分词，超长单词/中文（无空格）按字符断行；保留 \n 硬换行。
function wrapLines(ctx, text, maxWidth) {
    const out = [];
    const paras = (text || '').split('\n');
    for (const para of paras) {
        if (para.length === 0) { out.push(''); continue; }
        let line = '';
        const tokens = para.split(' ');
        for (let i = 0; i < tokens.length; i++) {
            const word = tokens[i];
            const candidate = line === '' ? word : line + ' ' + word;
            if (line === '' || ctx.measureText(candidate).width <= maxWidth) {
                line = candidate;
            } else {
                out.push(line);
                if (ctx.measureText(word).width > maxWidth) {
                    let piece = '';
                    for (const ch of word) {
                        if (piece !== '' && ctx.measureText(piece + ch).width > maxWidth) { out.push(piece); piece = ch; }
                        else piece += ch;
                    }
                    line = piece;
                } else {
                    line = word;
                }
            }
        }
        out.push(line);
    }
    return out;
}

// 在离屏 canvas 上渲染文字（多行折行/描边/渐变/对齐），取 RGBA 后注册为纹理句柄。
// 对应 C# 端 MirEngine.BrowserCanvas.DrawLabel。
// format 位：HorizontalCenter=1, Right=2, VerticalCenter=4, Bottom=8, WordBreak=16。
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
    const wordBreak = (format & 16) !== 0;
    const fs = parseInt((fontCss.match(/(\d+)(px|pt)/) || [])[1] || 12);
    const lineHeight = fs * 1.3;
    const pad = 1;

    const lines = wordBreak ? wrapLines(c, text, Math.max(1, w - pad * 2)) : (text || '').split('\n');
    const totalHeight = lines.length * lineHeight;

    let startY;
    if (verticalCenter) startY = Math.max(0, (h - totalHeight) / 2);
    else if (bottom) startY = Math.max(0, h - totalHeight);
    else startY = 0;

    const ox = outlineArgb >= 0 ? 1 : 0;
    const oy = outlineArgb >= 0 ? 1 : 0;
    const drawText = (line, lx, ly, argb) => { c.fillStyle = toCss(argb); c.fillText(line, lx, ly); };

    for (let i = 0; i < lines.length; i++) {
        const line = lines[i];
        const lw = c.measureText(line).width;
        let lx = pad;
        if (horizontalCenter) lx = Math.max(pad, (w - lw) / 2);
        else if (right) lx = Math.max(pad, w - lw - pad);

        const lyBase = startY + i * lineHeight;
        if (outlineArgb >= 0) {
            drawText(line, lx + 1, lyBase, outlineArgb);
            drawText(line, lx, lyBase + 1, outlineArgb);
            drawText(line, lx + 2, lyBase + 1, outlineArgb);
            drawText(line, lx + 1, lyBase + 2, outlineArgb);
        }
        const ly = lyBase + oy;
        if (gradient) {
            const g = c.createLinearGradient(0, 0, 0, h);
            g.addColorStop(0, toCss(gradTopArgb));
            g.addColorStop(1, toCss(gradBottomArgb));
            c.fillStyle = g;
            c.fillText(line, lx + ox, ly);
        } else {
            drawText(line, lx + ox, ly, foreArgb);
        }
    }

    createImage(handle, new Uint8ClampedArray(c.getImageData(0, 0, w, h).data), w, h);
};

// 文本框文字渲染：背景 + 选择高亮矩形 + 文本 + 光标竖条。对应 C# 端 MirEngine.BrowserCanvas.DrawTextBox。
export const drawTextBox = (handle, w, h, text, fontCss, foreArgb, backArgb, selBackArgb, caretArgb, selStart, selLength, caretPos, caretVisible, verticalCenter) => {
    w = Math.max(1, w | 0); h = Math.max(1, h | 0);
    const cv = document.createElement('canvas');
    cv.width = w; cv.height = h;
    const c = cv.getContext('2d');
    c.clearRect(0, 0, w, h);
    if (backArgb >= 0) { c.fillStyle = toCss(backArgb); c.fillRect(0, 0, w, h); }
    if (!text) { createImage(handle, new Uint8ClampedArray(c.getImageData(0, 0, w, h).data), w, h); return; }

    c.font = fontCss;
    c.textBaseline = 'top';
    const fs = parseInt((fontCss.match(/(\d+)(px|pt)/) || [])[1] || 12);
    const lineHeight = fs * 1.3;
    const padX = 1;
    const top = verticalCenter ? Math.max(0, (h - lineHeight) / 2) : 0;
    const xOf = (i) => padX + c.measureText(text.substring(0, i)).width;

    if (selLength > 0 && selBackArgb >= 0) {
        const s = Math.min(selStart, selStart + selLength);
        const e = Math.max(selStart, selStart + selLength);
        const sx = xOf(s), ex = xOf(e);
        c.fillStyle = toCss(selBackArgb);
        c.fillRect(sx, top, Math.max(1, ex - sx), lineHeight);
    }

    c.fillStyle = toCss(foreArgb);
    c.fillText(text, padX, top);

    if (caretVisible) {
        const cx = xOf(caretPos);
        c.fillStyle = toCss(caretArgb);
        c.fillRect(cx, top, Math.max(1, Math.round(fs / 12)), lineHeight);
    }

    createImage(handle, new Uint8ClampedArray(c.getImageData(0, 0, w, h).data), w, h);
};
