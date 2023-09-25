/* Licensed under the Open Software License version 3.0 */

using OTRMod.Utility;

namespace OTRMod.Z;

public class Texture : Resource {
	public static byte[] Export(ID.Texture.Codec codec, int width, int height, byte[] input) {
		int texSize = input.Length;

		byte[] data = new byte[HeaderSize + 16 + texSize];

		data.Set(0, GetHeader(ResourceType.Texture));
		data.Set(HeaderSize, (byte)(int)codec);
		data.Set(HeaderSize + 4, (byte)width);
		data.Set(HeaderSize + 8, (byte)height);
		data.Set(HeaderSize + 12, BitConverter.GetBytes(texSize));
		data.Set(HeaderSize + 16, input);

		return data;
	}
}