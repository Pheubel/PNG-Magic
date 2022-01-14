using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PngMagic.Core;

public static class PackOperation
{

    public static byte[] Start(string containerPng, string payloadPng)
    {
        if (!File.Exists(containerPng))
        {
            throw new FileNotFoundException(containerPng);
        }

        if (!File.Exists(payloadPng))
        {
            throw new FileNotFoundException(payloadPng);
        }

        ReadOnlySpan<byte> containerBytes = File.ReadAllBytes(containerPng);
        ReadOnlySpan<byte> payloadBytes = File.ReadAllBytes(payloadPng);

        if (!Utils.IsPNG(containerBytes))
        {
            throw new InvalidDataException($"\"{containerPng}\" is not a valid PNG file.");
        }

        if (!Utils.IsPNG(payloadBytes))
        {
            throw new InvalidDataException($"\"{payloadPng}\" is not a valid PNG file.");
        }



        return default;
    }


}

internal static class Utils
{
    static readonly byte[] PngHeader = new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 };

    public static bool IsPNG(ReadOnlySpan<byte> fileData) => fileData[..PngHeader.Length].SequenceEqual(PngHeader);
}
