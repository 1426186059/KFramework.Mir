// jsengine/core/PrintTool.js
// JS 端统一日志工具，与 C# 端 PrintTool 对应。
// 输出格式：[TAG] hh/mm/ss, 消息（TAG 默认 "JS"，时间格式 hh/mm/ss）。
// 用法：
//   PrintTool.Write("消息")          -> 默认 TAG = JS
//   PrintTool.Write("TAG", "消息")   -> 指定 TAG
// C# 侧 [JSImport("mir.log")] 也走本工具的 log 导出（与 Write 同实现）。

const pad2 = (n) => String(n).padStart(2, '0');

export const Write = (tagOrMsg, maybeMsg) => {
    const tag = maybeMsg === undefined ? '' : tagOrMsg;
    const message = maybeMsg === undefined ? tagOrMsg : maybeMsg;
    const d = new Date();
    const time = `${pad2(d.getHours())}/${pad2(d.getMinutes())}/${pad2(d.getSeconds())}`;
    console.log(`[JS][${tag}] ${time}, ${message}`);
};

// 供 C# 侧 [JSImport("mir.log")] 绑定（BrowserResource.Log -> 404 等）。
export const log = Write;

export default { Write, log };
