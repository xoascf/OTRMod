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
	public List<DateTime> DateTimes { get; private set; } = new();
	public List<byte[]> Unk0x04s { get; private set; } = new();

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

		bool hasDateTime = Flags.HasFlag(AttributesFlags.DateTime);
		if (hasDateTime) {
			bytesPerMpqFile += 8;
		}

		bool hasUnk0x04 = Flags.HasFlag(AttributesFlags.Unk0x04);
		if (hasUnk0x04) {
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

			if (hasDateTime) {
				for (nint i = 0; i < fileCount; i++) {
					DateTimes.Add(new DateTime(reader.ReadInt64(), DateTimeKind.Unspecified));
				}
			}

			if (hasUnk0x04) {
				for (nint i = 0; i < fileCount; i++) {
					Unk0x04s.Add(reader.ReadBytes(16));
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

		if (Flags.HasFlag(AttributesFlags.DateTime)) {
			foreach (DateTime dateTime in DateTimes) {
				writer.Write(dateTime.Ticks);
			}
		}

		if (Flags.HasFlag(AttributesFlags.Unk0x04)) {
			foreach (byte[] unk0x04 in Unk0x04s) {
				writer.Write(unk0x04);
			}
		}
	}
}