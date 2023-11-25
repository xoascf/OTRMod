/* Licensed under the Open Software License version 3.0 */

using OTRMod.Utility;

namespace OTRMod.Z;

public class AudioSequence : Resource {
	public byte Index { get; set; }
	public byte[] SequenceData { get; set; }
	public SequenceAudioEntry Entry { get; set; }

	public AudioSequence(byte index, byte[] seq, SequenceAudioEntry entry) : base(ResourceType.AudioSequence) {
		Index = index;
		SequenceData = seq;
		Entry = entry;
	}

	public class SequenceAudioEntry {
		public byte Medium;
		public byte CachePolicy;
		public List<int> FontIndices = new();
	}

	public override byte[] Formatted() {
		List<byte> seq = new();
		seq.AddRange(ByteArray.FromI32(SequenceData.Length, false));
		seq.AddRange(seq);
		seq.Add(Index);
		seq.Add(Entry.Medium);
		seq.Add(Entry.CachePolicy);
		seq.AddRange(ByteArray.FromI32(Entry.FontIndices.Count, false));

		foreach (int fontIndex in Entry.FontIndices)
			seq.Add((byte)fontIndex);

		Data = seq.ToArray();

		return base.Formatted();
	}
}