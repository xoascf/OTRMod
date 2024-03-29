/* Licensed under the Open Software License version 3.0 */

using System.Text;
using static System.BitConverter;

namespace OTRMod.Utility;

public static class ByteArray {
	private static bool ShouldRev(bool big)
		=> (IsLittleEndian && big) || (!IsLittleEndian && !big);

	public static byte[] Reverse(this byte[] data) { Array.Reverse(data); return data; }

	public static byte[] FromI16(short value, bool big = true) /* 2 bytes */
		=> ShouldRev(big) ? Reverse(GetBytes(value)) : GetBytes(value);

	public static byte[] FromU16(ushort value, bool big = true) /* 2 bytes */
		=> ShouldRev(big) ? Reverse(GetBytes(value)) : GetBytes(value);

	public static byte[] FromI32(int value, bool big = true) /* 4 bytes */
		=> ShouldRev(big) ? Reverse(GetBytes(value)) : GetBytes(value);

	public static byte[] FromU32(uint value, bool big = true) /* 4 bytes */
		=> ShouldRev(big) ? Reverse(GetBytes(value)) : GetBytes(value);

	public static byte[] FromF32(float value, bool big = true) /* 4 bytes */
		=> ShouldRev(big) ? Reverse(GetBytes(value)) : GetBytes(value);

	public static byte[] FromU64(ulong value, bool big = true) /* 8 bytes */
		=> ShouldRev(big) ? Reverse(GetBytes(value)) : GetBytes(value);

	public static int ToI32(this byte[] data, bool big = true)
		=> ShouldRev(big) ? ToInt32(Reverse(data), 0) : ToInt32(data, 0);

	public static int ToI32(this byte[] data, int offset, bool big = true)
		=> ToI32(data.Get(offset, 4), big);

	public static short ToI16(this byte[] data, bool big = true)
		=> ShouldRev(big) ? ToInt16(Reverse(data), 0) : ToInt16(data, 0);

	public static short ToI16(this byte[] data, int offset, bool big = true)
		=> ToI16(data.Get(offset, 2), big);

	public static ushort ToU16(this byte[] data, bool big = true)
		=> ShouldRev(big) ? ToUInt16(Reverse(data), 0) : ToUInt16(data, 0);

	public static ushort ToU16(this byte[] data, int offset, bool big = true)
		=> ToU16(data.Get(offset, 2), big);

	public static uint ToU32(this byte[] data, bool big = true)
		=> ShouldRev(big) ? ToUInt32(Reverse(data), 0) : ToUInt32(data, 0);

	public static uint ToU32(this byte[] data, int offset, bool big = true)
		=> ToU32(data.Get(offset, 4), big);

	public static float ToF32(this byte[] data, bool big = true)
		=> ShouldRev(big) ? ToSingle(Reverse(data), 0) : ToSingle(data, 0);

	public static float ToF32(this byte[] data, int offset, bool big = true)
		=> ToF32(data.Get(offset, 4), big);

	public static ulong ToU64(this byte[] data, bool big = true)
		=> ShouldRev(big) ? ToUInt64(Reverse(data), 0) : ToUInt64(data, 0);

	public static ulong ToU64(this byte[] data, int offset, bool big = true)
		=> ToU64(data.Get(offset, 8), big);

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

	public static string[] ToStringArray(this string text) {
		return text.Split(Separators, StringSplitOptions.None);
	}

	public static string[] ToStringArray(this byte[] data, Encoding? encoding = null) {
		encoding ??= SturmScharf.EncodingProvider.Latin1;
		return encoding.GetString(data).ToStringArray();
	}
}