// jsengine/core/keyboard.js
// 键盘输入。对应 JSBind/BrowserKeyboard.cs（mir.keyboardAttach / keyboardDetach）。
// 仅做 DOM 事件 -> BrowserKeyboard 导出方法的转发；键名 -> Keys 的语义翻译在 C# 端完成。
import { host } from '../shared.js';

let attached = false;

function onKeyDown(e) {
    const k = host.exports.MirEngine.BrowserKeyboard;
    k.OnKeyDown(e.key);
    if (e.key.length === 1) k.OnKeyPress(e.key);
}

function onKeyUp(e) {
    host.exports.MirEngine.BrowserKeyboard.OnKeyUp(e.key);
}

export const keyboardAttach = () => {
    if (attached) return;
    window.addEventListener('keydown', onKeyDown);
    window.addEventListener('keyup', onKeyUp);
    attached = true;
};

export const keyboardDetach = () => {
    if (!attached) return;
    window.removeEventListener('keydown', onKeyDown);
    window.removeEventListener('keyup', onKeyUp);
    attached = false;
};
