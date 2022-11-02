/* Licensed under the Open Software License version 3.0 */

using System.Buffers.Binary;
using System.Text;
using OTRMod.Utility;

namespace OTRMod.OTR;

internal static class Format
{
	private const int HeaderSize = 0x40;
	private const ByteOrder.Format Endianness = ByteOrder.Format.LittleEndian;
	private const string EndianMagic = "DEADBEEF";
	private static readonly byte[] // Twice??
		EndiannessData = ByteArray.FromString(EndianMagic + EndianMagic).
			DataTo(Endianness, 0, 8);
	private const Version MajorVersion = Version.Deckard;

	internal static class Texture
	{
		public enum Codec
		{
			Unknown,
			RGBA32, RGBA16,
			CI4, CI8,
			I4, I8,
			IA4, IA8, IA16,
		}

		public static int GetLengthFrom(Codec codec, int wxh)
		{
			int length = 0;

			switch (codec)
			{
				case Codec.RGBA32: length = wxh * 4;
					break;
				case Codec.RGBA16: case Codec.IA16: length = wxh * 2;
					break;
				case Codec.CI4: case Codec.I4: case Codec.IA4: length = wxh / 2;
					break;
				case Codec.CI8: case Codec.I8: case Codec.IA8: length = wxh;
					break;
			}

			return length;
		}

		public static byte[] Export(Codec codec, int width, int height, byte[] input)
		{
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

	internal static class Text
	{
		private class MessageEntry
		{
			public ushort MessageID;
			public byte BoxType;
			public byte BoxPos;
			public int Offset;
			public List<byte>? Message;
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
			List<byte> newData = new List<byte>();
			int index = 0;
			MessageEntry msgEntry = new MessageEntry();
			const string toAdd = "0123456789" +
			                     "ABCDEFGHIJKLMNOPQRSTUVWXYZ" +
			                     "abcdefghijklmnopqrstuvwxyz -.";
			while (index < tableData.Length)
			{
				msgEntry.MessageID = BinaryPrimitives.ReverseEndianness
					(BitConverter.ToUInt16(tableData, index));
				msgEntry.BoxType = (byte)((tableData[index + 2] & 0xF0) >> 4);
				msgEntry.BoxPos = (byte)(tableData[index + 2] & 0x0F);
				msgEntry.Offset = (tableData[index + 5 + 2] << 0) |
								  (tableData[index + 5 + 1] << 8) |
								  (tableData[index + 5 + 0] << 16) & 0x00FFFFFF;
				msgEntry.Message = new List<byte>();

				int msg = msgEntry.Offset;
				byte c = messageData[msg];
				int extra = 0;
				bool stop = false;

				while ((c != '\0' && !stop) || extra > 0)
				{
					msgEntry.Message.Add(c);
					msg++;

					if (extra == 0)
						switch (c)
						{
							case 0x05: case 0x06: case 0x13: case 0x0E:
							case 0x0C: case 0x1E: case 0x14: extra = 1;
								break;
							case 0x07: extra = 2; stop = true;
								break;
							case 0x11: case 0x12: extra = 2;
								break;
							case 0x15: extra = 3;
								break;
						}
					else
						extra--;

					c = messageData[msg];
				}

				if (msgEntry.MessageID is 0xFFFD)
					if (addChars)
					{
						msgEntry.MessageID = 0xFFFC; msgEntry.Message.Clear();
						msgEntry.Message.AddRange(Encoding.ASCII.GetBytes(toAdd));
					}
					else
						break;

				if (msgEntry.MessageID is 0xFFFF)
					break;

				newData.AddRange(BitConverter.GetBytes(msgEntry.MessageID));
				newData.Add(msgEntry.BoxType);
				newData.Add(msgEntry.BoxPos);
				newData.AddRange(BitConverter.GetBytes(msgEntry.Message.Count));
				newData.AddRange(msgEntry.Message.ToArray());

				index += 8;
			}

			return Export(newData.ToArray(), BitConverter.GetBytes(index / 8));
		}
	}

	internal static class Audio
	{
		public static byte[] ExportSeq(int index, int font, byte[] input)
		{
			const int footerSize = 0x0C;
			int seqSize = input.Length;

			byte[] data = new byte[HeaderSize + seqSize + footerSize];
			byte[] unkBytes = { 0x02, 0x02, 0x01 };

			data.Set(0, GetHeader(ResourceType.AudioSequence, Version.Rachael));
			data.Set(HeaderSize, BitConverter.GetBytes(seqSize));
			data.Set(HeaderSize + 4, input);
			data.Set(HeaderSize + seqSize + 4, (byte)index);
			data.Set(HeaderSize + seqSize + 5, unkBytes);
			data.Set(HeaderSize + seqSize + 11, (byte)font);

			return data;
		}
	}

	internal static byte[] GetHeader(ResourceType type, Version version = MajorVersion)
	{
		byte[] header = new byte[HeaderSize];
		// Twice?? (prev: BitConverter.GetBytes((int)resourceType))
		header.Set(0x04, ByteArray.FromInt((int)type).CopyAs(Endianness));
		header.Set(0x08, BitConverter.GetBytes((int)version));
		header.Set(0x0C, EndiannessData);

		return header;
	}
}