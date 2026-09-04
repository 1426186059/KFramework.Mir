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
    if (gfx.cur === gfx.mainTarget) console.log(`[crClear] MAIN a=${a} r=${r} g=${g} b=${b}`);
    gfx.cur.ctx.clearRect(0, 0, gfx.cur.canvas.width, gfx.cur.canvas.height);
    if (a > 0) {
        gfx.cur.ctx.fillStyle = `rgba(${r},${g},${b},${(a / 255).toFixed(3)})`;
        gfx.cur.ctx.fillRect(0, 0, gfx.cur.canvas.width, gfx.cur.canvas.height);
    }
};

export const crDraw = (tex, sx, sy, sw, sh, dx, dy, dw, dh, colorArgb) => {
    const src = gfx.textures.get(tex) || gfx.offscreens.get(tex)?.canvas;
    const found = src ? 1 : 0;
    const main = gfx.cur === gfx.mainTarget ? 1 : 0;
    // 只关注：被跳过(纹理缺失)的绘制，或目标/源尺寸较大的图像绘制（如登录背景 1024x768，可能源是 1x1）
    if (found === 0 || (sw >= 200 && sh >= 200) || (dw >= 200 && dh >= 200)) {
        console.log(`[crDraw] tex=${tex} sw=${sw} sh=${sh} dx=${dx|0} dy=${dy|0} dw=${dw|0} dh=${dh|0} a=${((colorArgb >>> 24) & 0xFF)} found=${found} main=${main}`);
    }
    if (!src || sw <= 0 || sh <= 0) return;
    const ctx = gfx.cur.ctx;
    ctx.globalCompositeOperation = gfx.blendOp;
    let a = ((colorArgb >>> 24) & 0xFF) / 255;
    // HIGHLIGHT: 结果 = BlendFactor*Src + Dst，用 alpha 承载 rate
    if (gfx.blendOp === 'lighter' && gfx.blendRate !== 1) a *= gfx.blendRate;
    ctx.globalAlpha = a;
    ctx.drawImage(src, sx, sy, sw, sh, dx, dy, dw, dh);
    ctx.globalAlpha = 1;
    ctx.globalCompositeOperation = 'source-over';
    if (tex === 1 && main === 1 && !gfx._bgpx) {
        gfx._bgpx = true;
        try {
            const pts = [[5,5],[200,150],[500,400],[900,700]];
            const parts = pts.map(([x,y]) => {
                const p = gfx.cur.ctx.getImageData(x, y, 1, 1).data;
                return `(${x},${y})=${p[0]},${p[1]},${p[2]},${p[3]}`;
            });
            console.log(`[bgpx] canvas=${gfx.cur.canvas === dom.canvas ? 'screen' : 'OTHER'} ` + parts.join(' '));
        } catch (e) { console.log('[bgpx] err', e.message); }
    }
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

export const crFlush = () => {
    try {
        const p = gfx.mainTarget.ctx.getImageData(500, 400, 1, 1).data;
        console.log(`[flushpx] (500,400)=${p[0]},${p[1]},${p[2]},${p[3]}`);
    } catch (e) { console.log('[flushpx] err', e.message); }
};
