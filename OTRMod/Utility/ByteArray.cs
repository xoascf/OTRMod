/* Licensed under the Open Software License version 3.0 */

using System.Text;
using static System.BitConverter;

namespace OTRMod.Utility;

public static class ByteArray {
	private static bool ShouldRev(bool big)
		=> (IsLittleEndian && big) || (!IsLittleEndian && !big);

	public static byte[] Reverse(this byte[] data) { Array.Reverse(data); return data; }

	public static byte[] FromI32(int value, bool big = true) /* 4 bytes */
		=> ShouldRev(big) ? Reverse(GetBytes(value)) : GetBytes(value);

	public static byte[] FromU32(uint value, bool big = true) /* 4 bytes */
		=> ShouldRev(big) ? Reverse(GetBytes(value)) : GetBytes(value);

	public static byte[] FromU64(ulong value, bool big = true) /* 8 bytes */
		=> ShouldRev(big) ? Reverse(GetBytes(value)) : GetBytes(value);

	public static int ToI32(this byte[] data, bool big = true)
		=> ShouldRev(big) ? ToInt32(Reverse(data), 0) : ToInt32(data, 0);

	public static uint ToU32(this byte[] data, bool big = true)
		=> ShouldRev(big) ? ToUInt32(Reverse(data), 0) : ToUInt32(data, 0);

	public static byte[] ReadHex(this string hex) {
		if (hex.Length % 2 != 0)
			throw new ArgumentException("Invalid length.", nameof(hex));

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
				if (pattern[l] == input[i + l])
					l++;
				else
					break;
			if (l == pl)
				input.Set(i, to);
		}
	}

	public static bool Matches(this byte[] a, byte[] b) {
		if (a.Length != b.Length)
			return false;

		for (int i = 0; i < a.Length; i++)
			if (a[i] != b[i])
				return false;

		return true;
	}

	public static byte[] DataTo(this byte[] b, ByteOrder f, int s = 0, int l = 4) {
		b.Set(s, b.Get(s, l).ToBigEndian(f));

		return b;
	}

	public static byte[] CopyAs(this byte[] a, ByteOrder f, int s = 0, int l = 4) {
		byte[] b = new byte[l];
		Buffer.BlockCopy(a, 0, b, 0, l);

		return b.DataTo(f, s, l);
	}

	internal static readonly string[] Separators = { "\n", "\r\n", "\r" };

	public static string[] ToStringArray(this byte[] data, Encoding encoding) {
		return encoding.GetString(data).Split(Separators, StringSplitOptions.None);
	}
}