/* Licensed under the Open Software License version 3.0 */
// From OpenOcarinaBuilder.

namespace OTRMod.ROM;

internal class ByteOrder
{
	public enum Format
	{
		BigEndian, // .z64
		LittleEndian, // .n64
		ByteSwapped, // .v64
		WordSwapped, // .u64
		Unknown
	}

	public static Format IdentifyFormat(byte[] b64)
	{
		Format format = Format.Unknown;

		if (b64.Length < 4)

			return format;

		if (b64[0] == 0x80 &&
		    b64[1] == 0x37 &&
		    b64[2] == 0x12 &&
		    b64[3] == 0x40)
			format = Format.BigEndian;
		else if (b64[0] == 0x40 &&
		         b64[1] == 0x12 &&
		         b64[2] == 0x37 &&
		         b64[3] == 0x80)
			format = Format.LittleEndian;
		else if (b64[0] == 0x37 &&
		         b64[1] == 0x80 &&
		         b64[2] == 0x40 &&
		         b64[3] == 0x12)
			format = Format.ByteSwapped;
		else if (b64[0] == 0x12 &&
		         b64[1] == 0x40 &&
		         b64[2] == 0x80 &&
		         b64[3] == 0x37)
			format = Format.WordSwapped;

		return format;
	}

	public static byte[] MoveBytes(byte[] data, int[] order)
	{
		byte[] array = new byte[4];

		for (int i = 0; i < data.Length / 4; i++)
		{
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

	public static byte[] ToBigEndian(byte[] data, Format format)
	{
		switch (format)
		{
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
}