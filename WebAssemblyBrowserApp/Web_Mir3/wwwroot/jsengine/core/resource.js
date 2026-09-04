// jsengine/core/resource.js
// 资源 / 主机服务。对应 JSBind/BrowserResource.cs（mir.getBytes + mir.log）。
import { host, fetchBytes } from '../shared.js';

// 同步按需拉取 URL 字节（首次访问某资源时才发起请求并缓存），资源缺失时返回 null。
export const getBytes = (url) => fetchBytes(url);

// mir.log：C# 侧 BrowserResource.Log 通过 [JSImport] 调用，必须是 Function。
// 保留空实现以满足绑定校验，且不产生任何控制台输出（日志已清理）。
export const log = () => {};
