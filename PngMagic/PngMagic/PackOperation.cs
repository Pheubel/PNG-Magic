using System.Runtime.InteropServices;
using System.Text;

namespace PngMagic.Core;

public static class PackOperation
{
    const int DATA_BUFFER_SIZE = 1028;

    public static void Start(string containerPng, Stream outputStream, params Stream[] payloadStreams) =>
        Start(containerPng, outputStream, payloadStreams);

    public static void Start(string containerPng, Stream outputStream, ReadOnlySpan<Stream> payloadStreams)
    {
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

        outputStream.Write(signature);

        var table = new CrcTable();

        Span<byte> wordBuffer = stackalloc byte[sizeof(int)];
        Span<byte> dataBuffer = stackalloc byte[DATA_BUFFER_SIZE];
        bool quit = false;
        while (!quit)
        {
            // reset the CRC
            uint crc = default;

            // read the chunk size
            containerStream.Read(wordBuffer);
            outputStream.Write(wordBuffer);

            wordBuffer.ReverseOnLittleEndian();
            uint chunkSize = MemoryMarshal.Read<uint>(wordBuffer);
#if DEBUG
            Console.WriteLine($"Chunk size: {chunkSize}");
#endif

            // read the chunk type
            containerStream.Read(wordBuffer);
            outputStream.Write(wordBuffer);
            crc = table.UpdateCRC(wordBuffer, crc);

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

            // read the chunk data
            bool read = true;
            while (read)
            {
                int toRead = (int)Math.Min(DATA_BUFFER_SIZE, chunkSize);
                Span<byte> readSlice = dataBuffer[..toRead];

                int readBytes = containerStream.Read(readSlice);
                outputStream.Write(readSlice);
                crc = table.UpdateCRC(readSlice, crc);


                if (chunkSize <= DATA_BUFFER_SIZE)
                    read = false;

                chunkSize -= DATA_BUFFER_SIZE;
            }

            containerStream.Read(wordBuffer);

#if DEBUG
            wordBuffer.ReverseOnLittleEndian();
            uint fcrc = MemoryMarshal.Read<uint>(wordBuffer);
            Console.WriteLine($"Original chunk CRC: {fcrc}\nCalculated chunk CRC: {crc}");
#endif
            // write the CRC
            MemoryMarshal.Write(wordBuffer, ref crc);
            wordBuffer.ReverseOnLittleEndian();
            outputStream.Write(wordBuffer);

            // is current type IHDR
            if (chunkTypeValue == 1229472850)
            {
                foreach (var payloadStream in payloadStreams)
                {
                    // reset the CRC for the injected chunk header
                    crc = default;

                    // write injected chunk header size
                    uint cz = sizeof(HeaderType);

#if DEBUG
                    Console.WriteLine($"Injected chunk size: {cz}");
#endif
                    MemoryMarshal.Write(wordBuffer, ref cz);
                    wordBuffer.ReverseOnLittleEndian();

                    outputStream.Write(wordBuffer);

                    // write injected chunk type
                    outputStream.Write(Utils.InjectedChunkHeaderType);
                    crc = table.UpdateCRC(Utils.InjectedChunkHeaderType, crc);

#if DEBUG
                    Utils.InjectedChunkHeaderType.CopyTo(wordBuffer);
                    wordBuffer.ReverseOnLittleEndian();
                    Console.WriteLine($"Injected chunk type: {Encoding.UTF8.GetString(Utils.InjectedChunkHeaderType)} ({MemoryMarshal.Read<uint>(wordBuffer)})");
#endif
                    // write injected data
                    outputStream.WriteByte((byte)HeaderType.File);

                    crc = table.UpdateCRC((byte)HeaderType.File, crc);

                    // write injected CRC
                    MemoryMarshal.Write(wordBuffer, ref crc);
                    wordBuffer.ReverseOnLittleEndian();

                    outputStream.Write(wordBuffer);

#if DEBUG
                    Console.WriteLine($"injected CRC: {crc}");
#endif

                    // reset the CRC for the injected chunk
                    crc = default;


                    // write injected chunk size
                    cz = (uint)payloadStream.Length;
#if DEBUG
                    Console.WriteLine($"Injected chunk size: {cz}");
#endif

                    MemoryMarshal.Write(wordBuffer, ref cz);
                    wordBuffer.ReverseOnLittleEndian();

                    outputStream.Write(wordBuffer);

                    // write injected chunk type
                    outputStream.Write(Utils.InjectedChunkType);
                    crc = table.UpdateCRC(Utils.InjectedChunkType, crc);

#if DEBUG
                    Utils.InjectedChunkType.CopyTo(wordBuffer);
                    wordBuffer.ReverseOnLittleEndian();
                    Console.WriteLine($"Injected chunk type: {Encoding.UTF8.GetString(Utils.InjectedChunkType)} ({MemoryMarshal.Read<uint>(wordBuffer)})");
#endif

                    // write injected data
                    bool write = true;
                    while (write)
                    {
                        int toWrite = (int)Math.Min(DATA_BUFFER_SIZE, cz);
                        Span<byte> writeSlice = dataBuffer[..toWrite];

                        int readBytes = payloadStream.Read(writeSlice);
                        outputStream.Write(writeSlice);
                        crc = table.UpdateCRC(writeSlice, crc);


                        if (cz <= DATA_BUFFER_SIZE)
                            write = false;

                        cz -= DATA_BUFFER_SIZE;
                    }

                    // write injected CRC
                    MemoryMarshal.Write(wordBuffer, ref crc);
                    wordBuffer.ReverseOnLittleEndian();

                    outputStream.Write(wordBuffer);

#if DEBUG
                    Console.WriteLine($"injected CRC: {crc}");
#endif
                }
            }
        }
    }

