using SturmScharf.Extensions;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace SturmScharf;

/// <summary>
/// An entry in a <see cref="BlockTable" />, which corresponds to a single file in the <see cref="MpqArchive" />.
/// </summary>
public class MpqEntry {
	/// <summary>
	/// The length (in bytes) of an <see cref="MpqEntry" />.
	/// </summary>
	public const uint Size = 16;

	private string? _fileName;

	/// <summary>
	/// Initializes a new instance of the <see cref="MpqEntry" /> class.
	/// </summary>
	/// <param name="fileName"></param>
	/// <param name="headerOffset">The containing <see cref="MpqArchive" />'s header offset.</param>
	/// <param name="fileOffset">The file's position in the archive, relative to the header offset.</param>
	/// <param name="compressedSize">The compressed size of the file.</param>
	/// <param name="fileSize">The uncompressed size of the file.</param>
	/// <param name="flags">The file's <see cref="MpqFileFlags" />.</param>
	internal MpqEntry(string? fileName, uint headerOffset, uint fileOffset, uint compressedSize, uint fileSize,
		MpqFileFlags flags) {
		HeaderOffset = headerOffset;
		FileOffset = fileOffset;
		_fileName = fileName;
		CompressedSize = compressedSize;
		FileSize = fileSize;
		Flags = flags;

		if (fileName != null) {
			UpdateEncryptionSeed();
		}
	}

	/// <summary>
	/// Gets the compressed file size of this <see cref="MpqEntry" />.
	/// </summary>
	public uint CompressedSize { get; }

	/// <summary>
	/// Gets the uncompressed file size of this <see cref="MpqEntry" />.
	/// </summary>
	public uint FileSize { get; }

	/// <summary>
	/// Gets the file's flags.
	/// </summary>
	public MpqFileFlags Flags { get; }

	/// <summary>
	/// Gets the encryption seed that is used if the file is encrypted.
	/// </summary>
	public uint EncryptionSeed { get; private set; }

	/// <summary>
	/// Gets the encryption seed for this entry's filename.
	/// </summary>
	/// <remarks>
	/// The base encryption seed is not adjusted for <see cref="MpqFileFlags.BlockOffsetAdjustedKey" />.
	/// </remarks>
	public uint BaseEncryptionSeed { get; private set; }

	/// <summary>
	/// Gets the filename of the file in the archive.
	/// </summary>
	[DisallowNull]
	public string? FileName {
		get => _fileName;
		internal set {
			_fileName = value;
			UpdateEncryptionSeed();
		}
	}

	/// <summary>
	/// Gets the containing <see cref="MpqArchive" />'s header offset.
	/// </summary>
	public uint HeaderOffset { get; }

	/// <summary>
	/// Gets the relative (to the <see cref="MpqHeader" />) position of the file in the archive.
	/// </summary>
	public uint FileOffset { get; }

	/// <summary>
	/// Gets the absolute position of this <see cref="MpqEntry" />'s file in the base stream of the containing
	/// <see cref="MpqArchive" />.
	/// </summary>
	public uint FilePosition => HeaderOffset + FileOffset;

	/// <summary>
	/// Gets a value indicating whether this <see cref="MpqEntry" /> has any of the <see cref="MpqFileFlags.Compressed" />
	/// flags.
	/// </summary>
	public bool IsCompressed => (Flags & MpqFileFlags.Compressed) != 0;

	/// <summary>
	/// Gets a value indicating whether this <see cref="MpqEntry" /> has the flag <see cref="MpqFileFlags.Encrypted" />.
	/// </summary>
	public bool IsEncrypted => Flags.HasFlag(MpqFileFlags.Encrypted);

	/// <summary>
	/// Gets a value indicating whether this <see cref="MpqEntry" /> has the flag <see cref="MpqFileFlags.SingleUnit" />.
	/// </summary>
	public bool IsSingleUnit => Flags.HasFlag(MpqFileFlags.SingleUnit);

	public static MpqEntry Parse(Stream stream, uint headerOffset) {
		using BinaryReader reader = new(stream);
		return FromReader(reader, headerOffset);
	}

