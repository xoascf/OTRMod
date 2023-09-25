/* Licensed under the Open Software License version 3.0 */

using OTRMod.Utility;

namespace OTRMod.Z;

public partial class Audio : Resource {
	public static byte[] ExportSft(int index, byte[] input) {
		int fntSize = input.Length;

		byte[] data = new byte[HeaderSize + fntSize];

		data.Set(0, GetHeader(ResourceType.AudioSoundFont, 2));
		data.Set(HeaderSize, (byte)index);
		data.Set(HeaderSize + 3, BitConverter.GetBytes(fntSize));
		data.Set(HeaderSize + 4, input);

		return data;
	}

	public static byte[] ExportSeq(int index, int font, int cachePolicy, byte[] input) {
		const int footerSize = 0x0C;
		int seqSize = input.Length;

		byte[] data = new byte[HeaderSize + seqSize + footerSize];

		data.Set(0, GetHeader(ResourceType.AudioSequence, 2));
		data.Set(HeaderSize, BitConverter.GetBytes(seqSize));
		data.Set(HeaderSize + 4, input);
		data.Set(HeaderSize + seqSize + 4, (byte)index);
		data.Set(HeaderSize + seqSize + 5, (byte)2); // Medium?
		data.Set(HeaderSize + seqSize + 6, (byte)cachePolicy);
		data.Set(HeaderSize + seqSize + 7, (byte)1); // FontIndexSize?
		data.Set(HeaderSize + seqSize + 11, (byte)font);

		return data;
	}
}