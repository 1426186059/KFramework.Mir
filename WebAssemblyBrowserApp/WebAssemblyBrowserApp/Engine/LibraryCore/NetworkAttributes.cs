namespace Library.Network
{
    /// <summary>
    /// 原版定义在 <c>LibraryCore/Network/Packet.cs</c>。该文件的其余部分依赖 TcpClient/SslStream，
    /// 浏览器 WASM 不可用，故只把被 LibraryCore 数据模型（Globals/Stat 等）引用的两个标记属性抽出。
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    internal sealed class IgnorePropertyPacket : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Method)]
    internal sealed class CompleteObject : Attribute
    {
    }
}
