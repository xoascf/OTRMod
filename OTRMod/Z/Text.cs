/* Licensed under the Open Software License version 3.0 */

using static OTRMod.ID.Text;

using OTRMod.Utility;

namespace OTRMod.Z;

public class Text : Resource {
	public List<MessageEntry> Entries { get; set; }

	// Used to load from message entry table and the message data files (ROM data)
	public Text(byte[] messages, byte[] table, bool addChars) : base(ResourceType.Text) {
		Entries = GetMessageEntries(messages, table, addChars);
	}

	// Used to load from .h file (ZRET format)
	public Text(byte[] messagesH, StringsDict charMap) : base(ResourceType.Text) {
		Entries = Parse(Endec(messagesH, true, charMap));
	}
	public Text(string messagesH, StringsDict charMap) : base(ResourceType.Text) {
		Entries = Parse(Endec(messagesH, true, charMap));
	}

	// Used to load from SoH formatted binary data (the one in the OTR)
	public Text(List<MessageEntry> entries) : base(ResourceType.Text) {
		Entries = entries;
	}

	private static List<MessageEntry> GetMessageEntries(byte[] messageData, byte[] tableData, bool addChars) {
		List<MessageEntry> entries = new();
		int index = 0;

		do {
			int offset = tableData.ToI32(index + 4) & 0x00FFFFFF;
			ushort msgID = tableData.ToU16(index);

			if (msgID == 0xFFFF)
				break;

			if ((msgID == 0xFFFC || msgID == 0xFFFD) && addChars) {
				MessageEntry PalEntry = new() {
					ID = 0xFFFC,
					Content = new(SturmScharf.EncodingProvider.Latin1.GetBytes(ToAdd))
				};
				entries.Add(PalEntry);
				break;
			}

			MessageEntry entry = new() {
				ID = msgID,
				BoxType = (TextBoxType)((tableData[index + 2] & 0xF0) >> 4),
				BoxPos = (TextBoxPosition)(tableData[index + 2] & 0x0F),
				Content = GetContent(offset, messageData)
			};

			entries.Add(entry);
			index += 8;
		} while (index < tableData.Length);

		return entries;
	}

	public static Text LoadFrom(Resource res) {
		List<MessageEntry> entries = new();
		short totalMessagesCount = res.Data.ToI16(0x00, false);
		short currentMessagesCount = 0;
		int messagePos = 0x04;
		while (currentMessagesCount < totalMessagesCount) {
			int charCount = res.Data.ToI32(messagePos + 4, false);
			MessageEntry entry = new() {
				ID = res.Data.ToU16(messagePos, false),
				BoxType = (TextBoxType)res.Data[messagePos + 2],
				BoxPos = (TextBoxPosition)res.Data[messagePos + 3],
				Content = new(res.Data.Get(messagePos + 8, charCount))
			};

			messagePos += 8 + charCount;

			entries.Add(entry);
			currentMessagesCount++;
		}

		return new(entries) {
			Version = res.Version,
		};
	}

	public string ToHumanReadable(StringsDict extractionCharmap) {
		string messageDataFormattedH = "";
		foreach (MessageEntry entry in Entries)
			messageDataFormattedH += GetTbMsgFormatted(entry, extractionCharmap);

		return messageDataFormattedH;
	}

	// Used for PAL
	private const string ToAdd = "0123456789" +
	"ABCDEFGHIJKLMNOPQRSTUVWXYZ" +
	"abcdefghijklmnopqrstuvwxyz -.";

	public override byte[] Formatted() {
		List<byte> newData = new();
		int index = 0;

		foreach (MessageEntry entry in Entries)
			newData.AddSingle(entry, ref index);

		Data = new byte[0x04 + newData.Count];
		Data.Set(0x00, ByteArray.FromI32(index / 8, false)); // Size
		Data.Set(0x04, newData.ToArray());

		return base.Formatted();
	}
}