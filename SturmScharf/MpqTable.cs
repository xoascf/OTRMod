using System.IO;

namespace SturmScharf;

/// <summary>
/// A table in an <see cref="MpqArchive" />.
/// </summary>
public abstract class MpqTable {
	/// <summary>
	/// The maximum capacity of an <see cref="MpqTable" />.
	/// </summary>
	public const int MaxSize = 0x1 << 16;

	/// <summary>
	/// Initializes a new instance of the <see cref="MpqTable" /> class.
	/// </summary>
	internal MpqTable() {
	}

	/// <summary>
	/// Gets the capacity of the <see cref="MpqTable" />.
	/// </summary>
	public abstract uint Size { get; }

	/// <summary>
	/// Gets the key used to encrypt and decrypt the <see cref="MpqTable" />.
	/// </summary>
	protected abstract string Key { get; }

	/// <summary>
	/// Gets the length (in bytes) of a single entry in the <see cref="MpqTable" />.
	/// </summary>
	protected abstract int EntrySize { get; }

	/// <summary>
	/// Encrypts the contents of the <see cref="MpqTable" />.
	/// </summary>
	/// <param name="data">The unencrypted entries in the table.</param>
	internal void Encrypt(byte[] data)
		=> StormBuffer.EncryptBlock(data, StormBuffer.HashString(Key, 0x300));

	/// <summary>
	/// Decrypts the contents of the <see cref="MpqTable" />.
	/// </summary>
	/// <param name="data">The encrypted entries in the table.</param>
	internal void Decrypt(byte[] data)
		=> StormBuffer.DecryptBlock(data, StormBuffer.HashString(Key, 0x300));

	/// <summary>
	/// Write the entire <see cref="MpqTable" />'s encrypted contents to the <paramref name="stream" />.
	/// </summary>
	/// <param name="stream">The <see cref="Stream" /> to write the contents to.</param>
	internal void SerializeTo(Stream stream) {
		using BinaryWriter writer = new(stream, EncodingProvider.StrictUTF8, true);
		WriteTo(writer);
	}

	/// <summary>
	/// Write the entire <see cref="MpqTable" />'s encrypted contents to the <paramref name="writer" />.
	/// </summary>
	/// <param name="writer">The <see cref="BinaryWriter" /> to write the contents to.</param>
	internal void WriteTo(BinaryWriter writer) {
		using MemoryStream memoryStream = new(GetEncryptedData());
		using BinaryReader reader = new(memoryStream);
		long end = memoryStream.Length;
		for (int i = 0; i < end; i++)
			writer.Write(reader.ReadByte());
	}

	/// <summary>
	/// Writes the entry at index <paramref name="i" />.
	/// </summary>
	/// <param name="writer">The <see cref="BinaryWriter" /> to write the content to.</param>
	/// <param name="i">The index of the entry to write.</param>
	protected abstract void WriteEntry(BinaryWriter writer, int i);

	private byte[] GetEncryptedData() {
		using MemoryStream memoryStream = new();

		using (BinaryWriter writer = new(memoryStream, EncodingProvider.StrictUTF8, true))
			for (int i = 0; i < Size; i++)
				WriteEntry(writer, i);

		memoryStream.Position = 0;

		using BinaryReader reader = new(memoryStream);
		byte[] data = reader.ReadBytes((int)Size * EntrySize);
		Encrypt(data);

		return data;
	}
}