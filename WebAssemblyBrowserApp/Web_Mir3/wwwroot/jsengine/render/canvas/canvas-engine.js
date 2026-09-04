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

export const crSetTarget = (id) => {
    gfx.cur = id === 0 ? gfx.mainTarget : gfx.offscreens.get(id);
    // 切换绘制表面时复位混合模式：混合状态是全局的，若上一次绘制（如灯光 LIGHTMAP/特效）
    // 留下非 source-over 且本次未在当前表面显式 SetBlend 复位，会导致后续绘制（如窗口皮肤
    // DrawEdges 把图叠在透明离屏上）被错误混合而失踪。灯光混合组在 SetBlend 后连续同表面绘制、
    // 中间不切换表面，因此此处复位不影响它们。
    gfx.blendOp = 'source-over';
};

// 把 Zircon 的 BlendMode 映射到 Canvas2D 的 globalCompositeOperation。
// 语义取自 D3D9/D3D11 混合状态（Result = SrcBlend*Src + DstBlend*Dst）：
//   LIGHTMAP(12): Zero*Src + SrcColor*Dst      -> multiply（光图乘到场景，经典暗场光照）
//   COLORFY(8) : SourceAlpha*Src + Dst         -> lighter（灯光精灵累加到光层）
//   HIGHLIGHT(10): BlendFactor*Src + Dst       -> lighter（alpha 乘 rate）
//   LIGHT/LIGHTINV/NORMAL(默认分支): InvDestColor*Src + Dst -> screen
//   NONE(-1) / Blending=false                  -> source-over（标准 alpha）
export const crSetBlend = (mode, rate, enabled) => {
    gfx.blendRate = rate;
    if (!enabled) { gfx.blendOp = 'source-over'; return; }
    switch (mode) {
        case 12: gfx.blendOp = 'multiply'; break;   // LIGHTMAP
        case 8:  gfx.blendOp = 'lighter';  break;   // COLORFY
        case 10: gfx.blendOp = 'lighter';  break;   // HIGHLIGHT
        case 1:  // LIGHT
        case 2:  // LIGHTINV
        case 0:  // NORMAL（Blending=true 时 DX 走 screen）
        case 3:  // INVNORMAL
        case 5:  // INVLIGHTINV
        case 6:  // INVCOLOR
        case 7:  // INVBACKGROUND
            gfx.blendOp = 'screen'; break;
        case 4:  // INVLIGHT
        case 9:  // MASK
        case 11: // EFFECTMASK
            gfx.blendOp = 'lighter'; break;
        case -1: // NONE
        default:
            gfx.blendOp = 'source-over'; break;
    }
};

export const crClear = (r, g, b, a) => {
    gfx.cur.ctx.clearRect(0, 0, gfx.cur.canvas.width, gfx.cur.canvas.height);
    if (a > 0) {
        gfx.cur.ctx.fillStyle = `rgba(${r},${g},${b},${(a / 255).toFixed(3)})`;
        gfx.cur.ctx.fillRect(0, 0, gfx.cur.canvas.width, gfx.cur.canvas.height);
    }
};

export const crDraw = (tex, sx, sy, sw, sh, dx, dy, dw, dh, colorArgb) => {
    const src = gfx.textures.get(tex) || gfx.offscreens.get(tex)?.canvas;
    if (!src) { console.warn(`[crDraw] 纹理缺失 tex=${tex} (sx=${sx} sy=${sy} sw=${sw} sh=${sh})`); return; }
    if (sw <= 0 || sh <= 0) return;
    const ctx = gfx.cur.ctx;
    ctx.globalCompositeOperation = gfx.blendOp;
    let a = ((colorArgb >>> 24) & 0xFF) / 255;
    // HIGHLIGHT: 结果 = BlendFactor*Src + Dst，用 alpha 承载 rate
    if (gfx.blendOp === 'lighter' && gfx.blendRate !== 1) a *= gfx.blendRate;
    ctx.globalAlpha = a;
    ctx.drawImage(src, sx, sy, sw, sh, dx, dy, dw, dh);
    ctx.globalAlpha = 1;
    ctx.globalCompositeOperation = 'source-over';
};

export const crMeasureText = (text, fontCss, maxWidth) => {
    dom.ctx.font = fontCss;
    const m = dom.ctx.measureText(text);
    const fs = parseInt((fontCss.match(/(\d+)(px|pt)/) || [])[1] || 12);
    return `${Math.ceil(m.width)},${Math.ceil(fs * 1.3)}`;
};

export const crFillRect = (x, y, w, h, colorArgb) => {
    if (gfx.cur === gfx.mainTarget && w >= 200 && h >= 200)
        console.log(`[crFillRect] MAIN x=${x} y=${y} w=${w} h=${h} argb=${colorArgb}`);
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

export const crFlush = () => { };
