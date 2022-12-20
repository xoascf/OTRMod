/* Licensed under the Open Software License version 3.0 */
// From OpenOcarinaBuilder.

using OTRMod.Utility;

namespace OTRMod.ROM;

public class CRC
{
	/* CIC seeds. */
	private const uint CIC6102 = 0xF8CA4DDC;
	private const uint CIC6103 = 0xA3886759;
	private const uint CIC6105 = 0xDF26F436;
	private const uint CIC6106 = 0x1FEA617A;

	private byte[] _bytes;
	private byte[] Bytes { get { return _bytes; }  set { _bytes = value; } }

	private static uint[] _crcTable;
	private static uint[] CRCTable
	{
		get
		{
			if (_crcTable != null)
				return _crcTable;
			_crcTable = new uint[256];
			for (int i = 0; i < 256; i++)
			{
				uint crc = (uint)i;
				for (int j = 8; j > 0; j--)
					crc = ((crc & 1) == 0) ? (crc >> 1) :
						((crc >> 1) ^ 0xEDB88320u);
				_crcTable[i] = crc;
			}
			return _crcTable;
		}
	}

	public CRC(byte[] bytes)
	{
		Bytes = bytes;
	}

	public static byte[] GetNewCRC(byte[] bytes)
	{
		new CRC(bytes).FixCRC();
		return newCRCbytes;
	}

	private static byte[] newCRCbytes;

	public void FixCRC()
	{
		newCRCbytes = Bytes.Get(0, 1052672);
		uint[] array = CalculateCRC(newCRCbytes);
		newCRCbytes.Set(16, ByteArray.FromUInt(array[0]));
		newCRCbytes.Set(20, ByteArray.FromUInt(array[1]));
	}

	private static uint ROL(uint i, int b)
	{
		return (i << b) | (i >> 32 - b);
	}

	private static uint CRC32(byte[] data)
	{
		uint crc = uint.MaxValue;
		for (int i = 0; i < data.Length; i++)
		{
			byte b = data[i];
			crc = (crc >> 8) ^ CRCTable[(crc ^ b) & 0xFF];
		}
		return ~crc;
	}

	private static int GetCIC(byte[] bytes)
	{
		switch (CRC32(bytes.Get(64, 4032)))
		{
			case 0x6170A4A1: return 6101;
			case 0x90BB6CB5: return 6102;
			case 0x0B050EE0: return 6103;
			case 0x98BC2C86: return 6105;
			case 0xACC8580A: return 6106;
			default: return 0;
		}
	}

	private static uint[] CalculateCRC(byte[] bytes)
	{
		uint[] crc = new uint[2];
		int cic = GetCIC(bytes);
		uint seed;
		switch (cic)
		{
			case 6101:
			case 6102:
				seed = CIC6102;
				break;

			case 6103:
				seed = CIC6103;
				break;

			case 6105:
				seed = CIC6105;
				break;

			case 6106:
				seed = CIC6106;
				break;

			default:
				throw new Exception("CIC not valid!");
		}
		uint t1, t2, t3, t4, t5, t6;
		t1 = t2 = t3 = t4 = t5 = t6 = seed;
		for (int i = 4096; i < 1052672; i += 4)
		{
			uint d = ByteArray.ToUInt(bytes.Get(i, 4));
			if ((t6 + d) < t6)
				t4++;

			t6 += d;
			t3 ^= d;
			uint r = ROL(d, (int)(d & 0x1F));
			t5 += r;
			t2 = (t2 <= d) ? (t2 ^ (t6 ^ d)) : (t2 ^ r);
			t1 = (cic != 6105) ? (t1 + (t5 ^ d)) :
				(t1 + (ByteArray.ToUInt
					(bytes.Get(1872 + (i & 0xFF), 4)) ^ d));
		}
		switch (cic)
		{
			case 6103:
				crc[0] = (t6 ^ t4) + t3;
				crc[1] = (t5 ^ t2) + t1;
				break;

			case 6106:
				crc[0] = t6 * t4 + t3;
				crc[1] = t5 * t2 + t1;
				break;

			default:
				crc[0] = t6 ^ t4 ^ t3;
				crc[1] = t5 ^ t2 ^ t1;
				break;
		}

		return crc;
	}
}