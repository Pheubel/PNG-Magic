using System.Runtime.InteropServices;
using System.Text;

namespace PngMagic.Core;

public static class ExtractOperation
{
    const int DATA_BUFFER_SIZE = 1028;

    public static IEnumerable<IPayload> GetInjectedPayloads(string containerPng)
    {
        List<IPayload> payloads = new();

        if (!File.Exists(containerPng))
        {
            throw new FileNotFoundException(containerPng);
        }

        using FileStream containerStream = File.OpenRead(containerPng);

        Span<byte> signature = stackalloc byte[8];
        containerStream.Read(signature);

#if DEBUG
        Console.WriteLine($"Signature: {Encoding.UTF8.GetString(signature)}");
#endif

        if (!Utils.IsPNG(signature))
        {
            throw new InvalidDataException($"\"{containerPng}\" does not have a valid PNG signature.");
        }

        List<byte> payloadBytes = new(DATA_BUFFER_SIZE);
        Span<byte> wordBuffer = stackalloc byte[sizeof(int)];
        Span<byte> dataBuffer = stackalloc byte[DATA_BUFFER_SIZE];

        HeaderData? currentHeader = null;

        bool quit = false;
        while (!quit)
        {
            // read the chunk size
            containerStream.Read(wordBuffer);
            wordBuffer.ReverseOnLittleEndian();
            uint chunkSize = MemoryMarshal.Read<uint>(wordBuffer);

#if DEBUG
            Console.WriteLine($"Chunk size: {chunkSize}");
#endif

            // read the chunk type
            containerStream.Read(wordBuffer);

#if DEBUG
            string chunkType = Encoding.UTF8.GetString(wordBuffer);
#endif
            wordBuffer.ReverseOnLittleEndian();
            uint chunkTypeValue = MemoryMarshal.Read<uint>(wordBuffer);
#if DEBUG
            Console.WriteLine($"Chunk type: {chunkType} ({chunkTypeValue})");
#endif


            // is current type IEND
            if (chunkTypeValue == 1229278788)
            {
                quit = true;
            }
            else if (chunkTypeValue == Utils.GetInjectedChunkHeaderTypeValue())
            {
                // read the chunk data
                ReadToList(payloadBytes, chunkSize, containerStream, dataBuffer);

                var plSpan =  CollectionsMarshal.AsSpan(payloadBytes);

                currentHeader = new HeaderData()
                {
                    HeaderType = (HeaderType)plSpan[0],
                    HeaderContent = plSpan.Length > 1 ? plSpan[1..].ToArray() : Array.Empty<byte>()
                };

                payloadBytes.Clear();
            }
            // is current type injected payload
            else if (chunkTypeValue == Utils.GetInjectedChunkTypeValue())
            {
                // read the chunk data
                ReadToList(payloadBytes, chunkSize, containerStream, dataBuffer);

                var plb = payloadBytes.ToArray();

                IPayload payload = (currentHeader?.HeaderType ?? HeaderType.RawBytes) switch
                {
                    HeaderType.RawBytes => new RawBytePayload(plb),
                    HeaderType.File => new FilePayload(currentHeader!.ContentToString(), plb),
                    _ => throw new NotSupportedException($"header with type \"{currentHeader}\" is not supported")
                };

                payloads.Add(payload);

                // clean up
                payloadBytes.Clear();
                currentHeader = null;
            }
            // skip chunk if it's not interesting
            else
            {
                containerStream.Position += chunkSize;

                // reset header just in case
                currentHeader = null;
            }

            // read CRC
            containerStream.Read(wordBuffer);

#if DEBUG
            wordBuffer.ReverseOnLittleEndian();
            Console.WriteLine($"Chunk CRC: {MemoryMarshal.Read<uint>(wordBuffer)}");
#endif
        }

        return payloads;
    }

    private static void ReadToList(List<byte> container, uint chunkSize, Stream containerStream, Span<byte> dataBuffer)
    {
        // read the chunk data
        bool read = true;
        while (read)
        {
            int toRead = (int)Math.Min(DATA_BUFFER_SIZE, chunkSize);

            Span<byte> readSlice = dataBuffer[..toRead];

            int readBytes = containerStream.Read(readSlice);

            // might be a better way to do this
            foreach (var readByte in readSlice)
            {
                container.Add(readByte);
            }

            if (chunkSize <= DATA_BUFFER_SIZE)
                read = false;

            chunkSize -= DATA_BUFFER_SIZE;
        }
    }
}
