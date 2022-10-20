/* Licensed under the Open Software License version 3.0 */
// From OpenOcarinaBuilder.

namespace OTRMod.ROM;

public class Convert
{
	public static byte[] ToBigEndian(byte[] bytes)
	{
		ByteOrder.Format format = ByteOrder.IdentifyFormat
			(GetFrom(bytes, 0, 0x40));

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