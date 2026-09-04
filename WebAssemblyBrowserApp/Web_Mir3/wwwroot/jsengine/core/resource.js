// jsengine/core/resource.js
// 资源 / 主机服务。对应 JSBind/BrowserResource.cs（mir.getBytes + mir.log）。
import { host, fetchBytes } from '../shared.js';

// 同步按需拉取 URL 字节（首次访问某资源时才发起请求并缓存），资源缺失时返回 null。
export const getBytes = (url) => fetchBytes(url);

export const log = (message) => { console.log(message); };
