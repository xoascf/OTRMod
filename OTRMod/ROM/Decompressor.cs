/* Licensed under the Open Software License version 3.0 */

namespace OTRMod.ROM;

public static class Decompressor {
	public static byte[] Data(byte[] inROM, int outSize = Size.PDec, bool calc = true) {
		int tblStart = TableEntry.FindTable(inROM);
		
		byte[] dmadataSpan = GetAllFrom(inROM, tblStart);
		TableEntry dmadataEntry = TableEntry.Get(dmadataSpan, 2);
		int tblCount = dmadataEntry.Size / 16;
		Debug.WriteLine($"Number of files: {tblCount}.");

		byte[] inTable = inROM.Get(tblStart, dmadataEntry.Size);

		/* Check if already decompressed... */
		bool isCompressed = false;
		for (int i = 0; i < tblCount; i++) {
			TableEntry entry = TableEntry.Get(inTable, i);
			if (entry.PStart == -1 || entry.VStart == -1 || entry.PEnd == -1 || entry.VEnd == -1)
				continue;
			if (entry.PEnd != 0 && entry.PEnd != entry.PStart) {
				isCompressed = true;
				break;
			}
		}

		if (!isCompressed) {
			Debug.WriteLine("ROM is already decompressed.");
			return inROM;
		}

		byte[] outTable = new byte[dmadataEntry.Size];

		long requiredDstSize = inROM.Length;
		for (int i = 0; i < tblCount; i++) {
			TableEntry entry = TableEntry.Get(inTable, i);
			if (entry.VEnd > requiredDstSize) {
				requiredDstSize = entry.VEnd;
			}
		}

		int finalSize = (int)requiredDstSize;
		if (outSize > finalSize) {
			finalSize = outSize;
		} else {
			int dstSz = inROM.Length;
			while (dstSz < finalSize) {
				dstSz *= 2;
			}
			finalSize = dstSz;
		}

		byte[] outROM = new byte[finalSize];

		for (int i = 0; i < tblCount; i++) {
			TableEntry tbl = TableEntry.Get(inTable, i);

			if (tbl.PStart == -1 || tbl.VStart == -1 || tbl.PEnd == -1 || tbl.VEnd == -1 || tbl.VEnd <= tbl.VStart || (tbl.PEnd != 0 && tbl.PEnd == tbl.PStart)) {
				outTable.Set(i * 16, tbl.GetNew());
				continue;
			}

			if (tbl.PEnd != 0) {
				int uncompSize = tbl.VEnd - tbl.VStart;
				Decode(inROM.Slice(tbl.PStart), outROM.Slice(tbl.VStart), uncompSize);
			} else {
				int size = tbl.VEnd - tbl.VStart;
#if NETCOREAPP2_1_OR_GREATER
				inROM.Slice(tbl.PStart, size).CopyTo(outROM.Slice(tbl.VStart));
#else
				outROM.Set(tbl.VStart, inROM.Get(tbl.PStart, size));
#endif
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
		string header = System.Text.Encoding.ASCII.GetString(srcArray.Slice(0, 4));
		switch (header) {
			case "Yaz0":
				DecodeYaz0(srcArray, dstArray, size);
				break;
			case "ZLIB":
				DecodeZlib(srcArray, dstArray, size);
				break;
			case "LZO0":
				DecodeLzo(srcArray, dstArray, size);
				break;
			case "UCL0":
				DecodeUcl(srcArray, dstArray, size);
				break;
			case "APL0":
				DecodeApl(srcArray, dstArray, size);
				break;
			default:
				throw new Exception($"Unknown compression codec: {header}");
		}
	}

	private static void DecodeYaz0(Span<byte> srcArray, Span<byte> dstArray, int size) {
		int srcPlace = 16;
		int dstOffset = 0;
#else
	private static void Decode(ArraySegment<byte> src, ArraySegment<byte> dst, int size) {
		byte[] srcArray = src.Array;
		string header = System.Text.Encoding.ASCII.GetString(srcArray, src.Offset, 4);
		switch (header) {
			case "Yaz0":
				DecodeYaz0(src, dst, size);
				break;
			case "ZLIB":
				DecodeZlib(src, dst, size);
				break;
			case "LZO0":
				DecodeLzo(src, dst, size);
				break;
			case "UCL0":
				DecodeUcl(src, dst, size);
				break;
			case "APL0":
				DecodeApl(src, dst, size);
				break;
			default:
				throw new Exception($"Unknown compression codec: {header}");
		}
	}

	private static void DecodeYaz0(ArraySegment<byte> src, ArraySegment<byte> dst, int size) {
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

#if NETCOREAPP2_1_OR_GREATER
	private static void DecodeZlib(Span<byte> srcArray, Span<byte> dstArray, int size) {
		using var ms = new System.IO.MemoryStream(srcArray.Slice(8).ToArray());
		using var zs = new Ionic.Zlib.ZlibStream(ms, Ionic.Zlib.CompressionMode.Decompress);
		byte[] buffer = new byte[size];
		int bytesRead = 0;
		while (bytesRead < size) {
			int read = zs.Read(buffer, bytesRead, size - bytesRead);
			if (read == 0) break;
			bytesRead += read;
		}
		buffer.AsSpan(0, bytesRead).CopyTo(dstArray);
	}

	private static void DecodeLzo(Span<byte> srcArray, Span<byte> dstArray, int size) {
		throw new NotImplementedException("LZO decompression is not yet implemented.");
	}

	private static void DecodeUcl(Span<byte> srcArray, Span<byte> dstArray, int size) {
		throw new NotImplementedException("UCL decompression is not yet implemented.");
	}

	private static void DecodeApl(Span<byte> srcArray, Span<byte> dstArray, int size) {
		throw new NotImplementedException("APLib decompression is not yet implemented.");
	}
#else
	private static void DecodeZlib(ArraySegment<byte> src, ArraySegment<byte> dst, int size) {
		using var ms = new System.IO.MemoryStream(src.Array, src.Offset + 8, src.Count - 8);
		using var zs = new Ionic.Zlib.ZlibStream(ms, Ionic.Zlib.CompressionMode.Decompress);
		int bytesRead = 0;
		while (bytesRead < size) {
			int read = zs.Read(dst.Array, dst.Offset + bytesRead, size - bytesRead);
			if (read == 0) break;
			bytesRead += read;
		}
	}

	private static void DecodeLzo(ArraySegment<byte> src, ArraySegment<byte> dst, int size) {
		throw new NotImplementedException("LZO decompression is not yet implemented.");
	}

	private static void DecodeUcl(ArraySegment<byte> src, ArraySegment<byte> dst, int size) {
		throw new NotImplementedException("UCL decompression is not yet implemented.");
	}

	private static void DecodeApl(ArraySegment<byte> src, ArraySegment<byte> dst, int size) {
		throw new NotImplementedException("APLib decompression is not yet implemented.");
	}
#endif
}