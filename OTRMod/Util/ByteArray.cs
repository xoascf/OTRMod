/* Licensed under the Open Software License version 3.0 */
// From OpenOcarinaBuilder.

using System.Numerics;

namespace OTRMod.Util;

public static class ByteArray
{
	public static byte[] FromInt(int value)
	{ return BitConverter.GetBytes(value).CopyAs(ByteOrder.Format.LittleEndian); }

	public static byte[] FromUInt(uint value)
	{ return BitConverter.GetBytes(value).CopyAs(ByteOrder.Format.LittleEndian); }

	// https://stackoverflow.com/a/11013375
	public static byte[] FromString(string input)
	{
		byte[] b = BigInteger.Parse(input, NumberStyles.HexNumber).ToByteArray();
		return b.DataTo(ByteOrder.Format.LittleEndian, 0, b.Length);
	}

	// https://stackoverflow.com/a/26880541
	public static void Replace(this byte[] input, byte[] pattern, byte[] newPattern)
	{
		int length = pattern.Length;
		int limit = input.Length - length;
		for (int i = 0; i <= limit; i++)
		{
			int l = 0;
			while (l < length)
				if (pattern[l] == input[i + l])
					l++;
				else
					break;
			if (l == length)
				input.Set(i, newPattern);
		}
	}

	public static bool Matches(this ReadOnlySpan<byte> a, ReadOnlySpan<byte> b)
	{
		return a.SequenceEqual(b);
	}

	public static int ToInt(this byte[] bytes)
	{ return BitConverter.ToInt32(bytes.CopyAs(ByteOrder.Format.LittleEndian)); }

	public static uint ToUInt(byte[] bytes)
	{ return BitConverter.ToUInt32(bytes.CopyAs(ByteOrder.Format.LittleEndian)); }
}