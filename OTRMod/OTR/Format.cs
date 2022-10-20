/* Licensed under the Open Software License version 3.0 */

using System.Buffers.Binary;
using System.Text;

namespace OTRMod.OTR;

internal static class Format
{
	private static readonly byte[] /* Little Endian: DE AD BE EF */
		EndiannessBytes = { 0xEF, 0xBE, 0xAD, 0xDE, 0xEF, 0xBE, 0xAD, 0xDE };

	internal static class Texture
	{
		private const int HeaderSize = 0x50;

		public enum Codec
		{
			Unknown,
			RGBA32,
			RGBA16,
			CI4,
			CI8,
			I4,
			I8,
			IA4,
			IA8,
			IA16,
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

		public static byte[] AddHeader
			(Codec codec, int width, int height, byte[] data)
		{
			int texSize = data.Length;

			byte[] withHeaderData = new byte[HeaderSize + texSize];
			byte[] headerData = new byte[HeaderSize];
			byte[] texBytes = { 0x58, 0x45, 0x54, 0x4F };
			byte[] typeBytes = { (byte)(int)codec };
			byte[] widthBytes = { (byte)width };
			byte[] heightBytes = { (byte)height };

			headerData.Set(0x04, texBytes);
			headerData.Set(0x0C, EndiannessBytes);
			headerData.Set(0x40, typeBytes);
			headerData.Set(0x44, widthBytes);
			headerData.Set(0x48, heightBytes);
			headerData.Set(0x4C, BitConverter.GetBytes(texSize));
			withHeaderData.Set(0x00, headerData);
			withHeaderData.Set(HeaderSize, data);

			return withHeaderData;
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
		};

		private const int HeaderSize = 0x44;

		public static byte[] AddHeader(byte[] data, byte[] sizeBytes)
		{
			byte[] withHeaderData = new byte[HeaderSize + data.Length];
			byte[] headerData = new byte[HeaderSize];
			byte[] txtBytes = { 0x54, 0x58, 0x54, 0x4F };

			headerData.Set(0x04, txtBytes);
			headerData.Set(0x0C, EndiannessBytes);
			headerData.Set(0x40, sizeBytes);
			withHeaderData.Set(0x00, headerData);
			withHeaderData.Set(HeaderSize, data);

			return withHeaderData;
		}

		public static byte[] Merge(byte[] messageData, byte[] tableData, bool addChars)
		{
			List<byte> newData = new List<byte>();
			int index = 0;

			MessageEntry msgEntry = new MessageEntry();
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

				if (msgEntry.MessageID == 0xFFFD && addChars)
				{
					msgEntry.MessageID = 0xFFFC;
					msgEntry.Message.Clear();
					const string toAdd = "0123456789" +
					                     "ABCDEFGHIJKLMNOPQRSTUVWXYZ" +
					                     "abcdefghijklmnopqrstuvwxyz -.";
					msgEntry.Message.AddRange(Encoding.ASCII.GetBytes(toAdd));
				}
				else if (msgEntry.MessageID == 0xFFFD && addChars == false)
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

			return AddHeader(newData.ToArray(), BitConverter.GetBytes(index / 8));
		}
	}

	internal static class Sequence
	{
		private const int HeaderSize = 0x40;
		private const int FooterSize = 0x0C;

		public static byte[] AddHeader(int index, int font, byte[] data)
		{
			int seqSize = data.Length;

			byte[] withHeaderData = new byte[HeaderSize + seqSize + FooterSize];
			byte[] headerData = new byte[HeaderSize];
			byte[] seqBytes = { 0x51, 0x45, 0x53, 0x4F, 0x02 };
			byte[] unkBytes = { 0x02, 0x02, 0x01 };
			byte[] indexBytes = { (byte)index };
			byte[] fontBytes = { (byte)font };

			headerData.Set(0x04, seqBytes);
			headerData.Set(0x0C, EndiannessBytes);
			withHeaderData.Set(0x00, headerData);
			withHeaderData.Set(HeaderSize, data);
			withHeaderData.Set(0x40, BitConverter.GetBytes(seqSize));
			withHeaderData.Set(HeaderSize + seqSize + 4, indexBytes);
			withHeaderData.Set(HeaderSize + seqSize + 5, unkBytes);
			withHeaderData.Set(HeaderSize + seqSize + 11, fontBytes);

			return withHeaderData;
		}
	}
}