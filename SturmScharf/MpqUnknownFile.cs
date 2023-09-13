namespace SturmScharf;

public class MpqUnknownFile : MpqFile {
	/// <summary>
	/// Initializes a new instance of the <see cref="MpqUnknownFile" /> class.
	/// </summary>
	internal MpqUnknownFile(MpqStream mpqStream, MpqFileFlags flags, MpqHash mpqHash, uint hashIndex,
		uint hashCollisions, uint? encryptionSeed = null)
		: base(mpqHash.Name, mpqStream, flags, mpqHash.Locale, false) {
		if (mpqHash.Mask == 0) {
			throw new ArgumentException(
				"Expected the Mask value of mpqHash argument to be set to a non-zero value.", nameof(mpqHash));
		}

		if (flags.HasFlag(MpqFileFlags.Encrypted) && encryptionSeed is null) {
			throw new ArgumentException($"Cannot encrypt an {nameof(MpqUnknownFile)} without an encryption seed.",
				nameof(flags));
		}

		Mask = mpqHash.Mask;
		HashIndex = hashIndex;
		HashCollisions = hashCollisions;
		EncryptionSeed = encryptionSeed;
	}

	public uint Mask { get; }

	internal override uint HashIndex { get; }

	internal override uint HashCollisions { get; }

	protected override uint? EncryptionSeed { get; }

	public MpqKnownFile TryAsKnownFile(string fileName) {
		throw new NotImplementedException();
	}

	protected override void GetTableEntries(MpqArchive mpqArchive, uint index, uint relativeFileOffset,
		uint compressedSize, uint fileSize, out MpqEntry mpqEntry, out MpqHash mpqHash) {
		mpqEntry = new MpqEntry(null, mpqArchive.HeaderOffset, relativeFileOffset, compressedSize, fileSize,
			TargetFlags);
		mpqHash = new MpqHash(Name, Locale, index, Mask);
	}
}