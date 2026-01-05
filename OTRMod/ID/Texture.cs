namespace OTRMod.ID;

/* Sources:
 * Retro - texture.dart
 * ZAPD - ZTexture.h
 * Texture64 - N64Graphics.cs
 */

public static class Texture {
	public enum Codec {
		Error = 0,

		/* RGB + alpha */
		RGBA32,
		RGBA16,

		/* Palette */
		CI4,
		CI8,

		/* Grayscale */
		I4,
		I8,

		/* Grayscale + alpha */
		IA4,
		IA8,
		IA16,
	}

	public static int GetOffset(Codec codec, int i) => codec switch {
		Codec.RGBA32 => i * 4,
		Codec.RGBA16 or Codec.IA16 => i * 2,
		Codec.CI4 or Codec.I4 or Codec.IA4 => i / 2,
		Codec.CI8 or Codec.I8 or Codec.IA8 => i,
		Codec.Error => throw new NotImplementedException(),
		_ => throw new ArgumentOutOfRangeException
			(nameof(codec), $"Unknown texture type: {codec}"),
	};

	private static int SCALE_5_8(int val) => (val * 0xFF) / 0x1F;
	private static byte SCALE_8_5(byte val) => (byte)((((val) + 4) * 0x1F) / 0xFF);
	private static byte SCALE_8_4(byte val) => (byte)(val / 0x11);
	private static int SCALE_3_8(byte val) => (val * 0xFF) / 0x7;
	private static byte SCALE_8_3(byte val) => (byte)(val / 0x24);

	private static Color RGBA32Color(byte[] data, int pixelOffset) {
		int r = data[pixelOffset];
		int g = data[pixelOffset + 1];
		int b = data[pixelOffset + 2];
		int a = data[pixelOffset + 3];

		return Color.FromArgb(a, r, g, b);
	}

	private static Color RGBA16Color(byte c0, byte c1) {
		int r = SCALE_5_8((c0 & 0xF8) >> 3);
		int g = SCALE_5_8(((c0 & 0x07) << 2) | ((c1 & 0xC0) >> 6));
		int b = SCALE_5_8((c1 & 0x3E) >> 1);
		int a = ((c1 & 0x1) > 0) ? 255 : 0;

		return Color.FromArgb(a, r, g, b);
	}

	private static Color RGBA16Color(byte[] data, int pixelOffset) {
		byte c0 = data[pixelOffset];
		byte c1 = data[pixelOffset + 1];

		return RGBA16Color(c0, c1);
	}

	private static Color IA16Color(byte[] data, int pixelOffset) {
		int i = data[pixelOffset];
		int a = data[pixelOffset + 1];

		return Color.FromArgb(a, i, i, i);
	}

	private static Color IA8Color(byte[] data, int pixelOffset) {
		byte c = data[pixelOffset];
		int i = (c >> 4) * 0x11;
		int a = (c & 0xF) * 0x11;

		return Color.FromArgb(a, i, i, i);
	}

	private static Color IA4Color(byte[] data, int pixelOffset, int nibble) {
		int shift = (1 - nibble) * 4;
		int val = (data[pixelOffset] >> shift) & 0xF;
		int i = SCALE_3_8((byte)(val >> 1));
		int a = (val & 0x1) > 0 ? 0xFF : 0x00;

		return Color.FromArgb(a, i, i, i);
	}

	private static Color I8Color(byte[] data, int pixelOffset) {
		int i = data[pixelOffset];

		return Color.FromArgb(0xFF, i, i, i);
	}

	private static Color I4Color(byte[] data, int pixelOffset, int nibble) {
		int shift = (1 - nibble) * 4;
		int i = (data[pixelOffset] >> shift) & 0xF;
		i *= 0x11;

		return Color.FromArgb(0xFF, i, i, i);
	}

	public static Color Colorize(byte[] data, int offset, int select, Codec codec)
	=> codec switch {
		Codec.RGBA32 => RGBA32Color(data, offset),
		Codec.RGBA16 => RGBA16Color(data, offset),
		Codec.IA16 => IA16Color(data, offset),
		Codec.IA8 => IA8Color(data, offset),
		Codec.IA4 => IA4Color(data, offset, select),
		/* FIXME: Let's colorize without palettes for now! */
		Codec.I8 or Codec.CI8 => I8Color(data, offset),
		Codec.I4 or Codec.CI4 => I4Color(data, offset, select),
		_ => RGBA16Color(data, offset),
	};

