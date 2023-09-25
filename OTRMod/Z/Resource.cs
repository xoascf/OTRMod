/* Licensed under the Open Software License version 3.0 */

using OTRMod.Utility;

namespace OTRMod.Z;

public class Resource {
	protected static int HeaderSize = 0x40;
	private const ulong MagicValue = 0xDEAD_BEEF_DEAD_BEEF;
	private static readonly byte[] BeefData = ByteArray.FromU64(MagicValue, big: false);

	internal static byte[] GetHeader(ResourceType type, int version = 0) {
		byte[] header = new byte[HeaderSize];
		header.Set(0x04, ByteArray.FromI32((int)type, big: false));
		header.Set(0x08, ByteArray.FromI32(version, big: false));
		header.Set(0x0C, BeefData);

		return header;
	}
}