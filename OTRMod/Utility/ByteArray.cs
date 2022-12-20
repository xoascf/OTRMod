/* Licensed under the Open Software License version 3.0 */
// From OpenOcarinaBuilder.

using System.Numerics;

namespace OTRMod.Utility;

public static class ByteArray {
	private static readonly bool IsLittleEndian = BitConverter.IsLittleEndian;

	public static byte[] FromInt(int value) {
		byte[] b = BitConverter.GetBytes(value);

		return IsLittleEndian ?
			b.CopyAs(ByteOrder.Format.LittleEndian) : b;
	}

	public static byte[] FromUInt(uint value) {
		byte[] b = BitConverter.GetBytes(value);

		return IsLittleEndian ?
			b.CopyAs(ByteOrder.Format.LittleEndian) : b;
	}

	// https://stackoverflow.com/a/11013375
	public static byte[] ReadHEX(this string input) {
		byte[] b = BigInteger.Parse(input, NumberStyles.HexNumber).ToByteArray();

		return IsLittleEndian ?
			b.DataTo(ByteOrder.Format.LittleEndian, 0, b.Length) : b;
	}

	// https://stackoverflow.com/a/26880541
	public static void Replace(this byte[] input, byte[] pattern, byte[] to) {
		int length = pattern.Length;
		int limit = input.Length - length;
		for (int i = 0; i <= limit; i++) {
			int l = 0;
			while (l < length)
				if (pattern[l] == input[i + l])
					l++;
				else
					break;
			if (l == length)
				input.Set(i, to);
		}
	}

	public static bool Matches(this ReadOnlySpan<byte> a, ReadOnlySpan<byte> b) {
		return a.SequenceEqual(b);
	}

	public static int ToInt(this byte[] bytes) {
		return IsLittleEndian ?
			BitConverter.ToInt32(bytes.CopyAs(ByteOrder.Format.LittleEndian)) :
			BitConverter.ToInt32(bytes);
	}

	public static uint ToUInt(byte[] bytes) {
		return IsLittleEndian ?
			BitConverter.ToUInt32(bytes.CopyAs(ByteOrder.Format.LittleEndian)) :
			BitConverter.ToUInt32(bytes);
	}
}