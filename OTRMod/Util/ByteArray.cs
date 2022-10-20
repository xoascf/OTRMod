/* Licensed under the Open Software License version 3.0 */
// From OpenOcarinaBuilder.

using System.Numerics;

namespace OTRMod.Util;

public static class ByteArray
{
	public static byte[] FromInt(int intVal)
	{ return Swap(BitConverter.GetBytes(intVal), 4); }

	public static byte[] FromUInt(uint intVal)
	{ return Swap(BitConverter.GetBytes(intVal), 4); }

	// https://stackoverflow.com/a/11013375
	public static byte[] FromString(string input)
	{
		byte[] b = BigInteger.Parse(input, NumberStyles.HexNumber).ToByteArray();
		Array.Reverse(b);
		return b;
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

	public static byte[] Swap(byte[] bytes, int count)
	{
		byte[] a = new byte[count];
		Buffer.BlockCopy(bytes, 0, a, 0, count);
		Array.Reverse(a, 0, a.Length);
		return a;
	}

	public static int ToInt(this byte[] bytes)
	{ return BitConverter.ToInt32(Swap(bytes, 4), 0); }

	public static uint ToUInt(byte[] bytes)
	{ return BitConverter.ToUInt32(Swap(bytes, 4), 0); }
}