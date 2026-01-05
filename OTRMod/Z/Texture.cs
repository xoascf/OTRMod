/* Licensed under the Open Software License version 3.0 */

using static OTRMod.ID.Texture;

using OTRMod.Utility;

namespace OTRMod.Z;

public class Texture : Resource {
	public Codec Codec { get; set; }
	public int Width { get; set; }
	public int Height { get; set; }
	public byte[] TextureData { get; set; }
	private int Flags;
	private float HByteScale;
	private float VPixelScale;

	public Texture(Codec codec, int width, int height, byte[] texData)
		: base(ResourceType.Texture) {
		Codec = codec;
		Width = width;
		Height = height;
		TextureData = texData;
	}

	public Texture(Codec codec, int width, int height, byte[] texData, int flags = 0, float hbs = 1.0f, float vps = 1.0f)
		: base(ResourceType.Texture, 1) {
		Codec = codec;
		Width = width;
		Height = height;

		TextureData = texData;

		Flags = flags;
		HByteScale = hbs;
		VPixelScale = vps;
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

	private Bitmap GetBitmap(Codec codec) {
		Bitmap bmp = new(Width, Height);

		Iterate2D(Width, Height, (w, h) => {
			int pixelOffset = h * Width + w;
			int select = codec switch {
				Codec.IA4 or Codec.I4 or Codec.CI4 => pixelOffset & 0x1,
				_ => 0,
			};
			pixelOffset = GetOffset(codec, pixelOffset);

			bmp.SetPixel(w, h, Colorize(TextureData, pixelOffset, select, codec));
		});

		return bmp;
	}

	public Bitmap GetBitmap() =>
		TextureData.Length ==
			GetOffset(Codec.RGBA32, Width * Height) ?
				GetBitmap(Codec.RGBA32) : GetBitmap(Codec);

	public static Texture LoadFrom(Resource res) {
		if (res.Data == null)
			throw new ArgumentException("Resource data cannot be null.", nameof(res));

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