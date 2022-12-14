/* Licensed under the Open Software License version 3.0 */
// From OpenOcarinaBuilder.

using OTRMod.Utility;

namespace OTRMod.ROM;

public struct TableEntry {
	public int VStart; // Start Virtual
	public int VEnd;   // End Virtual

	public int PStart; // Start Physical
	public int PEnd;   // End Physical

	public int Size => VEnd - VStart;

	public static int FindTable(byte[] dt) {
		for (int i = 0; i + 16 < dt.Length; i += 16)
			if (dt.Get(i, 4).ToInt() == 2053467236 &&
			    dt.Get(i + 4, 4).ToInt() == 1631613810 &&
			    (dt.Get(i + 8, 4).ToInt() & 0xFF000000u) == 1677721600) {
				i += 16;
				int t;
				do { i += 16; t = dt.Get(i, 4).ToInt(); }
				while (t != 4192);
				return i - 16;
			}

		throw new Exception("Could not find file table!");
	}

	public static TableEntry Get(byte[] data, int i) {
		i *= 16;
		TableEntry e = new TableEntry();

		e.VStart = data.Get(i, 4).ToInt();
		e.VEnd   = data.Get(i + 4, 4).ToInt();
		e.PStart = data.Get(i + 8, 4).ToInt();
		e.PEnd   = data.Get(i + 12, 4).ToInt();

		return e;
	}

	public byte[] GetNew() {
		byte[] intData = new byte[16];

		intData.Set(0, ByteArray.FromInt(VStart));
		intData.Set(4, ByteArray.FromInt(VEnd));
		intData.Set(8, ByteArray.FromInt(PStart));
		intData.Set(12, ByteArray.FromInt(PEnd));

		return intData;
	}
}