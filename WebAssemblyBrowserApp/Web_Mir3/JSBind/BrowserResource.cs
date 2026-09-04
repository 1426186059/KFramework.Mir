using System.Collections.Generic;
using System.Runtime.InteropServices.JavaScript;

namespace MirEngine;

/// <summary>
/// 浏览器端资源 / 主机服务封装。对应 jsengine/core/resource.js（mir.getBytes + mir.log）。
/// getBytes 同步按需拉取 URL 字节（带缓存），log 转发到 console。
/// </summary>
internal static partial class BrowserResource
{
    [JSImport("mir.getBytes", "main.js")]
    private static partial byte[] GetBytesImpl(string url);

    [JSImport("mir.log", "main.js")]
    private static partial void LogImpl(string message);

    private static readonly HashSet<string> _logged404 = new HashSet<string>();

    public static byte[] GetBytes(string url)
    {
        byte[] data = GetBytesImpl(url);
        if ((data == null || data.Length == 0) && _logged404.Add(url))
            Log($"[Mir] 资源加载失败(404?): {url}");
        return data;
    }

    public static void Log(string message) => LogImpl(message);
}
