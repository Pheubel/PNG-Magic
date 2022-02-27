using System.Text;

namespace PngMagic.Core;

public class HeaderData
{
    public HeaderType HeaderType { get; init; }
    public Memory<byte> HeaderContent { get; init; }

    public string ContentToString() => Encoding.UTF8.GetString(HeaderContent.Span);
}