	public static void Iterate2D(int width, int height, Action<int, int> action) {
		for (int y = 0; y < height; y++) for (int x = 0; x < width; x++) action(x, y);
	}

	public static byte[] Convert(this Bitmap bmp, Codec codec) {
		int pixels = bmp.Width * bmp.Height;
		byte[] imageData = new byte[GetOffset(codec, pixels)];
		switch (codec) {
			case Codec.RGBA32:
				Iterate2D(bmp.Width, bmp.Height, (x, y) => {
					Color c = bmp.GetPixel(x, y);
					int idx = 4 * (y * bmp.Width + x);
					imageData[idx + 0] = c.R;
					imageData[idx + 1] = c.G;
					imageData[idx + 2] = c.B;
					imageData[idx + 3] = c.A;
				});
				break;
			case Codec.RGBA16:
				Iterate2D(bmp.Width, bmp.Height, (x, y) => {
					Color c = bmp.GetPixel(x, y);
					byte r, g, b;
					r = SCALE_8_5(c.R);
					g = SCALE_8_5(c.G);
					b = SCALE_8_5(c.B);
					byte c0 = (byte)((r << 3) | (g >> 2));
					byte c1 = (byte)(((g & 0x3) << 6) | (b << 1) | ((c.A > 0) ? 1 : 0));
					int idx = 2 * (y * bmp.Width + x);
					imageData[idx + 0] = c0;
					imageData[idx + 1] = c1;
				});
				break;
			case Codec.IA16:
				Iterate2D(bmp.Width, bmp.Height, (x, y) => {
					Color c = bmp.GetPixel(x, y);
					int sum = c.R + c.G + c.B;
					byte intensity = (byte)(sum / 3);
					byte alpha = c.A;
					int idx = 2 * (y * bmp.Width + x);
					imageData[idx + 0] = intensity;
					imageData[idx + 1] = alpha;
				});
				break;
			case Codec.IA8:
				Iterate2D(bmp.Width, bmp.Height, (x, y) => {
					Color c = bmp.GetPixel(x, y);
					int sum = c.R + c.G + c.B;
					byte intensity = SCALE_8_4((byte)(sum / 3));
					byte alpha = SCALE_8_4(c.A);
					int idx = y * bmp.Width + x;
					imageData[idx] = (byte)((intensity << 4) | alpha);
				});
				break;
			case Codec.IA4:
				Iterate2D(bmp.Width, bmp.Height, (x, y) => {
					Color c = bmp.GetPixel(x, y);
					int sum = c.R + c.G + c.B;
					byte intensity = SCALE_8_3((byte)(sum / 3));
					byte alpha = (byte)(c.A > 0 ? 1 : 0);
					int idx = y * bmp.Width + x;
					byte old = imageData[idx / 2];
					imageData[idx / 2] = (idx % 2) > 0 ?
						(byte)((old & 0xF0) | (intensity << 1) | alpha) :
						(byte)((old & 0x0F) | (((intensity << 1) | alpha) << 4));
				});
				break;
			/* FIXME: Let's convert without palettes for now! */
			case Codec.I8 or Codec.CI8:
				Iterate2D(bmp.Width, bmp.Height, (x, y) => {
					Color c = bmp.GetPixel(x, y);
					int sum = c.R + c.G + c.B;
					byte intensity = (byte)(sum / 3);
					int idx = y * bmp.Width + x;
					imageData[idx] = intensity;
				});
				break;
			case Codec.I4 or Codec.CI4:
				Iterate2D(bmp.Width, bmp.Height, (x, y) => {
					Color c = bmp.GetPixel(x, y);
					int sum = c.R + c.G + c.B;
					byte intensity = SCALE_8_4((byte)(sum / 3));
					int idx = y * bmp.Width + x;
					byte old = imageData[idx / 2];
					imageData[idx / 2] = (idx % 2) > 0 ?
						(byte)((old & 0xF0) | intensity) :
						(byte)((old & 0x0F) | (intensity << 4));
				});
				break;
		}

		return imageData;
	}
}