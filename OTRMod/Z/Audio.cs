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

	public class SequenceAudioEntry {
		public byte medium;
		public byte cachePolicy;
		public List<int> fontIndices = new();
	}

	public static byte[] ExportSeq(byte index, byte[] seq, SequenceAudioEntry entry) {
		List<byte> bytes = new();
		bytes.AddRange(GetHeader(ResourceType.AudioSequence, 2));
		bytes.AddRange(ByteArray.FromI32(seq.Length, false));
		bytes.AddRange(seq);
		bytes.Add(index);
		bytes.Add(entry.medium);
		bytes.Add(entry.cachePolicy);
		bytes.AddRange(ByteArray.FromI32(entry.fontIndices.Count, false));

		foreach (int fontIndex in entry.fontIndices)
			bytes.Add((byte)fontIndex);

		return bytes.ToArray();
	}
}