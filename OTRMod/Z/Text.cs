/* Licensed under the Open Software License version 3.0 */

using static OTRMod.ID.Text;

using OTRMod.Utility;

namespace OTRMod.Z;

public class Text : Resource {
	public byte[] MessageData { get; set; }
	public byte[] TableData { get; set; }
	public bool AddChars { get; set; }

	public Text(byte[] messages, byte[] table, bool addChars) : base(ResourceType.Text) {
		MessageData = messages;
		TableData = table;
		AddChars = addChars;
	}

	public override byte[] Formatted() {
		List<byte> newData = new();
		MessageEntry entry = new();
		int index = 0;
		const string toAdd = "0123456789" +
			"ABCDEFGHIJKLMNOPQRSTUVWXYZ" +
			"abcdefghijklmnopqrstuvwxyz -.";

		while (index < TableData.Length) {
			entry.ID = TableData.ToU16(index);
			entry.BoxType = (TextBoxType)((TableData[index + 2] & 0xF0) >> 4);
			entry.BoxPos = (TextBoxPosition)(TableData[index + 2] & 0x0F);
			entry.Offset = TableData.ToI32(index + 4) & 0x00FFFFFF;
			entry.Content = new();

			int msg = entry.Offset;
			byte c = MessageData[msg];
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

				c = MessageData[msg];
			}

			if (entry.ID is 0xFFFD)
				if (AddChars) {
					entry.ID = 0xFFFC;
					entry.Content.Clear();
					entry.Content.AddRange(System.Text.Encoding.ASCII.GetBytes(toAdd));
				}
				else break;

			if (entry.ID is 0xFFFF) break;

			newData.AddRange(ByteArray.FromU16(entry.ID, false));
			newData.Add((byte)entry.BoxType);
			newData.Add((byte)entry.BoxPos);
			newData.AddRange(ByteArray.FromI32(entry.Content.Count, false));
			newData.AddRange(entry.Content.ToArray());

			index += 8;
		}

		Data = new byte[0x04 + newData.Count];
		Data.Set(0x00, ByteArray.FromI32(index / 8, false)); // Size
		Data.Set(0x04, newData.ToArray());

		return base.Formatted();
	}
}