using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Security.Cryptography;
using System.Text;

using Ionic.Crc;
using SturmScharf.Extensions;

namespace SturmScharf;

/// <summary>
/// Represents a MoPaQ file, that is used to archive files.
/// </summary>
public sealed class MpqArchive : IDisposable, IEnumerable<MpqEntry> {
	private const int PreArchiveAlignBytes = 0x200;
	private const int BlockSizeModifier = 0x200;
	private readonly bool _archiveFollowsHeader;

	private readonly long _headerOffset;

	private readonly bool _isStreamOwner;

	/// <summary>
	/// Initializes a new instance of the <see cref="MpqArchive" /> class.
	/// </summary>
	/// <param name="sourceStream">The <see cref="Stream" /> from which to load an <see cref="MpqArchive" />.</param>
	/// <param name="loadListFile">
	/// If <see langword="true" />, automatically add filenames from the <see cref="MpqArchive" />'s
	/// <see cref="ListFile" /> after the archive is initialized.
	/// </param>
	/// <exception cref="ArgumentNullException">Thrown when the <paramref name="sourceStream" /> is <see langword="null" />.</exception>
	/// <exception cref="MpqParserException">
	/// Thrown when the <see cref="MpqHeader" /> could not be found, or when the MPQ
	/// format version is not 0.
	/// </exception>
	public MpqArchive(Stream sourceStream, bool loadListFile = false) {
		_isStreamOwner = true;
		BaseStream = sourceStream ?? throw new ArgumentNullException(nameof(sourceStream));

		if (!TryLocateMpqHeader(BaseStream, out MpqHeader? mpqHeader, out _headerOffset))
			throw new MpqParserException("Unable to locate MPQ header.");

		Header = mpqHeader;
		BlockSize = BlockSizeModifier << Header.BlockSize;
		_archiveFollowsHeader = Header.IsArchiveAfterHeader();

		using (BinaryReader reader = new(BaseStream, Encoding.UTF8, true)) {
			BaseStream.Seek(Header.HashTablePosition, SeekOrigin.Begin);
			HashTable = new HashTable(reader, Header.HashTableSize);

			BaseStream.Seek(Header.BlockTablePosition, SeekOrigin.Begin);
			BlockTable = new BlockTable(reader, Header.BlockTableSize, (uint)_headerOffset);
		}

		AddFileName(ListFile.FileName);
		AddFileName(Attributes.FileName);

		if (loadListFile)
			if (TryOpenFile(ListFile.FileName, out MpqStream? listFileStream)) {
				/* Read the file list as Latin-1 */
				using StreamReader listFileReader = new(listFileStream, EncodingProvider.Latin1);
				AddFileNames(listFileReader.ReadListFile().FileNames);
			}
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="MpqArchive" /> class.
	/// </summary>
	/// <param name="sourceStream">The <see cref="Stream" /> containing pre-archive data. Can be <see langword="null" />.</param>
	/// <param name="inputFiles">The <see cref="MpqFile" />s that should be added to the archive.</param>
	/// <param name="createOptions"></param>
	/// <param name="leaveOpen">
	/// If <see langword="false" />, the given <paramref name="sourceStream" /> will be disposed when
	/// the <see cref="MpqArchive" /> is disposed.
	/// </param>
	/// <exception cref="ArgumentNullException">
	/// Thrown when the <paramref name="mpqFiles" /> collection is
	/// <see langword="null" />.
	/// </exception>
	public MpqArchive(Stream? sourceStream, IEnumerable<MpqFile> inputFiles, MpqArchiveCreateOptions createOptions,
		bool leaveOpen = false) {
		if (inputFiles is null)
			throw new ArgumentNullException(nameof(inputFiles));

		if (createOptions is null)
			throw new ArgumentNullException(nameof(createOptions));

		_isStreamOwner = !leaveOpen;
		BaseStream = AlignStream(sourceStream);

		_headerOffset = BaseStream.Position;
		BlockSize = BlockSizeModifier << createOptions.BlockSize;
		_archiveFollowsHeader = createOptions.WriteArchiveFirst;

		ulong listFileName = ListFile.FileName.GetStringHash();
		ulong attributesName = Attributes.FileName.GetStringHash();

		MpqFileCreateMode listFileCreateMode =
			createOptions.ListFileCreateMode.GetValueOrDefault(MpqFileCreateMode.Overwrite);
		MpqFileCreateMode attributesCreateMode =
			createOptions.AttributesCreateMode.GetValueOrDefault(MpqFileCreateMode.Overwrite);
		bool haveListFile = false;
		bool haveAttributes = false;
		HashSet<MpqFile> mpqFiles = new(MpqFileComparer.Default);
		foreach (MpqFile mpqFile in inputFiles) {
			if (mpqFile is MpqOrphanedFile)
				continue;

			if (mpqFile.Name == listFileName) {
				if (listFileCreateMode.HasFlag(MpqFileCreateMode.RemoveFlag))
					continue;

				haveListFile = true;
			}
			if (mpqFile.Name == attributesName) {
				if (attributesCreateMode.HasFlag(MpqFileCreateMode.RemoveFlag))
					continue;

				haveAttributes = true;
			}

			if (!mpqFiles.Add(mpqFile)) {
			}
		}

		uint fileCount = (uint)mpqFiles.Count;

		bool wantGenerateListFile = !haveListFile && listFileCreateMode.HasFlag(MpqFileCreateMode.AddFlag);
		ListFile? listFile = wantGenerateListFile ? new ListFile() : null;
		if (wantGenerateListFile)
			fileCount++;

		bool wantGenerateAttributes = !haveAttributes && attributesCreateMode.HasFlag(MpqFileCreateMode.AddFlag);
		Attributes? attributes = wantGenerateAttributes ? new Attributes(createOptions) : null;
		if (wantGenerateAttributes)
			fileCount++;

		HashTable = new HashTable(Math.Max(fileCount,
			createOptions.HashTableSize ?? Math.Min(fileCount * 8, MpqTable.MaxSize)));
		BlockTable = new BlockTable();

		using BinaryWriter writer = new(BaseStream, EncodingProvider.StrictUTF8, true);
		writer.Seek((int)MpqHeader.Size, SeekOrigin.Current);

		uint fileIndex = 0U;
		uint fileOffset = _archiveFollowsHeader ? MpqHeader.Size : throw new NotImplementedException();

		long endOfStream = BaseStream.Position;

		bool includeCrc32 = false;
		bool includeFileTime = false;
		bool includeMd5 = false;

		if (attributes is not null) {
			includeCrc32 = attributes.Flags.HasFlag(AttributesFlags.Crc32);
			includeFileTime = attributes.Flags.HasFlag(AttributesFlags.FileTime);
			includeMd5 = attributes.Flags.HasFlag(AttributesFlags.Md5);
		}

		void InsertMpqFile(MpqFile mpqFile, bool updateEndOfStream, bool allowMultiple = true) {
			if (listFile is not null && mpqFile is MpqKnownFile knownFile)
				listFile.FileNames.Add(knownFile.FileName);

			mpqFile.AddToArchive(this, fileIndex, out MpqEntry mpqEntry, out MpqHash mpqHash);
			uint hashTableEntries = HashTable.Add(mpqHash, mpqFile.HashIndex, mpqFile.HashCollisions);
			if (!allowMultiple && hashTableEntries > 1)
				throw new Exception();

			byte[] md5 = new byte[16];
			int crc32 = 0;

			if ((includeCrc32 || includeMd5) && allowMultiple && mpqFile.MpqStream.CanSeek && mpqFile.MpqStream.CanRead) {
				mpqFile.MpqStream.Position = 0;
				if (includeCrc32) crc32 = new CRC32().GetCrc32(mpqFile.MpqStream);
				if (includeMd5) using (MD5 m = MD5.Create()) md5 = m.ComputeHash(mpqFile.MpqStream);
			}

			for (int i = 0; i < hashTableEntries; i++) {
				BlockTable.Add(mpqEntry);
				if (attributes is not null) {
					if (includeCrc32)
						attributes.Crc32s.Add(crc32);

					if (includeFileTime)
						attributes.FileTimes.Add(DateTime.Now.ToFileTime());

					if (includeMd5)
						attributes.Md5s.Add(md5);
				}
			}

			mpqFile.Dispose();

			fileIndex += hashTableEntries;
			if (updateEndOfStream)
				endOfStream = BaseStream.Position;
		}

		List<MpqFile> mpqFixedPositionFiles = new();
		foreach (MpqFile mpqFile in mpqFiles)
			if (mpqFile.IsFilePositionFixed)
				mpqFixedPositionFiles.Add(mpqFile);

		mpqFixedPositionFiles.Sort((mpqFile1, mpqFile2) => mpqFile1.MpqStream.FilePosition.CompareTo(mpqFile2.MpqStream.FilePosition));
		if (mpqFixedPositionFiles.Count > 0) {
			if (mpqFixedPositionFiles[0].MpqStream.FilePosition < 0)
				throw new NotSupportedException("Cannot place files in front of the header.");

			foreach (MpqFile mpqFixedPositionFile in mpqFixedPositionFiles) {
				uint position = mpqFixedPositionFile.MpqStream.FilePosition;
				if (position < endOfStream)
					throw new ArgumentException(
					"Fixed position files overlap with each other and/or the header. Archive cannot be created.",
					nameof(inputFiles));

				if (position > endOfStream) {
					long gapSize = position - endOfStream;
					writer.Seek((int)gapSize, SeekOrigin.Current);
				}

				InsertMpqFile(mpqFixedPositionFile, true);
			}
		}

		foreach (MpqFile mpqFile in mpqFiles)
			if (!mpqFile.IsFilePositionFixed) {
				long selectedPosition = endOfStream;
				bool selectedGap = false;
				BaseStream.Position = selectedPosition;

				InsertMpqFile(mpqFile, !selectedGap);
			}

		if (listFile is not null) {
			BaseStream.Position = endOfStream;

			using MemoryStream listFileStream = new();
			using StreamWriter listFileWriter = new(listFileStream, EncodingProvider.Latin1); /* Write the file list as Latin-1 */
			listFileWriter.NewLine = "\r\n"; /* Use CRLF! */
			listFileWriter.WriteListFile(listFile);
			listFileWriter.Flush();

			using MpqFile listFileMpqFile = MpqFile.New(listFileStream, ListFile.FileName);
			listFileMpqFile.TargetFlags = MpqFileFlags.Exists | MpqFileFlags.CompressedMulti |
										  MpqFileFlags.Encrypted | MpqFileFlags.BlockOffsetAdjustedKey;
			InsertMpqFile(listFileMpqFile, true);
		}

		if (attributes is not null) {
			BaseStream.Position = endOfStream;

			if (includeCrc32)
				attributes.Crc32s.Add(0);

			if (includeFileTime)
				attributes.FileTimes.Add(DateTime.Now.ToFileTime());

			if (includeMd5)
				attributes.Md5s.Add(new byte[16]);

			using MemoryStream attributesStream = new();
			using BinaryWriter attributesWriter = new(attributesStream);
			attributesWriter.Write(attributes);
			attributesWriter.Flush();

			using MpqFile attributesMpqFile = MpqFile.New(attributesStream, Attributes.FileName);
			attributesMpqFile.TargetFlags = MpqFileFlags.Exists | MpqFileFlags.CompressedMulti |
											MpqFileFlags.Encrypted | MpqFileFlags.BlockOffsetAdjustedKey;
			InsertMpqFile(attributesMpqFile, true, false);
		}

		BaseStream.Position = endOfStream;
		HashTable.WriteTo(writer);
		BlockTable.WriteTo(writer);

		writer.Seek((int)_headerOffset, SeekOrigin.Begin);

		Header = new MpqHeader((uint)_headerOffset, (uint)(endOfStream - fileOffset), HashTable.Size,
			BlockTable.Size, createOptions.BlockSize, _archiveFollowsHeader);
		Header.WriteTo(writer);
	}

	internal Stream BaseStream { get; }

	internal uint HeaderOffset => (uint)_headerOffset;

	/// <summary>
	/// Gets the length (in bytes) of blocks in compressed files.
	/// </summary>
	internal int BlockSize { get; }

	internal MpqHeader Header { get; }

	internal HashTable HashTable { get; }

	internal BlockTable BlockTable { get; }

	internal MpqEntry this[int index] => BlockTable[index];

	/// <inheritdoc />
	public void Dispose() {
		if (_isStreamOwner)
			BaseStream?.Close();
	}

	/// <inheritdoc />
	IEnumerator IEnumerable.GetEnumerator() => (BlockTable as IEnumerable).GetEnumerator();

	/// <inheritdoc />
	IEnumerator<MpqEntry> IEnumerable<MpqEntry>.GetEnumerator() => (BlockTable as IEnumerable<MpqEntry>).GetEnumerator();

	/// <summary>
	/// Opens an existing <see cref="MpqArchive" /> for reading.
	/// </summary>
	/// <param name="path">The <see cref="MpqArchive" /> to open.</param>
	/// <param name="loadListFile">
	/// If <see langword="true" />, automatically add filenames from the <see cref="MpqArchive" />'s
	/// <see cref="ListFile" /> after the archive is initialized.
	/// </param>
	/// <returns>An <see cref="MpqArchive" /> opened from the specified <paramref name="path" />.</returns>
	/// <exception cref="IOException">
	/// Thrown when unable to create a <see cref="FileStream" /> from the given
	/// <paramref name="path" />.
	/// </exception>
	/// <exception cref="MpqParserException">
	/// Thrown when the <see cref="MpqHeader" /> could not be found, or when the MPQ
	/// format version is not 0.
	/// </exception>
	public static MpqArchive Open(string path, bool loadListFile = false) {
		FileStream fileStream;

		try {
			fileStream = File.OpenRead(path);
		}
		catch (Exception exception) {
			throw new IOException($"Failed to open the {nameof(MpqArchive)} at {path}", exception);
		}

		return Open(fileStream, loadListFile);
	}

	/// <summary>
	/// Opens an existing <see cref="MpqArchive" /> for reading.
	/// </summary>
	/// <param name="sourceStream">The <see cref="Stream" /> from which to load an <see cref="MpqArchive" />.</param>
	/// <param name="loadListFile">
	/// If <see langword="true" />, automatically add filenames from the <see cref="MpqArchive" />'s
	/// <see cref="ListFile" /> after the archive is initialized.
	/// </param>
	/// <returns>An <see cref="MpqArchive" /> opened from the specified <paramref name="sourceStream" />.</returns>
	/// <exception cref="ArgumentNullException">Thrown when the <paramref name="sourceStream" /> is <see langword="null" />.</exception>
	/// <exception cref="MpqParserException">
	/// Thrown when the <see cref="MpqHeader" /> could not be found, or when the MPQ
	/// format version is not 0.
	/// </exception>
	public static MpqArchive Open(Stream sourceStream, bool loadListFile = false)
		=> new(sourceStream, loadListFile);

	/// <summary>
	/// Creates a new <see cref="MpqArchive" />.
	/// </summary>
	/// <param name="path">The path and name of the <see cref="MpqArchive" /> to create.</param>
	/// <param name="mpqFiles">The <see cref="MpqFile" />s that should be added to the archive.</param>
	/// <param name="createOptions"></param>
	/// <returns>An <see cref="MpqArchive" /> created as a new file at the specified <paramref name="path" />.</returns>
	/// <exception cref="IOException">
	/// Thrown when unable to create a <see cref="FileStream" /> from the given
	/// <paramref name="path" />.
	/// </exception>
	public static MpqArchive Create(string path, IEnumerable<MpqFile> mpqFiles,
		MpqArchiveCreateOptions createOptions) {
		FileStream fileStream;

		try {
			fileStream = new FileStream(path, FileMode.CreateNew, FileAccess.ReadWrite);
		}
		catch (Exception exception) {
			throw new IOException($"Failed to create a {nameof(FileStream)} at {path}", exception);
		}

		return Create(fileStream, mpqFiles, createOptions);
	}

	/// <summary>
	/// Creates a new <see cref="MpqArchive" />.
	/// </summary>
	/// <param name="sourceStream">The <see cref="Stream" /> containing pre-archive data. Can be <see langword="null" />.</param>
	/// <param name="mpqFiles">The <see cref="MpqFile" />s that should be added to the archive.</param>
	/// <param name="createOptions"></param>
	/// <param name="leaveOpen">
	/// If <see langword="false" />, the given <paramref name="sourceStream" /> will be disposed when
	/// the <see cref="MpqArchive" /> is disposed.
	/// </param>
	/// <returns>An <see cref="MpqArchive" /> that is created.</returns>
	/// <exception cref="ArgumentNullException">
	/// Thrown when the <paramref name="mpqFiles" /> collection is
	/// <see langword="null" />.
	/// </exception>
	public static MpqArchive Create(Stream? sourceStream, IEnumerable<MpqFile> mpqFiles,
		MpqArchiveCreateOptions createOptions, bool leaveOpen = false)
		=> new(sourceStream, mpqFiles, createOptions, leaveOpen);

	/// <summary>
	/// Searches the <see cref="MpqArchive" /> for files with the given <paramref name="fileName" />, and sets the
	/// <see cref="MpqEntry.FileName" /> if it was not known before.
	/// </summary>
	/// <param name="fileName">The name for which to check if any corresponding files exist.</param>
	/// <returns>The amount of files in the <see cref="BlockTable" /> with the given <paramref name="fileName" />.</returns>
	public int AddFileName(string fileName) {
		if (fileName is null)
			throw new ArgumentNullException(nameof(fileName));

		IEnumerable<MpqHash> hashes = GetHashEntries(fileName);
		HashSet<uint> fileIndicesFound = new();
		foreach (MpqHash hash in hashes)
			if (fileIndicesFound.Add(hash.BlockIndex))
				BlockTable[hash.BlockIndex].FileName = fileName;

		return fileIndicesFound.Count;
	}

	/// <summary>
	/// Searches the <see cref="MpqArchive" /> for files with any of the given <paramref name="fileNames" />, and sets the
	/// <see cref="MpqEntry.FileName" /> if it was not known before.
	/// </summary>
	/// <param name="fileNames">The names for which to check if any corresponding files exist.</param>
	/// <returns>The amount of files in the <see cref="BlockTable" /> with any of the given <paramref name="fileNames" />.</returns>
	public int AddFileNames(params string[] fileNames) {
		int totalCount = 0;
		for (int i = 0; i < fileNames.Length; i++)
			totalCount += AddFileName(fileNames[i]);
		return totalCount;
	}

	/// <summary>
	/// Searches the <see cref="MpqArchive" /> for files with any of the given <paramref name="fileNames" />, and sets the
	/// <see cref="MpqEntry.FileName" /> if it was not known before.
	/// </summary>
	/// <param name="fileNames">The names for which to check if any corresponding files exist.</param>
	/// <returns>The amount of files in the <see cref="BlockTable" /> with any of the given <paramref name="fileNames" />.</returns>
	public int AddFileNames(IEnumerable<string> fileNames) {
		int totalCount = 0;
		foreach (string fileName in fileNames)
			totalCount += AddFileName(fileName);
		return totalCount;
	}

	/// <summary>
	/// Tries to find mpq entries corresponding to the given <paramref name="fileName" />.
	/// </summary>
	/// <param name="fileName">The name for which to check if a corresponding <see cref="MpqEntry" /> exists.</param>
	/// <returns>
	/// <see langword="true" /> if any file with the given <paramref name="fileName" /> exists,
	/// <see langword="false" /> otherwise.
	/// </returns>	
	public bool FileExists(string? fileName)
		=> !fileName.IsNullOrEmpty() && AddFileName(fileName) > 0;

	/// <summary>
	/// Opens the first matching <see cref="MpqEntry" />.
	/// </summary>
	/// <param name="fileName">The name of the <see cref="MpqEntry" /> to open.</param>
	/// <param name="locale">
	/// The locale of the <see cref="MpqEntry" /> to open.
	/// If <see langword="null" />, any file with the given <paramref name="fileName" /> can be opened.
	/// If <see cref="MpqLocale.Neutral" />, only files with the neutral locale can be opened.
	/// If not <see cref="MpqLocale.Neutral" />, only files with the specified <paramref name="locale" /> or
	/// <see cref="MpqLocale.Neutral" /> can be opened.
	/// </param>
	/// <param name="orderByBlockIndex">
	/// If <see langword="true" />, the file with the lowest position in the
	/// <see cref="BlockTable" /> is returned.
	/// </param>
	/// <returns>An <see cref="MpqStream" /> that provides access to the matched <see cref="MpqEntry" />.</returns>
	/// <exception cref="FileNotFoundException">Thrown when no matching <see cref="MpqEntry" /> exists.</exception>
	public MpqStream OpenFile(string fileName, MpqLocale? locale = null, bool orderByBlockIndex = true) => TryOpenFile(fileName, locale, orderByBlockIndex, out MpqStream? stream)
			? stream
			: throw new FileNotFoundException($"File not found: {fileName}");

	/// <summary>
	/// Opens the given <see cref="MpqEntry" />.
	/// </summary>
	/// <param name="entry">The <see cref="MpqEntry" /> to open.</param>
	/// <returns>An <see cref="MpqStream" /> that provides access to the given <paramref name="entry" />.</returns>
	/// <exception cref="ArgumentNullException">Thrown when the given <paramref name="entry" /> is <see langword="null" />.</exception>
	public MpqStream OpenFile(MpqEntry entry)
		=> new(this, entry ?? throw new ArgumentNullException(nameof(entry)));

	public bool TryOpenFile(string fileName, [NotNullWhen(true)] out MpqStream? stream) => TryOpenFile(fileName, null, true, out stream);

	public bool TryOpenFile(string fileName, MpqLocale? locale, [NotNullWhen(true)] out MpqStream? stream) => TryOpenFile(fileName, locale, true, out stream);

	public bool TryOpenFile(string fileName, bool orderByBlockIndex, [NotNullWhen(true)] out MpqStream? stream) => TryOpenFile(fileName, null, orderByBlockIndex, out stream);

	public bool TryOpenFile(string fileName, MpqLocale? locale, bool orderByBlockIndex,
		[NotNullWhen(true)] out MpqStream? stream) {
		MpqEntry? entry = null;

		foreach (MpqEntry mpqEntry in GetMpqEntries(fileName, locale, orderByBlockIndex)) {
			entry = mpqEntry;
			break;
		}

		if (entry is not null) {
			stream = new MpqStream(this, entry);
			return true;
		}

		stream = null;
		return false;
	}


	public IEnumerable<MpqEntry> GetMpqEntries(string fileName, MpqLocale? locale = null, bool orderByBlockIndex = true) {
		IEnumerable<MpqHash> hashes = GetHashEntries(fileName);

		List<MpqHash> orderedHashes = new();
		foreach (MpqHash hash in hashes)
			orderedHashes.Add(hash);

		if (orderByBlockIndex)
			orderedHashes.Sort((hash1, hash2) => hash1.BlockIndex.CompareTo(hash2.BlockIndex));

		foreach (MpqHash hash in orderedHashes) {
			MpqEntry entry = BlockTable[hash.BlockIndex];
			entry.FileName = fileName;

			if (!locale.HasValue || locale.Value == hash.Locale)
				yield return entry;
		}

		if (locale.HasValue && locale.Value != MpqLocale.Neutral)
			foreach (MpqHash hash in orderedHashes)
				if (hash.Locale == MpqLocale.Neutral)
					yield return BlockTable[hash.BlockIndex];
	}


	public IEnumerable<MpqFile> GetMpqFiles() {
		MpqFile[] files = new MpqFile[BlockTable.Size];
		HashSet<MpqEntry> addedEntries = new();

		for (int hashIndex = 0; hashIndex < HashTable.Size; hashIndex++) {
			MpqHash mpqHash = HashTable[hashIndex];
			if (!mpqHash.IsEmpty) {
				MpqEntry? mpqEntry = mpqHash.IsValidBlockIndex ? BlockTable[mpqHash.BlockIndex] : null;
				if (mpqEntry != null) {
					MpqStream stream = OpenFile(mpqEntry);
					MpqFile mpqFile = mpqEntry.FileName is null
						? MpqFile.New(stream, mpqHash, (uint)hashIndex, 0, mpqEntry.BaseEncryptionSeed)
						: MpqFile.New(stream, mpqEntry.FileName, mpqHash.Locale);

					mpqFile.TargetFlags = mpqEntry.Flags & ~MpqFileFlags.Garbage;

					files[mpqHash.BlockIndex] = mpqFile;
					addedEntries.Add(mpqEntry);
				}
			}
		}

		for (int i = 0; i < (int)BlockTable.Size; i++) {
			MpqEntry mpqEntry = BlockTable[i];
			if (!addedEntries.Contains(mpqEntry)) {
				MpqFile mpqFile = MpqFile.New(OpenFile(mpqEntry));
				mpqFile.TargetFlags = 0;
				files[i] = mpqFile;
			}
		}

		return files;
	}

	internal bool TryGetEntryFromHashTable(
		uint hashTableIndex,
		[NotNullWhen(true)] out MpqEntry? mpqEntry) {
		if (hashTableIndex >= HashTable.Size)
			throw new ArgumentOutOfRangeException(nameof(hashTableIndex));

		MpqHash mpqHash = HashTable[hashTableIndex];
		if (mpqHash.IsEmpty)
			throw new ArgumentException($"The {nameof(MpqHash)} at the given index is empty.",
			nameof(hashTableIndex));

		if (mpqHash.IsDeleted) {
			mpqEntry = null;
			return false;
		}

		mpqEntry = BlockTable[mpqHash.BlockIndex];
		return true;
	}

	private static bool TryLocateMpqHeader(
		Stream sourceStream,
		[NotNullWhen(true)] out MpqHeader? mpqHeader,
		out long headerOffset) {
		sourceStream.Seek(0, SeekOrigin.Begin);
		using (BinaryReader reader = new(sourceStream, Encoding.UTF8, true))
			for (headerOffset = 0;
			     headerOffset <= sourceStream.Length - MpqHeader.Size;
			     headerOffset += PreArchiveAlignBytes) {
				if (reader.ReadUInt32() == MpqHeader.MpqId) {
					sourceStream.Seek(-4, SeekOrigin.Current);
					mpqHeader = MpqHeader.FromReader(reader);
					mpqHeader.HeaderOffset = (uint)headerOffset;
					return true;
				}

				sourceStream.Seek(PreArchiveAlignBytes - 4, SeekOrigin.Current);
			}

		mpqHeader = null;
		headerOffset = -1;
		return false;
	}

	private static Stream AlignStream(Stream? stream, bool leaveOpen = false) {
		if (stream is null)
			return new MemoryStream();

		if (!stream.CanWrite) {
			MemoryStream memoryStream = new();

			stream.Seek(0, SeekOrigin.Begin);
			stream.CopyTo(memoryStream);
			if (!leaveOpen)
				stream.Dispose();

			return memoryStream;
		}

		stream.Seek(0, SeekOrigin.End);
		uint i = (uint)stream.Position & PreArchiveAlignBytes - 1;
		if (i > 0)
			for (; i < PreArchiveAlignBytes; i++)
				stream.WriteByte(0);

		return stream;
	}

	private IEnumerable<MpqHash> GetHashEntries(string fileName) {
		if (!StormBuffer.TryGetHashString(fileName, 0, out uint index))
			yield break;

		index &= Header.HashTableSize - 1;
		ulong name = fileName.GetStringHash();

		bool foundAnyHash = false;

		for (uint i = index; i < HashTable.Size; ++i) {
			MpqHash hash = HashTable[i];
			if (hash.Name == name) {
				yield return hash;
				foundAnyHash = true;
			}
			else if (hash.IsEmpty && foundAnyHash) {
				yield break;
			}
		}

		for (uint i = 0; i < index; ++i) {
			MpqHash hash = HashTable[i];
			if (hash.Name == name) {
				yield return hash;
				foundAnyHash = true;
			}
			else if (hash.IsEmpty && foundAnyHash) {
				yield break;
			}
		}
	}
}