/* Licensed under the Open Software License version 3.0 */

using OTRMod.Utility;

namespace OTRMod.Z;

public class Text : Resource {
	private class MessageEntry {
		public ushort ID;
		public byte BoxType;
		public byte BoxPos;
		public int Offset;
		public List<byte>? Content;
	}

	public static byte[] Export(byte[] input, byte[] sizeBytes) {
		byte[] data = new byte[HeaderSize + 4 + input.Length];

		data.Set(0, GetHeader(ResourceType.Text));
		data.Set(HeaderSize, sizeBytes); // 4
		data.Set(HeaderSize + 4, input);

		return data;
	}

	public static byte[] Merge(byte[] messageData, byte[] tableData, bool addChars) {
		List<byte> newData = new();
		MessageEntry entry = new();
		int index = 0;
		const string toAdd = "0123456789" +
			"ABCDEFGHIJKLMNOPQRSTUVWXYZ" +
			"abcdefghijklmnopqrstuvwxyz -.";

		while (index < tableData.Length) {
			entry.ID = BitConverter.ToUInt16(tableData, index).SwapEndian();
			entry.BoxType = (byte)((tableData[index + 2] & 0xF0) >> 4);
			entry.BoxPos = (byte)(tableData[index + 2] & 0x0F);
			entry.Offset = Misc.ToI32BigEndian(tableData, index + 4) & 0x00FFFFFF;
			entry.Content = new();

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
						case 0x05:
						case 0x06:
						case 0x0C:
						case 0x0E:
						case 0x13:
						case 0x14:
						case 0x1E:
							extra = 1; break;
						case 0x07:
							extra = 2; stop = true; break;
						case 0x11:
						case 0x12:
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
				else {
					break;
				}

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