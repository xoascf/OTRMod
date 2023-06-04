/* Licensed under the Open Software License version 3.0 */
// From OpenOcarinaBuilder.

using OTRMod.ID;
using OTRMod.Utility;

namespace OTRMod.ROM;

public static class Convert {
	private static readonly byte[] N64Header = "80371240".ReadHex();

	public static byte[] ToBigEndian(this byte[] bytes) {
		byte[] fileHeader = bytes.Get(0, 4);

		Endianness format = ByteOrder.Identify(fileHeader, N64Header);

		if (format == Endianness.Unknown)
			throw new Exception("ROM format is not valid!");

		switch (format) {
			case Endianness.BigEndian:
				// We don't convert anything.
				// But return the bytes anyway!
				return bytes;

			case Endianness.LittleEndian:
			case Endianness.ByteSwapped:
			case Endianness.WordSwapped:
				return ByteOrder.ToBigEndian(bytes, format);
		}

		return new byte[] { 0x00 };
	}
}