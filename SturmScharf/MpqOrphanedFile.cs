﻿namespace SturmScharf;

public sealed class MpqOrphanedFile : MpqFile {
	public MpqOrphanedFile(MpqStream mpqStream, MpqFileFlags flags)
		: base(null, mpqStream, flags, MpqLocale.Neutral, false) {
	}

	internal override uint HashIndex => throw new NotSupportedException();

	internal override uint HashCollisions => throw new NotSupportedException();

	protected override uint? EncryptionSeed => null;

	protected override void GetTableEntries(MpqArchive mpqArchive, uint index, uint relativeFileOffset,
		uint compressedSize, uint fileSize, out MpqEntry mpqEntry, out MpqHash mpqHash) => throw new NotSupportedException();
}