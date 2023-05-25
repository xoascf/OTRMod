/* Licensed under the Open Software License version 3.0 */
// From OpenOcarinaBuilder.

namespace OTRMod.ROM;

public static class Decompressor {
	public const int DComSize = 0x2000000; // Default Compressed: 32MB ROM
	public const int PDecSize = 0x34D3040; // 52.8MB ROM (for PAL_1.0)
	public const int EDecSize = 0x3600000; // 54MB ROM (for EUR_MQD)
	public const int ADecSize = 0x4000000; // 64MB ROM

	public static byte[] Data(byte[] inROM, int outSize = PDecSize, bool calc = true) {
		// FIXME: Assumed to be decompressed if the ROM size is larger than 32MB
		if (inROM.Length > DComSize)
			return inROM;

		byte[] outROM = new byte[outSize];
		inROM.CopyTo(outROM, 0);

		int tblStart = TableEntry.FindTable(inROM);
		TableEntry tbl = TableEntry.Get(GetAllFrom(inROM, tblStart), 2);

		int tblCount = tbl.Size / 16;
		Debug.WriteLine("Number of files: " + tblCount);

		byte[] inTable  = inROM.Get(tblStart, tbl.VEnd - tblStart);
		byte[] outTable = outROM.Get(tblStart, tbl.VEnd - tblStart);

		Array.Clear(outROM, tbl.VEnd, outROM.Length - tbl.VEnd);

		for (int i = 3; i < tblCount; i++) {
			tbl = TableEntry.Get(inTable, i);

			switch (tbl.PEnd) {
				case -1: /* MM ROM, that's for sure */
					continue;

				case 0: /* Already decompressed */
					inROM.Slice(tbl.PStart, tbl.Size).CopyTo(outROM.Slice(tbl.VStart));
					break;

				default:
					Decode(inROM.Slice(tbl.PStart), outROM.Slice(tbl.VStart), tbl.Size);
					break;
			}

			tbl.PStart = tbl.VStart;
			tbl.PEnd = 0;
			outTable.Set(i * 16, tbl.GetNew());
		}

		outROM.Set(tblStart, outTable);

		if (calc) /* Recalculate CRC */
			outROM.Set(0, CRC.GetNewCRC(outROM));

		return outROM;
	}

	private static void Decode(Span<byte> src, Span<byte> dst, int size) {
		/* Yaz0: http://amnoid.de/gc/yaz0.txt */
		int srcPlace = 16;
		int dstPlace = 0;
		int bitCount = 0;
		byte codeByte = 0;

		while (dstPlace < size) {
			if (bitCount == 0) {
				codeByte = src[srcPlace++];
				bitCount = 8;
			}
			if ((codeByte & 0x80u) != 0)
				dst[dstPlace++] = src[srcPlace++];
			else {
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
	}
}