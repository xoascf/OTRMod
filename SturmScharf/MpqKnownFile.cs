using SturmScharf.Extensions;

namespace SturmScharf;

public sealed class MpqKnownFile : MpqFile {
	/// <summary>
	/// Initializes a new instance of the <see cref="MpqKnownFile" /> class.
	/// </summary>
	internal MpqKnownFile(string fileName, MpqStream mpqStream, MpqFileFlags flags, MpqLocale locale,
		bool leaveOpen = false)
		: base(fileName.GetStringHash(), mpqStream, flags, locale, leaveOpen) {
		FileName = fileName;
	}

	public string FileName { get; }

	internal override uint HashIndex => MpqHash.GetIndex(FileName);

	internal override uint HashCollisions => 0;

	protected override uint? EncryptionSeed => MpqEntry.CalculateEncryptionSeed(FileName, out uint encryptionSeed)
		? encryptionSeed
		: null;

	public override string ToString() {
		return FileName;
	}

	protected override void GetTableEntries(MpqArchive mpqArchive, uint index, uint relativeFileOffset,
		uint compressedSize, uint fileSize, out MpqEntry mpqEntry, out MpqHash mpqHash) {
		mpqEntry = new MpqEntry(FileName, mpqArchive.HeaderOffset, relativeFileOffset, compressedSize, fileSize,
			TargetFlags);
		mpqHash = new MpqHash(FileName, mpqArchive.HashTable.Mask, Locale, index);
	}
}