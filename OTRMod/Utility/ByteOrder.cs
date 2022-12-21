/* Licensed under the Open Software License version 3.0 */
// From OpenOcarinaBuilder.

namespace OTRMod.Utility;

public static class ByteOrder {
	public enum Format {
		BigEndian,    // .z64
		LittleEndian, // .n64
		ByteSwapped,  // .v64
		WordSwapped,  // .u64
		Unknown
	}

	public static Format Identify(byte[] input, ReadOnlySpan<byte> magic) {
		if (magic.Length != input.Length)
			return Format.Unknown;

		if (magic.Matches(input))
			return Format.BigEndian;

		foreach (Format f in Enum.GetValues(typeof(Format))) {
			if (f is Format.BigEndian
				  or Format.Unknown)
				continue;

			if (magic.Matches(input.CopyAs(f)))
				return f;
		}

		return Format.Unknown;
	}

	public static byte[] MoveBytes(byte[] data, int[] order) {
		byte[] array = new byte[4];

		for (int i = 0; i < data.Length / 4; i++) {
			array[0] = data[i * 4 + order[0]];
			array[1] = data[i * 4 + order[1]];
			array[2] = data[i * 4 + order[2]];
			array[3] = data[i * 4 + order[3]];

			data[i * 4 + 0] = array[0];
			data[i * 4 + 1] = array[1];
			data[i * 4 + 2] = array[2];
			data[i * 4 + 3] = array[3];
		}

		return data;
	}

	public static byte[] ToBigEndian(byte[] data, Format format) {
		switch (format) {
			case Format.LittleEndian:
				MoveBytes(data, new[] { 3, 2, 1, 0 });
				break;

			case Format.ByteSwapped:
				MoveBytes(data, new[] { 1, 0, 3, 2 });
				break;

			case Format.WordSwapped:
				MoveBytes(data, new[] { 2, 3, 0, 1 });
				break;
		}

		return data;
	}

	public static void To(this byte[] data, Format f, int s = 0, int l = 4)
	{
		data.Set(s, ToBigEndian(data.Get(s, l), f));
	}

	public static byte[] DataTo(this byte[] data, Format f, int s = 0, int l = 4) {
		data.To(f, s, l);

		return data;
	}

	public static byte[] CopyAs(this byte[] a, Format f, int s = 0, int l = 4) {
		byte[] b = new byte[l];

		Buffer.BlockCopy(a, 0, b, 0, l);

		return b.DataTo(f, s, l);
	}
}