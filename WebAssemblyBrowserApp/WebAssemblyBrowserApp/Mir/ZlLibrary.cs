using System.IO;

namespace MirClient.Assets;

public enum ZlCodec : byte
{
    Dxt1,
    Dxt5,
    Bgra32,
    Bc7,
    Png,
}

/// <summary>
/// 原版 Mir3 .Zl 资源库中的一张图（索引条目）。
/// 与 Zircon 的 ZlImageMetadata 二进制兼容，但不依赖 System.Drawing / 原生纹理。
/// </summary>
public sealed class ZlImage
{
    public int Index;
    public int Position;
    public short Width;
    public short Height;
    public short OffSetX;
    public short OffSetY;
    public byte ShadowType;
    public short ShadowWidth;
    public short ShadowHeight;
    public short ShadowOffSetX;
    public short ShadowOffSetY;
    public short OverlayWidth;
    public short OverlayHeight;

    public ZlCodec Codec;
    public int StoredDataSize;

    public bool HasShadow => ShadowWidth > 0 && ShadowHeight > 0;
    public bool IsDxt => Codec is ZlCodec.Dxt1 or ZlCodec.Dxt5;

    public int DataSize => StoredDataSize > 0 ? StoredDataSize : ComputeSize(Width, Height, Codec);

    public static int ComputeSize(int width, int height, ZlCodec codec) => codec switch
    {
        ZlCodec.Dxt1 => BlockCount(width, height) * 8,
        ZlCodec.Dxt5 => BlockCount(width, height) * 16,
        ZlCodec.Bc7 => BlockCount(width, height) * 16,
        ZlCodec.Bgra32 => Math.Max(0, width) * Math.Max(0, height) * 4,
        _ => 0,
    };

    public static int BlockCount(int width, int height)
        => ((Math.Max(0, width) + 3) / 4) * ((Math.Max(0, height) + 3) / 4);
}

/// <summary>
/// .Zl 资源库。索引区在文件头部，图片数据按 Position 随机访问——
/// 因此 Web 端可只下载索引，再按需拉取图片数据（HTTP Range）。
/// </summary>
public sealed class ZlLibrary
{
    public string Name = string.Empty;
    public int Version;
    public ZlImage?[] Images = Array.Empty<ZlImage>();

    /// <summary>索引区字节数，Web 端首次只需拉取这一段。</summary>
    public int IndexRegionSize { get; private set; }

    public int ValidCount { get; private set; }

    public static ZlLibrary ReadIndex(ReadOnlySpan<byte> fileHeader)
    {
        ZlLibrary lib = new();
        if (fileHeader.Length < 4)
            throw new InvalidDataException("文件过短，无法读取索引长度");

        int indexSize = BitConverter.ToInt32(fileHeader);
        if (indexSize <= 4 || indexSize > fileHeader.Length - 4)
            throw new InvalidDataException($"非法的索引长度: {indexSize}");

        lib.IndexRegionSize = 4 + indexSize;

        using MemoryStream ms = new(fileHeader.Slice(4, indexSize).ToArray(), writable: false);
        using BinaryReader reader = new(ms);

        int value = reader.ReadInt32();
        int count = value & 0x1FFFFFF;
        int version = (value >> 25) & 0x7F;
        if (version == 0)
            count = value;

        if (count < 0 || count > 10_000_000)
            throw new InvalidDataException($"非法的图片数量: {count}");

        lib.Version = version;
        lib.Images = new ZlImage[count];

        for (int i = 0; i < count; i++)
        {
            if (!reader.ReadBoolean())
                continue;

            ZlImage img = new()
            {
                Index = i,
                Position = reader.ReadInt32(),
                Width = reader.ReadInt16(),
                Height = reader.ReadInt16(),
                OffSetX = reader.ReadInt16(),
                OffSetY = reader.ReadInt16(),
                ShadowType = reader.ReadByte(),
                ShadowWidth = reader.ReadInt16(),
                ShadowHeight = reader.ReadInt16(),
                ShadowOffSetX = reader.ReadInt16(),
                ShadowOffSetY = reader.ReadInt16(),
                OverlayWidth = reader.ReadInt16(),
                OverlayHeight = reader.ReadInt16(),
            };

            if (version >= 2)
            {
                reader.ReadInt32();    // AtlasPage
                reader.ReadBytes(8);   // SourceRectangle
                reader.ReadBytes(8);   // VisibleBounds
                img.Codec = (ZlCodec)reader.ReadByte();
                reader.ReadBytes(3);   // Shadow/Overlay Codec
                reader.ReadBytes(3);   // RuntimePreference
                img.StoredDataSize = reader.ReadInt32();
                reader.ReadBytes(36);  // 其余 BC7/Fallback 尺寸字段
            }
            else
            {
                // 原版 Mir3：version 0 = DXT1，其余 = DXT5
                img.Codec = version == 0 ? ZlCodec.Dxt1 : ZlCodec.Dxt5;
            }

            lib.Images[i] = img;
            lib.ValidCount++;
        }

        return lib;
    }

    /// <summary>解码主图层，返回 RGBA32。payload 需从 Position 起、至少 DataSize 长。</summary>
    public byte[]? DecodeImage(ZlImage image, ReadOnlySpan<byte> payload)
    {
        int size = image.DataSize;
        if (image.Width <= 0 || image.Height <= 0 || size <= 0 || payload.Length < size)
            return null;

        ReadOnlySpan<byte> slice = payload.Slice(0, size);

        return image.Codec switch
        {
            ZlCodec.Dxt1 => DxtDecoder.Decode(slice, image.Width, image.Height, dxt1: true),
            ZlCodec.Dxt5 => DxtDecoder.Decode(slice, image.Width, image.Height, dxt1: false),
            ZlCodec.Bgra32 => BgraToRgba(slice),
            _ => null,
        };
    }

    private static byte[] BgraToRgba(ReadOnlySpan<byte> src)
    {
        byte[] dst = new byte[src.Length];
        for (int i = 0; i + 3 < src.Length; i += 4)
        {
            dst[i] = src[i + 2];
            dst[i + 1] = src[i + 1];
            dst[i + 2] = src[i];
            dst[i + 3] = src[i + 3];
        }
        return dst;
    }
}
