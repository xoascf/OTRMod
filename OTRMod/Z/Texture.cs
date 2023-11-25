/* Licensed under the Open Software License version 3.0 */

using static OTRMod.ID.Texture;

using OTRMod.Utility;

namespace OTRMod.Z;

public class Texture : Resource {
	public Codec Codec { get; set; }
	public int Width { get; set; }
	public int Height { get; set; }
	public byte[] TextureData { get; set; }
	private int Flags = 0;
	private float HByteScale = 1;
	private float VPixelScale = 1;

	public Texture(Codec codec, int width, int height, byte[] texData)
		: base(ResourceType.Texture) {
		Codec = codec;
		Width = width;
		Height = height;
		TextureData = texData;
	}

	public override byte[] Formatted() {
		List<byte> tex = new();
		tex.AddRange(ByteArray.FromI32((int)Codec, Big));
		tex.AddRange(ByteArray.FromI32(Width, Big));
		tex.AddRange(ByteArray.FromI32(Height, Big));

		if (Version >= 1) { // Roy allows tex flags and scale
			tex.AddRange(ByteArray.FromI32(Flags, Big));
			tex.AddRange(ByteArray.FromF32(HByteScale, Big));
			tex.AddRange(ByteArray.FromF32(VPixelScale, Big));
		}

		tex.AddRange(ByteArray.FromI32(TextureData.Length, Big));
		tex.AddRange(TextureData);

		Data = tex.ToArray();

		return base.Formatted();
	}

	public static Texture LoadFrom(Resource res) {
		Codec codec = (Codec)res.Data.ToI32(0x00, res.Big);
		int width = res.Data.ToI32(0x04, res.Big);
		int height = res.Data.ToI32(0x08, res.Big);
		int size;
		byte[] data;
		if (res.Version >= 1) { // Roy allows tex flags and scale
			size = res.Data.ToI32(0x1C - 0x04, res.Big);
			data = res.Data.Get(0x1C, size);
		}
		else {
			size = res.Data.ToI32(0x10 - 0x04, res.Big);
			data = res.Data.Get(0x10, size);
		}

		return new(codec, width, height, data) {
			IsModded = res.IsModded,
			Version = res.Version,
		};
	}
}