    public static void Start(string containerPng, Stream outputStream, params string[] payloadFiles) =>
        Start(containerPng, outputStream, payloadFiles);

    public static void Start(string containerPng, Stream outputStream, ReadOnlySpan<string> payloadFiles)
    {
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

        outputStream.Write(signature);

        var table = new CrcTable();

        Span<byte> wordBuffer = stackalloc byte[sizeof(int)];
        Span<byte> dataBuffer = stackalloc byte[DATA_BUFFER_SIZE];
        bool quit = false;
        while (!quit)
        {
            // reset the CRC
            uint crc = default;

            // read the chunk size
            containerStream.Read(wordBuffer);
            outputStream.Write(wordBuffer);

            wordBuffer.ReverseOnLittleEndian();
            uint chunkSize = MemoryMarshal.Read<uint>(wordBuffer);
#if DEBUG
            Console.WriteLine($"Chunk size: {chunkSize}");
#endif

            // read the chunk type
            containerStream.Read(wordBuffer);
            outputStream.Write(wordBuffer);
            crc = table.UpdateCRC(wordBuffer, crc);

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

            // read the chunk data
            bool read = true;
            while (read)
            {
                int toRead = (int)Math.Min(DATA_BUFFER_SIZE, chunkSize);
                Span<byte> readSlice = dataBuffer[..toRead];

                int readBytes = containerStream.Read(readSlice);
                outputStream.Write(readSlice);
                crc = table.UpdateCRC(readSlice, crc);


                if (chunkSize <= DATA_BUFFER_SIZE)
                    read = false;

                chunkSize -= DATA_BUFFER_SIZE;
            }

            containerStream.Read(wordBuffer);

#if DEBUG
            wordBuffer.ReverseOnLittleEndian();
            uint fcrc = MemoryMarshal.Read<uint>(wordBuffer);
            Console.WriteLine($"Original chunk CRC: {fcrc}\nCalculated chunk CRC: {crc}");
#endif
            // write the CRC
            MemoryMarshal.Write(wordBuffer, ref crc);
            wordBuffer.ReverseOnLittleEndian();
            outputStream.Write(wordBuffer);

            // is current type IHDR
            if (chunkTypeValue == 1229472850)
            {
                foreach (var payloadFile in payloadFiles)
                {
                    // reset the CRC for the injected chunk header
                    crc = default;

                    

                    // write injected chunk header size
                    int fileLength = Encoding.UTF8.GetBytes(Path.GetFileName(payloadFile.AsSpan()), dataBuffer);    // TODO: make this safer for when the byte count is larger than DATA_BUFFER_SIZE
                    uint cz = (uint)fileLength + sizeof(HeaderType);

#if DEBUG
                    Console.WriteLine($"Injected chunk size: {cz}");
#endif
                    MemoryMarshal.Write(wordBuffer, ref cz);
                    wordBuffer.ReverseOnLittleEndian();

                    outputStream.Write(wordBuffer);

                    // write injected chunk header type
                    outputStream.Write(Utils.InjectedChunkHeaderType);
                    table.UpdateCRC(Utils.InjectedChunkHeaderType, crc);

#if DEBUG
                    Utils.InjectedChunkHeaderType.CopyTo(wordBuffer);
                    wordBuffer.ReverseOnLittleEndian();
                    Console.WriteLine($"Injected chunk type: {Encoding.UTF8.GetString(Utils.InjectedChunkHeaderType)} ({MemoryMarshal.Read<uint>(wordBuffer)})");
#endif

                    // write injected header data
                    outputStream.WriteByte((byte)HeaderType.File);
                    outputStream.Write(dataBuffer[..fileLength]);

                    crc = table.UpdateCRC((byte)HeaderType.File, crc);
                    crc = table.UpdateCRC(dataBuffer[..fileLength], crc);

                    // write injected header CRC
                    MemoryMarshal.Write(wordBuffer, ref crc);
                    wordBuffer.ReverseOnLittleEndian();

                    outputStream.Write(wordBuffer);

#if DEBUG
                    Console.WriteLine($"injected CRC: {crc}");
#endif

                    // reset the CRC for the injected chunk
                    crc = default;

                    using var payloadStream = File.OpenRead(payloadFile);

                    // write injected chunk size
                    cz = (uint)payloadStream.Length;
#if DEBUG
                    Console.WriteLine($"Injected chunk size: {cz}");
#endif

                    MemoryMarshal.Write(wordBuffer, ref cz);
                    wordBuffer.ReverseOnLittleEndian();

                    outputStream.Write(wordBuffer);

                    // write injected chunk type
                    outputStream.Write(Utils.InjectedChunkType);
                    crc = table.UpdateCRC(Utils.InjectedChunkType, crc);

#if DEBUG
                    Utils.InjectedChunkType.CopyTo(wordBuffer);
                    wordBuffer.ReverseOnLittleEndian();
                    Console.WriteLine($"Injected chunk type: {Encoding.UTF8.GetString(Utils.InjectedChunkType)} ({MemoryMarshal.Read<uint>(wordBuffer)})");
#endif

                    // write injected data
                    bool write = true;
                    while (write)
                    {
                        int toWrite = (int)Math.Min(DATA_BUFFER_SIZE, cz);
                        Span<byte> writeSlice = dataBuffer[..toWrite];

                        int readBytes = payloadStream.Read(writeSlice);
                        outputStream.Write(writeSlice);
                        crc = table.UpdateCRC(writeSlice, crc);


                        if (cz <= DATA_BUFFER_SIZE)
                            write = false;

                        cz -= DATA_BUFFER_SIZE;
                    }

                    // write injected CRC
                    MemoryMarshal.Write(wordBuffer, ref crc);
                    wordBuffer.ReverseOnLittleEndian();

                    outputStream.Write(wordBuffer);

#if DEBUG
                    Console.WriteLine($"injected CRC: {crc}");
#endif
                }
            }
        }
    }
}
