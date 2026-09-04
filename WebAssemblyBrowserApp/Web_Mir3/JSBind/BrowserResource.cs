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

    public static byte[] GetBytes(string url) => GetBytesImpl(url);
    public static void Log(string message) => LogImpl(message);
}
