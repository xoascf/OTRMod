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

	private readonly byte[] bytes;
	private static uint[]? crcTable;

	public CRC(byte[] bytes) => this.bytes = bytes;

	public static byte[] GetNewCRC(byte[] bytes) {
		new CRC(bytes).FixCRC();
		return newCRCbytes!;
	}

	private static byte[]? newCRCbytes;

	private static uint[] CRCTable {
		get {
			if (crcTable == null) {
				crcTable = new uint[256];
				for (int i = 0; i < 256; i++) {
					uint crc = (uint)i;
					for (int j = 8; j > 0; j--)
						crc = ((crc & 1) == 0) ? (crc >> 1) : ((crc >> 1) ^ 0xEDB88320u);
					crcTable[i] = crc;
				}
			}
			return crcTable;
		}
	}

	private static uint ROL(uint i, int b) => (i << b) | (i >> 32 - b);

	public void FixCRC() {
		newCRCbytes = bytes.Get(0, 1052672);
		uint[] array = CalculateCRC(newCRCbytes);
		newCRCbytes.Set(16, ByteArray.FromUInt(array[0]));
		newCRCbytes.Set(20, ByteArray.FromUInt(array[1]));
	}

	private static uint CRC32(byte[] data) {
		uint crc = uint.MaxValue;
		foreach (byte b in data)
			crc = (crc >> 8) ^ CRCTable[(crc ^ b) & 0xFF];
		return ~crc;
	}

	private static int GetCIC(byte[] bytes) {
		return CRC32(bytes.Get(64, 4032)) switch {
			0x6170A4A1 => 6101,
			0x90BB6CB5 => 6102,
			0x0B050EE0 => 6103,
			0x98BC2C86 => 6105,
			0xACC8580A => 6106,
			_ => 0,
		};
	}

	private static uint[] CalculateCRC(byte[] bytes) {
		uint[] crc = new uint[2];
		int cic = GetCIC(bytes);
		uint seed = cic switch {
			6101 or 6102 => CIC6102,
			6103 => CIC6103,
			6105 => CIC6105,
			6106 => CIC6106,
			_ => throw new Exception("CIC not valid!"),
		};
		uint t1, t2, t3, t4, t5, t6;
		t1 = t2 = t3 = t4 = t5 = t6 = seed;
		for (int i = 4096; i < 1052672; i += 4) {
			uint d = ByteArray.ToUInt(bytes.Get(i, 4));
			if ((t6 + d) < t6)
				t4++;

			t6 += d;
			t3 ^= d;
			uint r = ROL(d, (int)(d & 0x1F));
			t5 += r;
			t2 = (t2 <= d) ? (t2 ^ (t6 ^ d)) : (t2 ^ r);
			t1 = (cic != 6105) ? (t1 + (t5 ^ d)) :
				(t1 + (ByteArray.ToUInt(bytes.Get(1872 + (i & 0xFF), 4)) ^ d));
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