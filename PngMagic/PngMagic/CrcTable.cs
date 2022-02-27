namespace PngMagic.Core;

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

    public uint UpdateCRC(byte buffer, uint crc)
    {
        uint c = crc ^ 0xffffffff;

        c = crcTable[(c ^ buffer) & 255] ^ ((c >> 8) & 0xFFFFFF);

        return c ^ 0xffffffff;
    }
}
