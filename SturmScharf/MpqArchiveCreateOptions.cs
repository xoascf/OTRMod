namespace SturmScharf;

public sealed class MpqArchiveCreateOptions {
	/// <summary>
	/// Default value for an <see cref="MpqArchive" />'s blocksize.
	/// </summary>
	public const ushort DefaultBlockSize = 3;

	public MpqArchiveCreateOptions() {
		BlockSize = DefaultBlockSize;
		HashTableSize = null;
		WriteArchiveFirst = true;
		ListFileCreateMode = null;
		AttributesCreateMode = null;
		AttributesFlags = AttributesFlags.Crc32 | AttributesFlags.FileTime;
	}

	/// <summary>
	/// The size of blocks in compressed files, which is used to enable seeking.
	/// </summary>
	public ushort BlockSize { get; set; }

	/// <summary>
	/// The desired size of the <see cref="BlockTable" />. Larger size decreases the likelihood of hash collisions.
	/// </summary>
	public ushort? HashTableSize { get; set; }

	/// <summary>
	/// If <see langword="true" />, the archive files will be positioned directly after the header. Otherwise, the
	/// hashtable and blocktable will come first.
	/// </summary>
	public bool WriteArchiveFirst { get; set; }

	public MpqFileCreateMode? SignatureCreateMode { get; set; }

	public string? SignaturePrivateKey { get; set; }

	public MpqFileCreateMode? ListFileCreateMode { get; set; }

	public MpqFileCreateMode? AttributesCreateMode { get; set; }

	public AttributesFlags AttributesFlags { get; set; }
}