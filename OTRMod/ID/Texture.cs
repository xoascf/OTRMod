namespace OTRMod.ID;

/* Sources:
 * Retro - texture.dart
 * ZAPD - ZTexture.h
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

		/* Background */
		JPEG32,
	}

	private static double GetMultiplier(Codec codec)
		=> codec switch {
			Codec.RGBA32 => 4,
			Codec.RGBA16 or Codec.IA16 => 2,
			Codec.CI8 or Codec.I8 or Codec.IA8 => 1,
			Codec.CI4 or Codec.I4 or Codec.IA4 => 0.5,
			_ => 0,
		};

	public static int GetSize(Codec codec, int wxh)
		=> (int)(wxh * GetMultiplier(codec));
}