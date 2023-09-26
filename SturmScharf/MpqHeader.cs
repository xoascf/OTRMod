using System.IO;
using System.Text;

namespace SturmScharf;

/// <summary>
/// The header of an <see cref="MpqArchive" />.
/// </summary>
public class MpqHeader {
	/// <summary>
	/// The expected signature of an MPQ file.
	/// </summary>
	public const uint MpqId = 0x1a51504d;

	/// <summary>
	/// The length (in bytes) of an <see cref="MpqHeader" />.
	/// </summary>
	public const uint Size = 32;

	private const uint ProtectedOffset = 0x6d9e4b86;

	/// <summary>
	/// Initializes a new instance of the <see cref="MpqHeader" /> class.
	/// </summary>
	/// <param name="fileArchiveSize">The length (in bytes) of the file archive.</param>
	/// <param name="hashTableEntries">The amount of <see cref="MpqHash" /> objects in the <see cref="HashTable" />.</param>
	/// <param name="blockTableEntries">The amount of <see cref="MpqEntry" /> objects in the <see cref="BlockTable" />.</param>
	/// <param name="blockSize">The blocksize that the corresponding <see cref="MpqArchive" /> has.</param>
	/// <param name="archiveBeforeTables">
	/// If true, the archive and table offsets are set so that the archive directly follows
	/// the header.
	/// </param>
	public MpqHeader(uint headerOffset, uint fileArchiveSize, uint hashTableEntries, uint blockTableEntries,
		ushort blockSize, bool archiveBeforeTables = true)
		: this() {
		uint hashTableSize = hashTableEntries * MpqHash.Size;
		uint blockTableSize = blockTableEntries * MpqEntry.Size;

		if (archiveBeforeTables) {
			DataOffset = Size - headerOffset;
			ArchiveSize = Size + fileArchiveSize + hashTableSize + blockTableSize;
			Version = MpqVersion.Original;
			BlockSize = blockSize;
			HashTableOffset = Size + fileArchiveSize - headerOffset;
			BlockTableOffset = Size + fileArchiveSize + hashTableSize - headerOffset;
			HashTableSize = hashTableEntries;
			BlockTableSize = blockTableEntries;
		}
		else {
			DataOffset = Size + hashTableSize + blockTableSize - headerOffset;
			ArchiveSize = Size + fileArchiveSize + hashTableSize + blockTableSize;
			Version = MpqVersion.Original;
			BlockSize = blockSize;
			HashTableOffset = Size - headerOffset;
			BlockTableOffset = Size + hashTableSize - headerOffset;
			HashTableSize = hashTableEntries;
			BlockTableSize = blockTableEntries;
		}
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="MpqHeader" /> class.
	/// </summary>
	private MpqHeader() {
		ID = MpqId;
	}

	/// <summary>
	/// Gets the signature of the MPQ file. Should be <see cref="MpqId" />.
	/// </summary>
	public uint ID { get; private set; }

	/// <summary>
	/// Gets the offset of the files in the archive, relative to the <see cref="MpqHeader" />.
	/// </summary>
	public uint DataOffset { get; private set; }

	/// <summary>
	/// Gets the size of the entire <see cref="MpqArchive" />. This includes the header, archive files, hashtable, and
	/// blocktable sizes.
	/// </summary>
	public uint ArchiveSize { get; internal set; }

	/// <summary>
	/// Gets the <see cref="MpqVersion" /> of the .mpq file.
	/// </summary>
	/// <remarks>
	/// Starting with World of Warcraft Burning Crusade, the version is 1.
	/// Currently, only versions 0 (read and write) and 1 (read only) are supported.
	/// </remarks>
	public MpqVersion Version { get; private set; }

	/// <summary>
	/// Gets the <see cref="MpqArchive" />'s block size.
	/// </summary>
	public ushort BlockSize { get; private set; }

	/// <summary>
	/// Gets the offset of the <see cref="HashTable" />, relative to the <see cref="MpqHeader" />.
	/// </summary>
	public uint HashTableOffset { get; internal set; }

	/// <summary>
	/// Gets the offset of the <see cref="BlockTable" />, relative to the <see cref="MpqHeader" />.
	/// </summary>
	public uint BlockTableOffset { get; internal set; }

	/// <summary>
	/// Gets the amount of <see cref="MpqHash" /> entries in the <see cref="HashTable" />.
	/// </summary>
	public uint HashTableSize { get; private set; }

	/// <summary>
	/// Gets the amount of <see cref="MpqEntry" /> entries in the <see cref="BlockTable" />.
	/// </summary>
	public uint BlockTableSize { get; private set; }

	/// <summary>
	/// Gets the offset of this <see cref="MpqHeader" />, relative to the start of the base stream.
	/// </summary>
	public uint HeaderOffset { get; internal set; }

	/// <summary>
	/// Gets the absolute offset of the files in the <see cref="MpqArchive" />'s base stream.
	/// </summary>
	public uint DataPosition => DataOffset == ProtectedOffset ? Size : DataOffset + HeaderOffset;

	/// <summary>
	/// Gets the absolute offset of the <see cref="HashTable" /> in the <see cref="MpqArchive" />'s base stream.
	/// </summary>
	public uint HashTablePosition => HashTableOffset + HeaderOffset;

	/// <summary>
	/// Gets the absolute offset of the <see cref="BlockTable" /> in the <see cref="MpqArchive" />'s base stream.
	/// </summary>
	public uint BlockTablePosition => BlockTableOffset + HeaderOffset;

	/// <summary>
	/// Reads from the given stream to create a new MPQ header.
	/// </summary>
	/// <param name="stream">The stream from which to read.</param>
	/// <param name="leaveOpen"><see langword="true" /> to leave the stream open; otherwise, <see langword="false" />.</param>
	/// <returns>The parsed <see cref="MpqHeader" />.</returns>
	public static MpqHeader Parse(Stream stream, bool leaveOpen = false) {
		using BinaryReader reader = new(stream, Encoding.UTF8, leaveOpen);
		return FromReader(reader);
	}

	/// <summary>
	/// Reads from the given reader to create a new MPQ header.
	/// </summary>
	/// <param name="reader">The reader from which to read.</param>
	/// <returns>The parsed <see cref="MpqHeader" />.</returns>
	public static MpqHeader FromReader(BinaryReader reader) {
		uint id = reader?.ReadUInt32() ?? throw new ArgumentNullException(nameof(reader));
		if (id != MpqId)
			throw new MpqParserException($"Invalid MPQ header signature: {id}");

		uint dataOffset = reader.ReadUInt32();
		uint archiveSize = reader.ReadUInt32();
		ushort mpqVersion = reader.ReadUInt16();
		if (!Enum.IsDefined(typeof(MpqVersion), mpqVersion))
			throw new MpqParserException($"Invalid MPQ format version: {mpqVersion}");

		MpqHeader header = new() {
			ID = id,
			DataOffset = dataOffset,
			ArchiveSize = archiveSize,
			Version = (MpqVersion)mpqVersion,
			BlockSize = reader.ReadUInt16(),
			HashTableOffset = reader.ReadUInt32(),
			BlockTableOffset = reader.ReadUInt32(),
			HashTableSize = reader.ReadUInt32(),
			BlockTableSize = reader.ReadUInt32()
		};

#if DEBUG
		if (header.Version <= MpqVersion.BurningCrusade)
		{
			uint expectedHashTableOffset = header.BlockTableOffset - (MpqHash.Size * header.HashTableSize);
			if (header.HashTableOffset != expectedHashTableOffset)
			{
				throw new MpqParserException(
					$"Invalid MPQ header field: {nameof(HashTableOffset)}. Expected: {expectedHashTableOffset}, Actual: {header.HashTableOffset}.");
			}

			uint expectedBlockTableOffset = header.HashTableOffset + (MpqHash.Size * header.HashTableSize);
			if (header.BlockTableOffset != expectedBlockTableOffset)
			{
				throw new MpqParserException(
					$"Invalid MPQ header field: {nameof(BlockTableOffset)}. Expected: {expectedBlockTableOffset}, Actual:  {header.BlockTableOffset}.");
			}
		}
#endif

		if (header.Version >= MpqVersion.CataclysmBeta)
			throw new NotSupportedException($"MPQ format version {header.Version} is not supported");

		return header;
	}

	/// <summary>
	/// Writes the header to the writer.
	/// </summary>
	/// <param name="writer">The writer to which the header will be written.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="writer" /> is null.</exception>
	public void WriteTo(BinaryWriter writer) {
		if (writer is null)
			throw new ArgumentNullException(nameof(writer));

		writer.Write(MpqId);
		writer.Write(DataOffset);
		writer.Write(ArchiveSize);
		writer.Write((ushort)Version);
		writer.Write(BlockSize);
		writer.Write(HashTableOffset);
		writer.Write(BlockTableOffset);
		writer.Write(HashTableSize);
		writer.Write(BlockTableSize);
	}

	internal bool IsArchiveAfterHeader() => DataOffset == Size || HashTableOffset != Size;
}