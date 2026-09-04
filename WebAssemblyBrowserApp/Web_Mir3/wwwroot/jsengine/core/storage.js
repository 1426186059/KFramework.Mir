// jsengine/core/storage.js
// 本地存储（localStorage）。对应 JSBind/BrowserStorage.cs（mir.storageAppend / storageGet / storageRemove）。

export const storageAppend = (key, text) => {
    try { localStorage.setItem(key, (localStorage.getItem(key) || '') + text); } catch (e) { }
};

export const storageGet = (key) => {
    try { return localStorage.getItem(key) || ''; } catch (e) { return ''; }
};

export const storageRemove = (key) => {
    try { localStorage.removeItem(key); } catch (e) { }
};
