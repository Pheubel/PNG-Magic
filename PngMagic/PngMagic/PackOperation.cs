using System.Runtime.InteropServices;
using System.Text;

namespace PngMagic.Core;

public static class PackOperation
{
    const int DATA_BUFFER_SIZE = 1028;
    public static void Start(string containerPng, string payload, Stream outputStream)
    {
        if (!File.Exists(containerPng))
        {
            throw new FileNotFoundException(containerPng);
        }

        if (!File.Exists(payload))
        {
            throw new FileNotFoundException(payload);
        }

        using FileStream containerStream = File.OpenRead(containerPng);

        ReadOnlySpan<byte> payloadBytes = File.ReadAllBytes(payload);

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
                // reset the CRC for the injected chunk
                crc = default;

                // write injected chunk size
                uint cz = (uint)payloadBytes.Length;
                MemoryMarshal.Write(wordBuffer, ref cz);
                wordBuffer.ReverseOnLittleEndian();

                outputStream.Write(wordBuffer);

                // write injected chunk type
                outputStream.Write(Utils.InjectedChunkType);
                table.UpdateCRC(Utils.InjectedChunkType, crc);

                // write injected data
                outputStream.Write(payloadBytes);
                table.UpdateCRC(payloadBytes, crc);

                // write injected CRC
                MemoryMarshal.Write(wordBuffer, ref crc);
                wordBuffer.ReverseOnLittleEndian();

                outputStream.Write(wordBuffer);
            }
        }
    }
}

internal class CrcTable
{
    /* Table of CRCs of all 8-bit messages. */
    readonly uint[] crcTable = new uint[256];

    /* Make the table for a fast CRC. */

    public CrcTable()
    {
        uint c;
        crcTable = new uint[256];
        for (uint n = 0; n <= 255; n++)
        {
            c = n;
            for (var k = 0; k <= 7; k++)
            {
                if ((c & 1) == 1)
                    c = 0xEDB88320 ^ ((c >> 1) & 0x7FFFFFFF);
                else
                    c = ((c >> 1) & 0x7FFFFFFF);
            }
            crcTable[n] = c;
        }
    }

    public uint UpdateCRC(ReadOnlySpan<byte> buffer, uint crc)
    {
        uint c = crc ^ 0xffffffff;
        for (var i = 0; i < buffer.Length; i++)
        {
            c = crcTable[(c ^ buffer[i]) & 255] ^ ((c >> 8) & 0xFFFFFF);
        }
        return c ^ 0xffffffff;
    }
}
