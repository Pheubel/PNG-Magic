namespace PngMagic.Core;

public interface IPayload
{
    HeaderType PayloadType { get; }
    byte[] PayloadData { get; }
}
