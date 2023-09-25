using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace SturmScharf;

public class MpqArchiveBuilder : IEnumerable<MpqFile> {
	private readonly List<MpqFile> _modifiedFiles;
	private readonly List<MpqFile> _originalFiles;
	private readonly ushort? _originalHashTableSize;
	private readonly List<ulong> _removedFiles;

	public MpqArchiveBuilder() {
		_originalHashTableSize = null;
		_originalFiles = new List<MpqFile>();
		_modifiedFiles = new List<MpqFile>();
		_removedFiles = new List<ulong>();
	}

	public MpqArchiveBuilder(MpqArchive originalMpqArchive) {
		if (originalMpqArchive is null)
			throw new ArgumentNullException(nameof(originalMpqArchive));

		_originalHashTableSize = (ushort)originalMpqArchive.HashTable.Size;
		_originalFiles = new List<MpqFile>(originalMpqArchive.GetMpqFiles());
		_modifiedFiles = new List<MpqFile>();
		_removedFiles = new List<ulong>();
	}

	/* FIXME: Not exposed for now 
	public IReadOnlyList<MpqFile> OriginalFiles => _originalFiles.AsReadOnly();

	public IReadOnlyList<MpqFile> ModifiedFiles => _modifiedFiles.AsReadOnly();

	public IReadOnlyList<ulong> RemovedFiles => _removedFiles.AsReadOnly();
	*/

	/// <inheritdoc />
	public IEnumerator<MpqFile> GetEnumerator() => GetMpqFiles().GetEnumerator();

	/// <inheritdoc />
	IEnumerator IEnumerable.GetEnumerator() => GetMpqFiles().GetEnumerator();

	public void AddFile(MpqFile file) => AddFile(file, MpqFileFlags.Exists | MpqFileFlags.CompressedMulti);

	public void AddFile(MpqFile file, MpqFileFlags targetFlags) {
		if (file is null)
			throw new ArgumentNullException(nameof(file));

		file.TargetFlags = targetFlags;
		_modifiedFiles.Add(file);
	}

	public void RemoveFile(ulong hashedFileName) => _removedFiles.Add(hashedFileName);

	public void RemoveFile(string fileName) => RemoveFile(fileName.GetStringHash());

	public void RemoveFile(MpqArchive mpqArchive, int blockIndex) {
		foreach (MpqHash mpqHash in mpqArchive.HashTable.Hashes)
			if (mpqHash.BlockIndex == blockIndex)
				RemoveFile(mpqHash.Name);
	}

	public void RemoveFile(MpqArchive mpqArchive, MpqEntry mpqEntry) {
		int blockIndex = mpqArchive.BlockTable.Entries.IndexOf(mpqEntry);
		if (blockIndex == -1)
			throw new ArgumentException("The given mpq entry could not be found in the archive.", nameof(mpqEntry));

		RemoveFile(mpqArchive, blockIndex);
	}

	public void SaveTo(string fileName) {
		using FileStream stream = FileProvider.CreateFileAndFolder(fileName);
		SaveTo(stream);
	}

	public void SaveTo(string fileName, MpqArchiveCreateOptions createOptions) {
		using FileStream stream = FileProvider.CreateFileAndFolder(fileName);
		SaveTo(stream, createOptions);
	}

	public void SaveTo(Stream stream, bool leaveOpen = false) {
		MpqArchiveCreateOptions createOptions = new() {
			HashTableSize = _originalHashTableSize,
			AttributesFlags = AttributesFlags.Crc32
		};

		SaveTo(stream, createOptions, leaveOpen);
	}

	public void SaveTo(Stream stream, MpqArchiveCreateOptions createOptions, bool leaveOpen = false) {
		if (createOptions == null)
			throw new ArgumentNullException(nameof(createOptions));

		if (!createOptions.ListFileCreateMode.HasValue)
			createOptions.ListFileCreateMode = _removedFiles.Contains(ListFile.FileName.GetStringHash())
			? MpqFileCreateMode.Prune
			: MpqFileCreateMode.Overwrite;

		if (!createOptions.AttributesCreateMode.HasValue)
			createOptions.AttributesCreateMode = _removedFiles.Contains(Attributes.FileName.GetStringHash())
			? MpqFileCreateMode.Prune
			: MpqFileCreateMode.Overwrite;

		if (!createOptions.HashTableSize.HasValue)
			createOptions.HashTableSize = _originalHashTableSize;

		List<MpqFile> mpqFiles = new();
		foreach (MpqFile mpqFile in _modifiedFiles)
			if (!_removedFiles.Contains(mpqFile.Name))
				mpqFiles.Add(mpqFile);

		foreach (MpqFile mpqFile in _originalFiles)
			if (!_removedFiles.Contains(mpqFile.Name))
				mpqFiles.Add(mpqFile);

		MpqArchive.Create(stream, mpqFiles.ToArray(), createOptions, leaveOpen).Dispose();
	}

	protected virtual IEnumerable<MpqFile> GetMpqFiles() {
		List<MpqFile> mpqFiles = new();
		foreach (MpqFile mpqFile in _modifiedFiles)
			if (!_removedFiles.Contains(mpqFile.Name))
				mpqFiles.Add(mpqFile);

		foreach (MpqFile mpqFile in _originalFiles)
			if (!_removedFiles.Contains(mpqFile.Name))
				mpqFiles.Add(mpqFile);

		return mpqFiles;
	}
}