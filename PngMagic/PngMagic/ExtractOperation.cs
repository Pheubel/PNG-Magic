using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace PngMagic.Core;

public static class ExtractOperation
{
    const int DATA_BUFFER_SIZE = 1028;

    public static IEnumerable<byte[]> GetInjectedPayloads(string containerPng)
    {
        List<byte[]> payloads = new();

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
            // is current type injected
            else if (chunkTypeValue == Utils.GetInjectedChunkTypeValue())
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
                        payloadBytes.Add(readByte);
                    }

                    if (chunkSize <= DATA_BUFFER_SIZE)
                        read = false;

                    chunkSize -= DATA_BUFFER_SIZE;
                }

                payloads.Add(payloadBytes.ToArray());
                payloadBytes.Clear();
            }
            // skip chunk if it's not interesting
            else
            {
                containerStream.Position += chunkSize;
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
}

