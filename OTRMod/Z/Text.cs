/* Licensed under the Open Software License version 3.0 */

using static OTRMod.ID.Text;

using OTRMod.Utility;

namespace OTRMod.Z;

public class Text : Resource {
	public byte[]? MessageData { get; set; }
	public byte[]? TableData { get; set; }
	public bool AddChars { get; set; }
	public List<MessageEntry>? Entries { get; set; }

	public Text(byte[] messages, byte[] table, bool addChars) : base(ResourceType.Text) {
		MessageData = messages;
		TableData = table;
		AddChars = addChars;
	}

	public Text(byte[] messagesH, string[] charMap, bool addChars) : base(ResourceType.Text) {
		StringsDict replacements = LoadCharMap(charMap);
		Entries = Parse(Endec(messagesH, true, replacements));
		AddChars = addChars;
	}

	// Used for PAL
	private const string ToAdd = "0123456789" +
	"ABCDEFGHIJKLMNOPQRSTUVWXYZ" +
	"abcdefghijklmnopqrstuvwxyz -.";

	public override byte[] Formatted() {
		List<byte> newData = new();
		int index = 0;

		if (TableData != null && MessageData != null) {
			MessageEntry entry = new();
			bool add;
			do {
				entry.ID = TableData.ToU16(index);
				entry.BoxType = (TextBoxType)((TableData[index + 2] & 0xF0) >> 4);
				entry.BoxPos = (TextBoxPosition)(TableData[index + 2] & 0x0F);
				entry.Offset = TableData.ToI32(index + 4) & 0x00FFFFFF;
				entry.Content = new();
				add = AddMessage(newData, entry, ref index, AddChars, MessageData);
			} while (index < TableData.Length && add);

		}
		else if (Entries != null) {
			foreach (MessageEntry entry in Entries) {
				byte[] msgData = entry.Content!.ToArray();
				entry.Content.Clear();
				AddMessage(newData, entry, ref index, AddChars, msgData);
			}
		}

		Data = new byte[0x04 + newData.Count];
		Data.Set(0x00, ByteArray.FromI32(index / 8, false)); // Size
		Data.Set(0x04, newData.ToArray());

		return base.Formatted();
	}

	private static bool AddMessage(List<byte> msgList, MessageEntry entry, ref int index, bool addChars, byte[] msgData) {
		int msg = entry.Offset;
		byte c = msgData[msg];
		int extra = 0;
		bool stop = false;

		while ((c != '\0' && !stop) || extra > 0) {
			entry.Content!.Add(c);
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

			if (msgData.Length > msg)
				c = msgData[msg];
		}

		if (entry.ID is 0xFFFD)
			if (addChars) {
				entry.ID = 0xFFFC;
				entry.Content!.Clear();
				entry.Content.AddRange(System.Text.Encoding.ASCII.GetBytes(ToAdd));
			}
			else return false;

		if (entry.ID is 0xFFFF) return false;

		msgList.AddSingle(entry);
		index += 8;

		return true;
	}
}