	public static MpqEntry FromReader(BinaryReader reader, uint headerOffset) {
		_ = reader ?? throw new ArgumentNullException(nameof(reader));
		return new MpqEntry(null, headerOffset, reader.ReadUInt32(), reader.ReadUInt32(), reader.ReadUInt32(),
			(MpqFileFlags)reader.ReadUInt32());
	}

	/// <inheritdoc />
	public override string ToString() {
		return FileName ?? (Flags == 0 ? "(Deleted file)" : $"Unknown file @ {FilePosition}");
	}

	public void SerializeTo(Stream stream) {
		using (BinaryWriter writer = new(stream, UTF8EncodingProvider.StrictUTF8, true)) {
			WriteTo(writer);
		}
	}

	/// <summary>
	/// Writes the entry to a <see cref="BlockTable" />.
	/// </summary>
	/// <param name="writer">The writer to which the entry is written.</param>
	public void WriteTo(BinaryWriter writer) {
		if (writer is null) {
			throw new ArgumentNullException(nameof(writer));
		}

		writer.Write(FileOffset);
		writer.Write(CompressedSize);
		writer.Write(FileSize);
		writer.Write((uint)Flags);
	}

	internal static uint AdjustEncryptionSeed(uint baseSeed, uint fileOffset, uint fileSize) {
		return baseSeed + fileOffset ^ fileSize;
	}

	internal static uint UnadjustEncryptionSeed(uint adjustedSeed, uint fileOffset, uint fileSize) {
		return (adjustedSeed ^ fileSize) - fileOffset;
	}

	internal static uint CalculateEncryptionSeed(string? fileName) {
		return CalculateEncryptionSeed(fileName, out uint encryptionSeed) ? encryptionSeed : 0;
	}

	internal static bool CalculateEncryptionSeed(string? fileName, out uint encryptionSeed) {
		string? name = fileName.GetFileName();
		if (!string.IsNullOrEmpty(name) && StormBuffer.TryGetHashString(name, 0x300, out encryptionSeed)) {
			return true;
		}

		encryptionSeed = 0;
		return false;
	}

	internal static uint CalculateEncryptionSeed(string? fileName, uint fileOffset, uint fileSize,
		MpqFileFlags flags) {
		if (fileName is null) {
			return 0;
		}

		bool blockOffsetAdjusted = flags.HasFlag(MpqFileFlags.BlockOffsetAdjustedKey);
		uint seed = CalculateEncryptionSeed(fileName);
		if (blockOffsetAdjusted) {
			seed = AdjustEncryptionSeed(seed, fileOffset, fileSize);
		}

		return seed;
	}

	/// <summary>
	/// Try to determine the entry's encryption seed when the filename is not known.
	/// </summary>
	/// <param name="blockPos0">The encrypted value for the first block's offset in the <see cref="MpqStream" />.</param>
	/// <param name="blockPos1">The encrypted value for the second block's offset in the <see cref="MpqStream" />.</param>
	/// <param name="blockPosSize">The size (in bytes) for all the block position offsets in the stream.</param>
	/// <param name="max">The highest possible value that <paramref name="blockPos1" /> can have when decrypted.</param>
	/// <returns>True if the operation was successful, false otherwise.</returns>
	internal bool TryUpdateEncryptionSeed(uint blockPos0, uint blockPos1, uint blockPosSize, uint max) {
		if (!StormBuffer.DetectFileSeed(blockPos0, blockPos1, blockPosSize, out uint result)) {
			List<uint> possibleSeeds = new();
			foreach (uint seed in StormBuffer.DetectFileSeeds(blockPos0, blockPosSize)) {
				uint[] data = { blockPos0, blockPos1 };
				StormBuffer.DecryptBlock(data, seed);
				if (data[1] <= max)
					possibleSeeds.Add(seed);
			}

			if (possibleSeeds.Count != 1)
				return false;

			result = possibleSeeds[0];
		}

		EncryptionSeed = result + 1;
		BaseEncryptionSeed = Flags.HasFlag(MpqFileFlags.BlockOffsetAdjustedKey)
			? UnadjustEncryptionSeed(EncryptionSeed, FileOffset, FileSize)
			: EncryptionSeed;

		return true;
	}

	private void UpdateEncryptionSeed() {
		EncryptionSeed = CalculateEncryptionSeed(_fileName, FileOffset, FileSize, Flags);
		BaseEncryptionSeed = CalculateEncryptionSeed(_fileName);
	}
}