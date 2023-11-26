namespace OTRMod.ID;

/* Sources:
 * ZAPD - ZAudio.h
 */

public class Audio {
	public class AdsrEnvelope {
		public short Delay;
		public short Arg;
	}

	public class AdpcmBook {
		public int Order;
		public int NPredictors;
		public List<short> Books = new() { };
	}

	public class AdpcmLoop {
		public int Start;
		public int End;
		public int Count;
		public List<short> States = new() { };
	}

	public class SampleEntry {
		public string FileName = "";
		public byte BankID;
		public int SampleDataOffset;
		public int SampleLoopOffset = -1;
		public byte Codec;
		public byte Medium;
		public byte Bit1; // unk_bit26
		public byte IsPatched; // unk_bit25/isRelocated
		public List<byte> Data = new();
		public AdpcmLoop Loop = new();
		public AdpcmBook Book = new();
	}

	public class SoundFontEntry {
		public SampleEntry? SampleEntry = null;
		public float Tuning;
	}

	public class DrumEntry {
		public byte ReleaseRate;
		public byte Pan;
		public byte Loaded; // bool?
		public int Offset;
		public float Tuning;
		public List<AdsrEnvelope> Envelopes = new();
		public SampleEntry? Sample = null;
	}

	public class InstrumentEntry {
		public bool IsValidInstrument;
		public byte Loaded;
		public byte NormalRangeLo;
		public byte NormalRangeHi;
		public byte ReleaseRate;
		public List<AdsrEnvelope> Envelopes = new();
		public SoundFontEntry? LowNotesSound = null;
		public SoundFontEntry? NormalNotesSound = null;
		public SoundFontEntry? HighNotesSound = null;
	}

	public class AudioTableEntry {
		public int Address; // ptr
		public int Size;
		public byte Medium;
		public byte CachePolicy;
		public short Data1;
		public short Data2;
		public short Data3;
		public List<DrumEntry> Drums = new();
		public List<SoundFontEntry> SoundEffects = new();
		public List<InstrumentEntry> Instruments = new();
	}

	public class AudioSequenceInfo {
		public byte Medium;
		public byte CachePolicy;
		public List<int> FontIndices = new();
	}

	// First Key = Bank ID, Sec Key = LoopDataOffset, Third Key = Sample Data Offset
	public class SampleOffsets : SortedDictionary<int, SortedDictionary<int, SortedDictionary<int, string>>> { }
}