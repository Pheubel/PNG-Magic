namespace PngMagic.Core;

public class FilePayload : IPayload
{
    public byte[] PayloadData { get; }
    public string FileName { get; }

    public FilePayload(string fileName, byte[] payloadData)
    {
        PayloadData = payloadData;
        FileName = fileName;
    }

    HeaderType IPayload.PayloadType => HeaderType.File;
}
