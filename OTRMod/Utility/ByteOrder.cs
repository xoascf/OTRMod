/* Licensed under the Open Software License version 3.0 */

using EF = OTRMod.ID.Endianness;

namespace OTRMod.Utility;

public static class ByteOrder {
	public static EF Identify(byte[] input, byte[] magic) {
		if (magic.Length != input.Length)
			return EF.Unknown;

		if (magic.Matches(input))
			return EF.BigEndian;

		foreach (EF f in Enum.GetValues(typeof(EF))) {
			if (f is EF.BigEndian or EF.Unknown)
				continue;

			if (magic.Matches(input.CopyAs(f)))
				return f;
		}

		return EF.Unknown;
	}

	public static byte[] MoveBytes(byte[] data, int[] order) {
		byte[] array = new byte[4];

		/* FIXME: Add exception if data is not divisible by 4!! */

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

	public static byte[] ToBigEndian(byte[] data, EF format) {
		switch (format) {
			case EF.LittleEndian:
				MoveBytes(data, new[] { 3, 2, 1, 0 });
				break;

			case EF.ByteSwapped:
				MoveBytes(data, new[] { 1, 0, 3, 2 });
				break;

			case EF.WordSwapped:
				MoveBytes(data, new[] { 2, 3, 0, 1 });
				break;
		}

		return data;
	}

	public static byte[] DataTo(this byte[] b, EF f, int s = 0, int l = 4) {
		b.Set(s, ToBigEndian(b.Get(s, l), f));

		return b;
	}

	public static byte[] CopyAs(this byte[] a, EF f, int s = 0, int l = 4) {
		byte[] b = new byte[l];
		Buffer.BlockCopy(a, 0, b, 0, l);

		return b.DataTo(f, s, l);
	}
}