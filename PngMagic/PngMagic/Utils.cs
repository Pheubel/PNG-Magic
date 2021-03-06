using System.Runtime.InteropServices;
using System.Text;

namespace PngMagic.Core;

internal static class Utils
{
    public static readonly byte[] DataChunkType = Encoding.UTF8.GetBytes("IDAT");
    public static readonly byte[] InjectedChunkHeaderType = Encoding.UTF8.GetBytes("hjNK");
    public static readonly byte[] InjectedChunkType = Encoding.UTF8.GetBytes("ijNK");

    static readonly byte[] PngHeader = new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 };

    public static bool IsPNG(ReadOnlySpan<byte> fileData) =>
        fileData[..PngHeader.Length].SequenceEqual(PngHeader);

    public static void ReverseOnLittleEndian(this Span<byte> bytes)
    {
        if (BitConverter.IsLittleEndian)
            bytes.Reverse();
    }

    public static uint GetInjectedChunkTypeValue()
    {
        Span<byte> buffer = stackalloc byte[sizeof(uint)];
        InjectedChunkType.CopyTo(buffer);
        buffer.ReverseOnLittleEndian();
        return MemoryMarshal.Read<uint>(buffer);
    }

    public static uint GetInjectedChunkHeaderTypeValue()
    {
        Span<byte> buffer = stackalloc byte[sizeof(uint)];
        InjectedChunkHeaderType.CopyTo(buffer);
        buffer.ReverseOnLittleEndian();
        return MemoryMarshal.Read<uint>(buffer);
    }
}
