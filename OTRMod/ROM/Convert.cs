/* Licensed under the Open Software License version 3.0 */
// From OpenOcarinaBuilder.

using OTRMod.Utility;

namespace OTRMod.ROM;

public static class Convert
{
	private static readonly byte[] N64Header = ByteArray.FromString("80371240");

	public static byte[] ToBigEndian(byte[] bytes)
	{
		byte[] fileHeader = GetFrom(bytes, 0, 4);

		ByteOrder.Format format = ByteOrder.Identify(fileHeader, N64Header);

		if (format == ByteOrder.Format.Unknown)
			throw new Exception("ROM format is not valid!");

		switch (format)
		{
			case ByteOrder.Format.BigEndian:
				// We don't convert anything.
				// But return the bytes anyway!
				return bytes;

			case ByteOrder.Format.LittleEndian:
			case ByteOrder.Format.ByteSwapped:
			case ByteOrder.Format.WordSwapped:
				return ByteOrder.ToBigEndian(bytes, format);
		}

		return new byte[] { 0x00 };
	}
}