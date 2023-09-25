using System.IO;
using SB = SturmScharf.StormBuffer;

namespace SturmScharf;

/// <summary>
/// An entry in a <see cref="HashTable" />.
/// </summary>
public struct MpqHash {
	/// <summary>
	/// The length (in bytes) of an <see cref="MpqHash" />.
	/// </summary>
	public const uint Size = 16;

	/// <summary>
	/// Initializes a new instance of the <see cref="MpqHash" /> struct.
	/// </summary>
	public MpqHash(ulong name, MpqLocale locale, uint blockIndex, uint mask)
		: this(name, locale, blockIndex) {
		Mask = mask;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="MpqHash" /> struct.
	/// </summary>
	public MpqHash(BinaryReader reader, uint mask)
		: this(reader.ReadUInt64(), (MpqLocale)reader.ReadUInt32(), reader.ReadUInt32()) {
		Mask = mask;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="MpqHash" /> struct.
	/// </summary>
	public MpqHash(string fileName, uint mask, MpqLocale locale, uint blockIndex)
		: this(GetHashedFileName(fileName), locale, blockIndex, mask) {
	}

	private MpqHash(ulong name, MpqLocale locale, uint blockIndex)
		: this() {
		Name = name;
		Locale = locale;
		BlockIndex = blockIndex;
	}

	public static MpqHash DELETED => new(ulong.MaxValue, (MpqLocale)0xFFFFFFFF, 0xFFFFFFFE);

	public static MpqHash NULL => new(ulong.MaxValue, (MpqLocale)0xFFFFFFFF, 0xFFFFFFFF);

	public ulong Name { get; }

	public MpqLocale Locale { get; }

	public uint BlockIndex { get; }

	public uint Mask { get; }

	/// <summary>
	/// Gets a value indicating whether the <see cref="MpqHash" /> corresponds to an <see cref="MpqEntry" />.
	/// </summary>
	public readonly bool IsEmpty => BlockIndex == 0xFFFFFFFF;

	/// <summary>
	/// Gets a value indicating whether the <see cref="MpqHash" /> has had its corresponding <see cref="MpqEntry" />
	/// deleted.
	/// </summary>
	public readonly bool IsDeleted => BlockIndex == 0xFFFFFFFE;

	/// <summary>
	/// Gets a value indicating whether this <see cref="MpqHash" /> can be overwritten by another hash in the
	/// <see cref="HashTable" />.
	/// </summary>
	public readonly bool IsAvailable => BlockIndex >= 0xFFFFFFFE;

	internal readonly bool IsValidBlockIndex => BlockIndex < 0x00FFFFFF;

	private const string InvalidCharMsg = "Input contains invalid characters larger than 0x200";

	public static uint GetIndex(string path) {
		if (path.ContainsInvalidChar())
			throw new ArgumentException(InvalidCharMsg, nameof(path));

		return SB.HashString(path, 0);
	}

	public static uint GetIndex(string path, uint mask) => GetIndex(path) & mask;

	public static ulong GetHashedFileName(string fileName) {
		if (fileName.ContainsInvalidChar())
			throw new ArgumentException(InvalidCharMsg, nameof(fileName));

		return CombineNames(SB.HashString(fileName, 0x100), SB.HashString(fileName, 0x200));
	}

	/// <inheritdoc />
	public readonly override string ToString() => IsEmpty ? "EMPTY" : IsDeleted ? "DELETED" : $"Entry #{BlockIndex}";

	public readonly void SerializeTo(Stream stream) {
		using BinaryWriter writer = new(stream, EncodingProvider.StrictUTF8, true);
		WriteTo(writer);
	}

	public readonly void WriteTo(BinaryWriter writer) {
		if (writer is null)
			throw new ArgumentNullException(nameof(writer));

		writer.Write(Name);
		writer.Write((uint)Locale);
		writer.Write(BlockIndex);
	}

	private static ulong CombineNames(uint name1, uint name2) => name1 | (ulong)name2 << 32;
}