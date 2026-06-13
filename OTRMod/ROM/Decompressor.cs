/* Licensed under the Open Software License version 3.0 */

using OTRMod.Utility;

namespace OTRMod.ROM;

public enum Codec {
	None,
	Yaz0,
	Zlib,
	Lzo,
	Ucl,
	Apl
}

public static class Decompressor {
	private static Codec GetCodec(byte[] rom, int offset) {
		int header = rom.ToI32(offset);
		return header switch {
			0x59617A30 => Codec.Yaz0, // "Yaz0"
			0x5A4C4942 => Codec.Zlib, // "ZLIB"
			0x4C5A4F30 => Codec.Lzo,  // "LZO0"
			0x55434C30 => Codec.Ucl,  // "UCL0"
			0x41504C30 => Codec.Apl,  // "APL0"
			_ => Codec.None
		};
	}

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

			Codec codec = Codec.None;
			if (tbl.PEnd != 0) {
				codec = GetCodec(inROM, tbl.PStart);
			}

			int size = tbl.VEnd - tbl.VStart;
			if (codec != Codec.None) {
				Decode(codec, inROM.Slice(tbl.PStart), outROM.Slice(tbl.VStart), size);
			} else {
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
	private static void Decode(Codec codec, Span<byte> srcArray, Span<byte> dstArray, int size) {
		switch (codec) {
			case Codec.Yaz0:
				DecodeYaz0(srcArray, dstArray, size);
				break;
			case Codec.Zlib:
				DecodeZlib(srcArray, dstArray, size);
				break;
			case Codec.Lzo:
				DecodeLzo(srcArray, dstArray, size);
				break;
			case Codec.Ucl:
				DecodeUcl(srcArray, dstArray, size);
				break;
			case Codec.Apl:
				DecodeApl(srcArray, dstArray, size);
				break;
			default:
				throw new ArgumentOutOfRangeException(nameof(codec), $"Unknown compression codec: {codec}");
		}
	}

	private static void DecodeYaz0(Span<byte> srcArray, Span<byte> dstArray, int size) {
		int srcPlace = 16;
		int dstOffset = 0;
#else
	private static void Decode(Codec codec, ArraySegment<byte> src, ArraySegment<byte> dst, int size) {
		switch (codec) {
			case Codec.Yaz0:
				DecodeYaz0(src, dst, size);
				break;
			case Codec.Zlib:
				DecodeZlib(src, dst, size);
				break;
			case Codec.Lzo:
				DecodeLzo(src, dst, size);
				break;
			case Codec.Ucl:
				DecodeUcl(src, dst, size);
				break;
			case Codec.Apl:
				DecodeApl(src, dst, size);
				break;
			default:
				throw new ArgumentOutOfRangeException(nameof(codec), $"Unknown compression codec: {codec}");
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
				int b0 = srcArray[srcPlace];
				int b1 = srcArray[srcPlace + 1];
				srcPlace += 2;

				int distance = ((b0 & 0xF) << 8) | b1;
				int copyPlace = dstPlace - (distance + 1);
				int numBytes = b0 >> 4;

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