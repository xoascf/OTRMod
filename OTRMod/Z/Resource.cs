/* Licensed under the Open Software License version 3.0 */

using OTRMod.Utility;

namespace OTRMod.Z;

public class Resource {
	private static readonly int HeaderSize = 0x40;
	public ResourceType Type { get; set; }
	public int Version { get; set; }
	public ulong MagicValue { get; set; }
	public bool Big { get; set; } /* Endianness */
	public byte[]? Data { get; set; }
	public bool IsModded { get; set; }

	public Resource(ResourceType type = ResourceType.Unknown, int version = 0) {
		Type = type;
		Version = version;
		MagicValue = 0xDEAD_BEEF_DEAD_BEEF;
		Big = false;
	}

	public virtual byte[] Formatted() {
		if (Data == null || Type == ResourceType.Unknown)
			throw new Exception("No valid exportable resource type.");

		byte[] format = new byte[HeaderSize + Data.Length];
		format.Set(0x04, ByteArray.FromI32((int)Type, Big));
		format.Set(0x08, ByteArray.FromI32(Version, Big));
		format.Set(0x0C, ByteArray.FromU64(MagicValue, Big));
		// FIXME: 0x14 is I32 for Retro's "resource version"
		format.Set(0x18, (byte)(IsModded ? 1 : 0));

		format.Set(HeaderSize, Data);

		return format;
	}

	public static Resource? Analyze(byte[] data)
		=> data == null || data.Length < HeaderSize ? null : new() {
		Type = (ResourceType)data.ToI32(0x04, false),
		Version = data.ToI32(0x08, false),
		IsModded = data[0x18] == 1
	};

	public static void SetData(ref Resource resource, byte[] data)
		=> resource.Data = data.Get(HeaderSize, data.Length - HeaderSize);

	public static Resource Read(byte[] data) => new() {
		Type = (ResourceType)data.ToI32(0x04, false),
		Version = data.ToI32(0x08, false),
		IsModded = data[0x18] == 1,
		Data = data.Get(HeaderSize, data.Length - HeaderSize)
	};
}