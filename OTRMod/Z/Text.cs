/* Licensed under the Open Software License version 3.0 */

using OTRMod.Utility;

using Code = OTRMod.ID.Text.Codes;

namespace OTRMod.Z;

public class Text : Resource {
	public enum TextBoxType : byte {
		TEXTBOX_TYPE_BLACK,
		TEXTBOX_TYPE_WOODEN,
		TEXTBOX_TYPE_BLUE,
		TEXTBOX_TYPE_OCARINA,
		TEXTBOX_TYPE_NONE_BOTTOM,
		TEXTBOX_TYPE_NONE_NO_SHADOW,
		TEXTBOX_TYPE_CREDITS = 11
	}

	public enum TextBoxPosition : byte {
		TEXTBOX_POS_VARIABLE,
		TEXTBOX_POS_TOP,
		TEXTBOX_POS_MIDDLE,
		TEXTBOX_POS_BOTTOM
	}

	private class MessageEntry {
		public ushort ID;
		public TextBoxType BoxType;
		public TextBoxPosition BoxPos;
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
			entry.ID = tableData.ToU16(index);
			entry.BoxType = (TextBoxType)((tableData[index + 2] & 0xF0) >> 4);
			entry.BoxPos = (TextBoxPosition)(tableData[index + 2] & 0x0F);
			entry.Offset = tableData.ToI32(index + 4) & 0x00FFFFFF;
			entry.Content = new();

			int msg = entry.Offset;
			byte c = messageData[msg];
			int extra = 0;
			bool stop = false;

			while ((c != '\0' && !stop) || extra > 0) {
				entry.Content.Add(c);
				msg++;

				if (extra == 0) {
					if (Enum.IsDefined(typeof(Code), c))
						switch ((Code)c) {
							case Code.END:
								stop = true; break;
							case Code.COLOR:
							case Code.SHIFT:
							case Code.BOX_BREAK_DELAYED:
							case Code.FADE:
							case Code.ITEM_ICON:
							case Code.TEXT_SPEED:
							case Code.HIGHSCORE:
								extra = 1; break;
							case Code.TEXTID:
								extra = 2; stop = true; break;
							case Code.FADE2:
							case Code.SFX:
								extra = 2; break;
							case Code.BACKGROUND:
								extra = 3; break;

						}
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

			newData.AddRange(ByteArray.FromU16(entry.ID, false));
			newData.Add((byte)entry.BoxType);
			newData.Add((byte)entry.BoxPos);
			newData.AddRange(ByteArray.FromI32(entry.Content.Count, false));
			newData.AddRange(entry.Content.ToArray());

			index += 8;
		}

		return Export(newData.ToArray(), ByteArray.FromI32(index / 8, false));
	}
}