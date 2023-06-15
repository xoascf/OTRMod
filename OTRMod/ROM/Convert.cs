/* Licensed under the Open Software License version 3.0 */

using OTRMod.Utility;

namespace OTRMod.ROM;

public static class Convert {
	private static readonly byte[] N64Header = { 0x80, 0x37, 0x12, 0x40 };

	private static ByteOrder Identify(byte[] input, byte[] magic) {
		if (magic.Length != input.Length)
			return ByteOrder.Unknown;

		if (magic.Matches(input))
			return ByteOrder.BigEndian;

		foreach (ByteOrder f in Enum.GetValues(typeof(ByteOrder))) {
			if (f is ByteOrder.BigEndian or ByteOrder.Unknown)
				continue;

			if (magic.Matches(input.CopyAs(f)))
				return f;
		}

		return ByteOrder.Unknown;
	}

	public static byte[] ToBigEndian(this byte[] bytes) {
		ByteOrder order = Identify(bytes.Get(0, 4), N64Header);

		return order switch {
			ByteOrder.Unknown => throw new Exception("Invalid ROM format."),
			ByteOrder.BigEndian => bytes, /* Just return the bytes! */
			ByteOrder.LittleEndian or ByteOrder.ByteSwapped or
			ByteOrder.WordSwapped => bytes.ToBigEndian(from: order),
			_ => new byte[] { 0x00 }
		};
	}
}