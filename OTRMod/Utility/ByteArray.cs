/* Licensed under the Open Software License version 3.0 */

namespace OTRMod.Utility;

public static class ByteArray {
	private static byte[] EndianConvert(byte[] b) {
		if (BitConverter.IsLittleEndian) Array.Reverse(b);
		
		return b;
	}

	public static byte[] FromInt(int v) => EndianConvert(BitConverter.GetBytes(v));

	public static byte[] FromUInt(uint v) => EndianConvert(BitConverter.GetBytes(v));

	public static byte[] ReadHex(this string hex) {
		if (hex.Length % 2 != 0) throw new ArgumentException("Invalid length", nameof(hex));

		int c = hex.Length / 2;
		byte[] b = new byte[c];

		for (int i = 0; i < c; i++)
			b[i] = (byte)(Convert.ToUInt16(hex.Substring(i * 2, 2), 16) & 0xFF);

		return b;
	}

	// https://stackoverflow.com/a/26880541
	public static void Replace(this byte[] input, byte[] pattern, byte[] to) {
		int pl = pattern.Length;
		int limit = input.Length - pl;
		for (int i = 0; i <= limit; i++) {
			int l = 0;
			while (l < pl)
				if (pattern[l] == input[i + l]) l++; else break;
			if (l == pl) input.Set(i, to);
		}
	}

	public static bool Matches(this byte[] a, byte[] b) {
		if (a.Length != b.Length) return false;

		for (int i = 0; i < a.Length; i++)
			if (a[i] != b[i]) return false;

		return true;
	}

	public static int ToInt(this byte[] b) => BitConverter.ToInt32(EndianConvert(b));

	public static uint ToUInt(byte[] b) => BitConverter.ToUInt32(EndianConvert(b));
}