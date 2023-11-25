/* Licensed under the Open Software License version 3.0 */

using OTRMod.Utility;

namespace OTRMod.ROM;

public struct TableEntry {
	/* Virtual */
	public int VStart;
	public int VEnd;

	/* Physical */
	public int PStart;
	public int PEnd;

	public readonly int Size => VEnd - VStart;

	public static int FindTable(byte[] dt) {
		for (int i = 0; i + 16 < dt.Length; i += 16)
			if (dt.ToI32(i) == 0x7A656C64 &&
			    dt.ToI32(i + 4) == 0x61407372 &&
			    (dt.ToI32(i + 8) & 0xFF000000u) == 0x64000000) {
				i += 16;
				int t;
				do { i += 16; t = dt.ToI32(i); }
				while (t != 0x1060);
				return i - 16;
			}

		throw new Exception("Couldn't find file table.");
	}

	public static TableEntry Get(byte[] data, int i) {
		i *= 16;
		TableEntry e = new() {
			VStart	= data.ToI32(i + 0x0),
			VEnd	= data.ToI32(i + 0x4),
			PStart	= data.ToI32(i + 0x8),
			PEnd	= data.ToI32(i + 0xC)
		};

		return e;
	}

	public readonly byte[] GetNew() {
		byte[] data = new byte[16];
		data.Set(0x0, ByteArray.FromI32(VStart));
		data.Set(0x4, ByteArray.FromI32(VEnd));
		data.Set(0x8, ByteArray.FromI32(PStart));
		data.Set(0xC, ByteArray.FromI32(PEnd));

		return data;
	}
}