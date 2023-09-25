/* Licensed under the Open Software License version 3.0 */

namespace OTRMod.ROM;

public static class Decompressor {
	public static byte[] Data(byte[] inROM, int outSize = Size.PDec, bool calc = true) {
		/* Assumes that the ROM is decompressed if its size is larger than 32MB. */
		if (inROM.Length > Size.DCom)
			return inROM;

		byte[] outROM = new byte[outSize];
		inROM.CopyTo(outROM, 0);

		int tblStart = TableEntry.FindTable(inROM);
		TableEntry tbl = TableEntry.Get(GetAllFrom(inROM, tblStart), 2);

		int tblCount = tbl.Size / 16;
		Debug.WriteLine($"Number of files: {tblCount}.");

		byte[] inTable = inROM.Get(tblStart, tbl.VEnd - tblStart);
		byte[] outTable = outROM.Get(tblStart, tbl.VEnd - tblStart);

		Array.Clear(outROM, tbl.VEnd, outROM.Length - tbl.VEnd);

		for (int i = 3; i < tblCount; i++) {
			tbl = TableEntry.Get(inTable, i);

			switch (tbl.PEnd) {
				case -1: /* MM ROM, that's for sure */
					continue;

				case 0: /* Already decompressed */
#if NETCOREAPP2_1_OR_GREATER
					inROM.Slice(tbl.PStart, tbl.Size).CopyTo(outROM.Slice(tbl.VStart));
#else
					outROM.Set(tbl.VStart, inROM.Get(tbl.PStart, tbl.Size));
#endif
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

	/* Yaz0: http://amnoid.de/gc/yaz0.txt */
#if NETCOREAPP2_1_OR_GREATER
	private static void Decode(Span<byte> srcArray, Span<byte> dstArray, int size) {
		int srcPlace = 16;
		int dstOffset = 0;
#else
	private static void Decode(ArraySegment<byte> src, ArraySegment<byte> dst, int size) {
		byte[] srcArray = src.Array;
		byte[] dstArray = dst.Array;
		int srcPlace = src.Offset + 16;
		int dstOffset = dst.Offset;
#endif
		int dstPlace = dstOffset;
		int bitCount = 0;

		byte codeByte = 0;

		while (dstPlace - dstOffset < size) {
			if (bitCount == 0) {
				codeByte = srcArray[srcPlace++];
				bitCount = 8;
			}
			if ((codeByte & 0x80u) != 0) {
				dstArray[dstPlace++] = srcArray[srcPlace++];
			}
			else {
#if NETCOREAPP2_1_OR_GREATER
				Span<byte> bytes = srcArray.Slice(srcPlace, 2);
#else
				byte[] bytes = srcArray.Get(srcPlace, 2);
#endif
				srcPlace += 2;

				int distance = ((bytes[0] & 0xF) << 8) | bytes[1];
				int copyPlace = dstPlace - (distance + 1);
				int numBytes = bytes[0] >> 4;

				numBytes = numBytes != 0 ? numBytes + 2 : srcArray[srcPlace++] + 18;

				for (int i = 0; i < numBytes; i++)
					dstArray[dstPlace++] = dstArray[copyPlace++];
			}

			codeByte = (byte)(codeByte << 1);
			bitCount--;
		}
	}
}