namespace PngMagic.Core;

public class RawBytePayload : IPayload
{
    public byte[] PayloadData { get; }

    public RawBytePayload(byte[] payloadData)
    {
        PayloadData = payloadData;  
    }

    HeaderType IPayload.PayloadType => HeaderType.RawBytes;
}
