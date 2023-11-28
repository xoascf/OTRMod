/* Licensed under the Open Software License version 3.0 */

using static OTRMod.ID.Audio;

using OTRMod.Utility;

namespace OTRMod.Z;

public class AudioSequence : Resource {
	public byte Index { get; set; }
	public byte[] SequenceData { get; set; }
	public AudioSequenceInfo Info { get; set; }

	public AudioSequence(byte index, byte[] seq, AudioSequenceInfo info) : base(ResourceType.AudioSequence, 2) {
		Index = index;
		SequenceData = seq;
		Info = info;
	}

	public override byte[] Formatted() {
		List<byte> seq = new();
		seq.AddRange(ByteArray.FromI32(SequenceData.Length, false));
		seq.AddRange(SequenceData);
		seq.Add(Index);
		seq.Add(Info.Medium);
		seq.Add(Info.CachePolicy);
		seq.AddRange(ByteArray.FromI32(Info.FontIndices.Count, false));

		foreach (int fontIndex in Info.FontIndices)
			seq.Add((byte)fontIndex);

		Data = seq.ToArray();

		return base.Formatted();
	}
}