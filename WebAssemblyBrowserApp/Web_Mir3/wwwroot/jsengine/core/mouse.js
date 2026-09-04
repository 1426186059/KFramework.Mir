// jsengine/core/mouse.js
// 鼠标输入。对应 JSBind/BrowserMouse.cs（mir.mouseAttach / mouseDetach）。
// 仅做 DOM 事件 -> BrowserMouse 导出方法的转发；按钮 -> MouseButtons 的语义翻译在 C# 端完成。
import { host, dom, canvasToClient } from '../shared.js';

let attached = false;

function onMouseDown(e) {
    const [x, y] = canvasToClient(e);
    host.exports.MirEngine.BrowserMouse.OnMouseDown(e.button, x, y);
}

function onMouseMove(e) {
    const [x, y] = canvasToClient(e);
    host.exports.MirEngine.BrowserMouse.OnMouseMove(x, y);
}

function onMouseUp(e) {
    const [x, y] = canvasToClient(e);
    host.exports.MirEngine.BrowserMouse.OnMouseUp(e.button, x, y);
}

function onWheel(e) {
    const [x, y] = canvasToClient(e);
    host.exports.MirEngine.BrowserMouse.OnMouseWheel(e.deltaY, x, y);
}

export const mouseAttach = () => {
    if (attached) return;
    dom.canvas.addEventListener('mousedown', onMouseDown);
    window.addEventListener('mousemove', onMouseMove);
    window.addEventListener('mouseup', onMouseUp);
    dom.canvas.addEventListener('wheel', onWheel, { passive: true });
    attached = true;
};

export const mouseDetach = () => {
    if (!attached) return;
    dom.canvas.removeEventListener('mousedown', onMouseDown);
    window.removeEventListener('mousemove', onMouseMove);
    window.removeEventListener('mouseup', onMouseUp);
    dom.canvas.removeEventListener('wheel', onWheel);
    attached = false;
};
