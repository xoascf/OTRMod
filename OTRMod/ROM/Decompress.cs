/* Licensed under the Open Software License version 3.0 */
// From OpenOcarinaBuilder.

namespace OTRMod.ROM;

public static class Decompress
{
	//private const int DefCompressedSize = 0x2000000; // 32MB ROM
	private const int PDecompressedSize = 0x34D3040; // 52.8MB ROM (for PAL_1.0)
	private const int EDecompressedSize = 0x3600000; // 54MB ROM (for EUR_MQD)
	private const int ADecompressedSize = 0x4000000; // 64MB ROM

	public static byte[] DecompressedBytes(byte[] inRom)
	{
		if (inRom.Length == PDecompressedSize ||
		    inRom.Length == ADecompressedSize ||
		    inRom.Length == EDecompressedSize)
			return inRom; // FIXME: Find a better way to know if already decompressed.

		//if (inRom.Length != DefCompressedSize)
		//{ Message.New(Message.Level.E, "Wrong_Size"); return null; }

		//Print("Will_Decompress");
		byte[] outRom = new byte[PDecompressedSize];
		inRom.CopyTo(outRom, 0);
		int tblStart = TableEntry.FindTable(inRom);
		TableEntry tbl = TableEntry.GetTableEntry
			(GetAllFrom(inRom, tblStart), 2);
		int tblCount = tbl.Size / 16;
		Debug.WriteLine("Number of files: " + tblCount);
		byte[] inTable = GetFrom(inRom, tblStart, tbl.VEnd - tblStart);
		byte[] outTable = GetFrom(outRom, tblStart, tbl.VEnd - tblStart);
		Array.Clear(outRom, tbl.VEnd, outRom.Length - tbl.VEnd);
		for (int i = 3; i < tblCount; i++)
		{
			tbl = TableEntry.GetTableEntry(inTable, i);
			if (tbl.PEnd == 0)
				inRom.Slice(tbl.PStart, tbl.Size)
					.CopyTo(outRom.Slice(tbl.VStart));
			else
				Decode(inRom.Slice(tbl.PStart),
					outRom.Slice(tbl.VStart), tbl.Size);

			tbl.PStart = tbl.VStart;
			tbl.PEnd = 0;
			outTable.Set(i * 16, tbl.GetNewTableEntry());
		}

		outRom.Set(tblStart, outTable);
		outRom.Set(0, CRC.GetNewCRC(outRom));

		return outRom;
	}

	private static Span<byte> Decode(Span<byte> src, Span<byte> dst, int size)
	{ /* Yaz0: http://www.amnoid.de/gc/yaz0.txt */
		int srcPlace = 16;
		int dstPlace = 0;
		int bitCount = 0;
		byte codeByte = 0;
		while (dstPlace < size)
		{
			if (bitCount == 0)
			{
				codeByte = src[srcPlace++];
				bitCount = 8;
			}
			if ((codeByte & 0x80u) != 0)
				dst[dstPlace++] = src[srcPlace++];
			else
			{
				Span<byte> bytes = src.Slice(srcPlace, 2);
				srcPlace += 2;
				int distance = ((bytes[0] & 0xF) << 8) | bytes[1];
				int copyPlace = dstPlace - (distance + 1);
				int numBytes = bytes[0] >> 4;
				numBytes = numBytes != 0 ? numBytes + 2 : src[srcPlace++] + 18;
				for (int i = 0; i < numBytes; i++)
					dst[dstPlace++] = dst[copyPlace++];
			}
			codeByte = (byte)(codeByte << 1);
			bitCount--;
		}

		return dst;
	}
}