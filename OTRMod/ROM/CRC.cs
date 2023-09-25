/* Licensed under the Open Software License version 3.0 */

using OTRMod.Utility;
using System.Numerics;

namespace OTRMod.ROM;

public class CRC
{
	/* CIC seeds. */
	private const uint CIC6102 = 0xF8CA4DDC;
	private const uint CIC6103 = 0xA3886759;
	private const uint CIC6105 = 0xDF26F436;
	private const uint CIC6106 = 0x1FEA617A;

	private readonly byte[] _bytes;
	private static uint[]? _crcTable;

	public CRC(byte[] bytes) => _bytes = bytes;

	public static byte[] GetNewCRC(byte[] bytes) {
		new CRC(bytes).FixCRC();
		return _newCRCData!;
	}

	private static byte[]? _newCRCData;

	private static uint[] CRCTable {
		get {
			if (_crcTable != null)
				return _crcTable;
			_crcTable = new uint[256];
			for (int i = 0; i < 256; i++) {
				uint crc = (uint)i;
				for (int j = 8; j > 0; j--)
					crc = ((crc & 1) == 0) ? (crc >> 1) : ((crc >> 1) ^ 0xEDB88320u);
				_crcTable[i] = crc;
			}
			return _crcTable;
		}
	}

	public void FixCRC() {
		_newCRCData = _bytes.Get(0, 0x101000);
		uint[] array = CalculateCRC(_newCRCData);
		_newCRCData.Set(16, ByteArray.FromU32(array[0]));
		_newCRCData.Set(20, ByteArray.FromU32(array[1]));
	}

	private static uint CRC32(byte[] data) {
		uint crc = uint.MaxValue;
		foreach (byte b in data)
			crc = (crc >> 8) ^ CRCTable[(crc ^ b) & 0xFF];
		return ~crc;
	}

	private static int GetCIC(byte[] bytes) => CRC32(bytes.Get(0x40, 0xFC0)) switch {
		0x6170A4A1 => 6101,
		0x90BB6CB5 => 6102,
		0x0B050EE0 => 6103,
		0x98BC2C86 => 6105,
		0xACC8580A => 6106,
		_ => 6105,
	};

	private static uint[] CalculateCRC(byte[] bytes) {
		uint[] crc = new uint[2];
		int cic = GetCIC(bytes);
		uint seed = cic switch {
			6101 or 6102 => CIC6102,
			6103 => CIC6103,
			6105 => CIC6105,
			6106 => CIC6106,
			_ => throw new Exception("Invalid CIC."),
		};
		uint t2, t3, t4, t5, t6;
		uint t1 = t2 = t3 = t4 = t5 = t6 = seed;
		for (int i = 0x1000; i < 0x101000; i += 4) {
			uint d = bytes.Get(i, 4).ToU32();
			if ((t6 + d) < t6)
				t4++;

			t6 += d;
			t3 ^= d;
			uint r = BitOperations.RotateLeft(d, (int)(d & 0x1F));
			t5 += r;
			t2 = (t2 <= d) ? (t2 ^ (t6 ^ d)) : (t2 ^ r);
			t1 = (cic != 6105) ? (t1 + (t5 ^ d)) :
				(t1 + (bytes.Get(1872 + (i & 0xFF), 4).ToU32() ^ d));
		}
		switch (cic) {
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