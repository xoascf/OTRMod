using System.Collections.Generic;
using System.IO;

namespace SturmScharf; 
public sealed class Attributes {
	public const string FileName = "(attributes)";

	internal Attributes(MpqArchiveCreateOptions mpqArchiveCreateOptions) {
		Unk = 100;
		Flags = mpqArchiveCreateOptions.AttributesFlags;
	}

	internal Attributes(BinaryReader reader) {
		ReadFrom(reader);
	}

	public int Unk { get; set; }

	public AttributesFlags Flags { get; set; }
	public List<int> Crc32s { get; private set; } = new();
	public List<long> FileTimes { get; private set; } = new();
	public List<byte[]> Md5s { get; private set; } = new();

	internal void ReadFrom(BinaryReader reader) {
		Unk = reader.ReadInt32();
		if (Unk != 100) {
			throw new InvalidDataException();
		}

		int flagsValue = reader.ReadInt32();
		if (!Enum.IsDefined(typeof(AttributesFlags), flagsValue)) {
			throw new ArgumentException($"Value '{flagsValue}' is not defined for enum of type {typeof(AttributesFlags).Name}.");
		}

		Flags = (AttributesFlags)flagsValue;

		int bytesPerMpqFile = 0;

		bool hasCrc32 = Flags.HasFlag(AttributesFlags.Crc32);
		if (hasCrc32) {
			bytesPerMpqFile += 4;
		}

		bool hasFileTime = Flags.HasFlag(AttributesFlags.FileTime);
		if (hasFileTime) {
			bytesPerMpqFile += 8;
		}

		bool hasMd5 = Flags.HasFlag(AttributesFlags.Md5);
		if (hasMd5) {
			bytesPerMpqFile += 16;
		}

		long remainingBytes = reader.BaseStream.Length - reader.BaseStream.Position;
		if (bytesPerMpqFile > 0) {
			if (remainingBytes % bytesPerMpqFile != 0) {
				throw new InvalidDataException();
			}

			nint fileCount = (int)remainingBytes / bytesPerMpqFile;

			if (hasCrc32) {
				for (nint i = 0; i < fileCount; i++) {
					Crc32s.Add(reader.ReadInt32());
				}
			}

			if (hasFileTime) {
				for (nint i = 0; i < fileCount; i++) {
					FileTimes.Add(reader.ReadInt64());
				}
			}

			if (hasMd5) {
				for (nint i = 0; i < fileCount; i++) {
					Md5s.Add(reader.ReadBytes(16));
				}
			}
		}
		else if (remainingBytes > 0) {
			throw new InvalidDataException();
		}
	}

	internal void WriteTo(BinaryWriter writer) {
		writer.Write(Unk);
		writer.Write((int)Flags);

		if (Flags.HasFlag(AttributesFlags.Crc32)) {
			foreach (int crc32 in Crc32s) {
				writer.Write(crc32);
			}
		}

		if (Flags.HasFlag(AttributesFlags.FileTime)) {
			foreach (long fileTime in FileTimes) {
				writer.Write(fileTime);
			}
		}

		if (Flags.HasFlag(AttributesFlags.Md5)) {
			foreach (byte[] md5 in Md5s) {
				writer.Write(md5);
			}
		}
	}
}