// jsengine/render/webgl/webgl-engine.js
// WebGL 渲染后端（占位 / 待实现）。
//
// 架构上对应 canvas/ 的拆分：当接入 WebGL 时，应拆为：
//   - webgl2d.js   ：mir.createImage / drawImage / drawBatch / fillRect / drawText / measureText / clear / setStatus
//                     对应 C# 端 MirClient/MirCanvas.cs；
//   - webgl-engine.js：mir.cr* 控件树引擎
//                     对应 C# 端 JSBind/BrowserCanvas.cs。
//
// 本文件先以占位形式导出完整渲染 API；待实现时再拆分，并接入 main.js 的 mir 聚合。
// 当前 main.js 仅聚合 canvas/ 后端，本文件尚未被加载。

const notImpl = (name) => { throw new Error(`WebGL 渲染后端未实现: mir.${name}`); };

export const createImage = (...a) => notImpl('createImage');
export const disposeImage = (...a) => notImpl('disposeImage');
export const drawImage = (...a) => notImpl('drawImage');
export const drawBatch = (...a) => notImpl('drawBatch');
export const fillRect = (...a) => notImpl('fillRect');
export const drawText = (...a) => notImpl('drawText');
export const measureText = (...a) => notImpl('measureText');
export const clear = (...a) => notImpl('clear');
export const setStatus = (...a) => notImpl('setStatus');

export const crCreateOffscreen = (...a) => notImpl('crCreateOffscreen');
export const crSetTarget = (...a) => notImpl('crSetTarget');
export const crClear = (...a) => notImpl('crClear');
export const crDraw = (...a) => notImpl('crDraw');
export const crMeasureText = (...a) => notImpl('crMeasureText');
export const crFillRect = (...a) => notImpl('crFillRect');
export const crDrawLine = (...a) => notImpl('crDrawLine');
export const crFlush = (...a) => notImpl('crFlush');
