/* Licensed under the Open Software License version 3.0 */

using OTRMod.ID;
using OTRMod.Utility;

namespace OTRMod.OTR;

internal static class Format {
	private const int HeaderSize = 0x40;
	private const Endianness Endian = Endianness.LittleEndian;
	private const string DBMagic = "DEADBEEFDEADBEEF";
	private static readonly byte[] EndianData = DBMagic.ReadHex().DataTo(Endian, 0, 8);
	private const ShipVersion MajorVersion = ShipVersion.Deckard;

	internal static class Tex {
		public static byte[] Export(Texture.Codec codec, int width, int height, byte[] input) {
			int texSize = input.Length;

			byte[] data = new byte[HeaderSize + 16 + texSize];

			data.Set(0, GetHeader(ResourceType.Texture));
			data.Set(HeaderSize, (byte)(int)codec); // 4
			data.Set(HeaderSize + 4, (byte)width); // 4
			data.Set(HeaderSize + 8, (byte)height); // 4
			data.Set(HeaderSize + 12, BitConverter.GetBytes(texSize)); // 4
			data.Set(HeaderSize + 16, input);

			return data;
		}
	}

	// ZText.cpp
	internal static class Txt
	{
		private class MessageEntry
		{
			public ushort ID;
			public byte BoxType;
			public byte BoxPos;
			public int Offset;
			public List<byte>? Content;
		}

		public static byte[] Export(byte[] input, byte[] sizeBytes)
		{
			byte[] data = new byte[HeaderSize + 4 + input.Length];

			data.Set(0, GetHeader(ResourceType.Text));
			data.Set(HeaderSize, sizeBytes); // 4
			data.Set(HeaderSize + 4, input);

			return data;
		}

		public static byte[] Merge(byte[] messageData, byte[] tableData, bool addChars)
		{
			List<byte> newData = new();
			MessageEntry entry = new();
			int index = 0;
			const string toAdd = "0123456789" +
				"ABCDEFGHIJKLMNOPQRSTUVWXYZ" +
				"abcdefghijklmnopqrstuvwxyz -.";

			while (index < tableData.Length)
			{
				entry.ID = System.Buffers.Binary.BinaryPrimitives.ReverseEndianness
					(BitConverter.ToUInt16(tableData, index));
				entry.BoxType = (byte)((tableData[index + 2] & 0xF0) >> 4);
				entry.BoxPos = (byte)(tableData[index + 2] & 0x0F);
				entry.Offset =
					(tableData[index + 5 + 2] << 0) |
					(tableData[index + 5 + 1] << 8) |
					(tableData[index + 5 + 0] << 16) & 0x00FFFFFF;
				entry.Content = new List<byte>();

				int msg = entry.Offset;
				byte c = messageData[msg];
				int extra = 0;
				bool stop = false;

				while ((c != '\0' && !stop) || extra > 0) {
					entry.Content.Add(c);
					msg++;

					if (extra == 0)
						switch (c) {
							case 0x02:
								stop = true; break;
							case 0x05: case 0x06: case 0x0C: case 0x0E:
							case 0x13: case 0x14: case 0x1E:
								extra = 1; break;
							case 0x07:
								extra = 2; stop = true; break;
							case 0x11: case 0x12:
								extra = 2; break;
							case 0x15:
								extra = 3; break;
						}
					else extra--;

					c = messageData[msg];
				}

				if (entry.ID is 0xFFFD)
					if (addChars) {
						entry.ID = 0xFFFC;
						entry.Content.Clear();
						entry.Content.AddRange(System.Text.Encoding.ASCII.GetBytes(toAdd));
					}
					else break;

				if (entry.ID is 0xFFFF) break;

				newData.AddRange(BitConverter.GetBytes(entry.ID));
				newData.Add(entry.BoxType);
				newData.Add(entry.BoxPos);
				newData.AddRange(BitConverter.GetBytes(entry.Content.Count));
				newData.AddRange(entry.Content.ToArray());

				index += 8;
			}

			return Export(newData.ToArray(), BitConverter.GetBytes(index / 8));
		}
	}

	internal static class Audio
	{
		public static byte[] ExportSft(int index, byte[] input)
		{
			int fntSize = input.Length;

			byte[] data = new byte[HeaderSize + fntSize];

			data.Set(0, GetHeader(ResourceType.AudioSoundFont, ShipVersion.Rachael));
			data.Set(HeaderSize, (byte)index);
			data.Set(HeaderSize + 3, BitConverter.GetBytes(fntSize));
			data.Set(HeaderSize + 4, input);

			return data;
		}

		public static byte[] ExportSeq(int index, int font, byte[] input)
		{
			const int footerSize = 0x0C;
			int seqSize = input.Length;

			byte[] data = new byte[HeaderSize + seqSize + footerSize];
			byte[] unkBytes = { 0x02, 0x02, 0x01 };

			data.Set(0, GetHeader(ResourceType.AudioSequence, ShipVersion.Rachael));
			data.Set(HeaderSize, BitConverter.GetBytes(seqSize));
			data.Set(HeaderSize + 4, input);
			data.Set(HeaderSize + seqSize + 4, (byte)index);
			data.Set(HeaderSize + seqSize + 5, unkBytes); // FIXME: This is cachePolicy
			data.Set(HeaderSize + seqSize + 11, (byte)font);

			return data;
		}
	}

	public enum AnimationType
	{
		Normal = 0,
		Link = 1,
		Curve = 2,
		Legacy = 3,
	}

	internal static class Animation
	{
		public static byte[] ParseAnimation(byte[] input, AnimationType type)
		{
			int aniSize = input.Length;

			byte[] data = new byte[HeaderSize + 12 + aniSize];

			data.Set(0, GetHeader(ResourceType.Animation));
			data.Set(HeaderSize, AnimationType.Normal); // FIXME: Autodetect!!
			data.Set(HeaderSize + 4, AnimationType.Normal); // FRAMECOUNT
			data.Set(HeaderSize + 10, input.CopyAs(Endianness.ByteSwapped, l: aniSize));

			return data;
		}


		public static byte[] Export(byte[] input)
		{
			int aniSize = input.Length;

			byte[] data = new byte[HeaderSize + 12 + aniSize];

			data.Set(0, GetHeader(ResourceType.Animation));
			data.Set(HeaderSize, AnimationType.Normal); // FIXME: Autodetect!!
			data.Set(HeaderSize + 4, AnimationType.Normal); // FRAMECOUNT
			data.Set(HeaderSize + 10, input.CopyAs(Endianness.ByteSwapped, l: aniSize));

			return data;
		}
	}

	internal static class PlayerAnimation
	{
		public static byte[] Export(byte[] input)
		{
			int aniSize = input.Length;

			byte[] data = new byte[HeaderSize + 4 + aniSize];

			data.Set(0, GetHeader(ResourceType.PlayerAnimation));
			data.Set(HeaderSize, BitConverter.GetBytes(aniSize / 2));
			data.Set(HeaderSize + 4, input.CopyAs(Endianness.ByteSwapped, l: aniSize));

			return data;
		}
	}

	internal static byte[] GetHeader(ResourceType type, ShipVersion version = MajorVersion)
	{
		byte[] header = new byte[HeaderSize];
		// Twice?? (prev: BitConverter.GetBytes((int)resourceType))
		header.Set(0x04, ByteArray.FromInt((int)type).CopyAs(Endian));
		header.Set(0x08, BitConverter.GetBytes((int)version));
		header.Set(0x0C, EndianData);

		return header;
	}
}