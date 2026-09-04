// jsengine/core/websocket.js
// 手写 WebSocket 网络层（浏览器）。对应 JSBind/BrowserWebSocket.cs（mir.ws*）。
// 设计镜像 KNet.WebSocket 的 WebGL 轮询模型：
//   - JS 端维护每个实例的消息队列（二进制帧）与事件队列；
//   - C# 每帧调用 mir.wsReceive 取出一条完整消息，mir.wsGetState 轮询连接状态。
// 不主动回调 C#，避免 JS 回调线程与游戏主线程的同步问题。

const instances = {};
let nextId = 1;

export const wsConnect = (url) => {
    let id = -1;
    try {
        // HTTPS 页面下浏览器禁止混合内容（ws://），自动升级为 wss://
        if (typeof location !== 'undefined' && location.protocol === 'https:' && url.startsWith('ws://')) {
            url = 'wss://' + url.slice('ws://'.length);
        }
        const ws = new WebSocket(url);
        ws.binaryType = 'arraybuffer';
        id = nextId++;
        const inst = { ws, messages: [], open: false };
        ws.onopen = () => { inst.open = true; };
        ws.onmessage = (e) => {
            // 一条 WS 二进制消息 = 一条 Mir Packet 的字节
            const data = (e.data instanceof ArrayBuffer) ? new Uint8Array(e.data) : new Uint8Array(0);
            inst.messages.push(data);
        };
        ws.onclose = () => { inst.open = false; };
        ws.onerror = () => { inst.open = false; };
        instances[id] = inst;
    } catch (e) {
        console.error('[ws] connect failed', e);
        id = -1;
    }
    return id;
};

export const wsClose = (id) => {
    const inst = instances[id];
    if (inst && inst.ws) {
        try { inst.ws.close(1000, 'Normal Closure'); } catch (e) { /* ignore */ }
    }
    delete instances[id];
};

export const wsSend = (id, data) => {
    const inst = instances[id];
    if (inst && inst.ws && inst.ws.readyState === WebSocket.OPEN) {
        try {
            inst.ws.send(data);
            return 1;
        } catch (e) {
            return 0;
        }
    }
    return 0;
};

export const wsGetState = (id) => {
    const inst = instances[id];
    if (!inst || !inst.ws) return 3; // CLOSED
    return inst.ws.readyState;       // 0 CONNECTING, 1 OPEN, 2 CLOSING, 3 CLOSED
};

export const wsReceive = (id) => {
    const inst = instances[id];
    if (!inst || inst.messages.length === 0) return new Uint8Array(0);
    return inst.messages.shift();
};
