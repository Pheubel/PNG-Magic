namespace PngMagic.Core;

public class GenericPayload : IPayload
{
    public HeaderData HeaderData { get; }
    public byte[] PayloadData { get; }

    public GenericPayload(HeaderData headerData, byte[] payloadData)
    {
        HeaderData = headerData;
        PayloadData = payloadData;
    }

    HeaderType IPayload.PayloadType => HeaderData.HeaderType;
}
