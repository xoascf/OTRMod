using System.Text;
using System.Text.RegularExpressions;

namespace OTRMod.ID;

public static class Text {
	public enum Code : byte {
		END = 0x02, BOX_BREAK = 0x04,
		COLOR, SHIFT, TEXTID,
		QUICKTEXT_ENABLE, QUICKTEXT_DISABLE,
		PERSISTENT, // LF (\n)
		EVENT,
		BOX_BREAK_DELAYED, AWAIT_BUTTON_PRESS,
		FADE, NAME, OCARINA,
		FADE2, SFX, ITEM_ICON,
		TEXT_SPEED, BACKGROUND,
		MARATHON_TIME, RACE_TIME,
		POINTS, TOKENS,
		UNSKIPPABLE,
		TWO_CHOICE, THREE_CHOICE,
		FISH_INFO, HIGHSCORE, TIME
	}

	public enum Color {
		DEFAULT = 0x40,
		RED, ADJUSTABLE,
		BLUE, LIGHTBLUE,
		PURPLE, YELLOW,
		BLACK
	}

	public enum Highscore {
		HS_HORSE_ARCHERY, HS_POE_POINTS, HS_LARGEST_FISH,
		HS_HORSE_RACE, HS_MARATHON, HS_DAMPE_RACE = 0x06
	}

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

	public class MessageEntry {
		public ushort ID;
		public TextBoxType BoxType;
		public TextBoxPosition BoxPos;
		public int Offset;
		public List<byte>? Content;
	}

	public static char C(this string hex) => (char)Convert.ToInt32(hex, 16);

	public static char C<TEnum>(string value) where TEnum : struct, Enum {
		return Enum.TryParse(value, out TEnum res)
			? (char)Convert.ToInt32(res)
			: throw new ArgumentException("Invalid hex", nameof(value));
	}

	public static string EvalCodes(string input) {
		StringBuilder sb = new(); bool inQuotes = false;
		string hex = @"\\x([0-9A-Fa-f]{2})";
		input = input.Replace("\\\"", "\u201C"); // Unescape quotes by replacing them
		string codes = $@"^(?<code>{string.Join("|", Enum.GetNames(typeof(Code)))})\b";
		for (int i = 0; i < input.Length; i++) {
			if (input[i] == '\"') { inQuotes = !inQuotes; continue; }
			if (inQuotes) sb.Append(input[i]);
			else {
				string s = input[i..]; Match m; string cn;
				if ((m = Regex.Match(s, @"^COLOR\((?<clr>\w+)\)")).Success)
					sb.Append($"{(char)Code.COLOR}{C<Color>(m.Groups["clr"].Value)}");
				else if ((m = Regex.Match(s, @"^HIGHSCORE\((?<hs>\w+)\)")).Success)
					sb.Append($"{(char)Code.HIGHSCORE}" +
						$"{C<Highscore>(m.Groups["hs"].Value)}");
				else if (s.StartsWith(cn = "ITEM_ICON") || s.StartsWith(cn = "SHIFT") ||
						 s.StartsWith(cn = "FADE") || s.StartsWith(cn = "TEXT_SPEED") ||
						 s.StartsWith(cn = "BOX_BREAK_DELAYED")) {
					m = Regex.Match(s, @$"^{cn}\(\""{hex}\""\)");
					sb.Append($"{C<Code>(cn)}{C(m.Groups[1].Value)}");
				}
				else if (s.StartsWith(cn = "TEXTID") || s.StartsWith(cn = "SFX") ||
						 s.StartsWith(cn = "FADE2")) { // FADE2 is used in STAFF
					m = Regex.Match(s, @$"^{cn}\(\""{hex}{hex}\""\)");
					sb.Append($"{C<Code>(cn)}" +
						$"{C(m.Groups[1].Value)}" +
						$"{C(m.Groups[2].Value)}");
				}
				else if (s.StartsWith(cn = "BACKGROUND")) {
					m = Regex.Match(s,
						@$"^{cn}\(\""{hex}\""\s*,\s*\""{hex}\""\s*,\s*\""{hex}\""\)");
					sb.Append($"{(char)Code.BACKGROUND}" +
						$"{C(m.Groups[1].Value)}" +
						$"{C(m.Groups[2].Value)}" +
						$"{C(m.Groups[3].Value)}");
				}
				else if ((m = Regex.Match(s, codes)).Success)
					sb.Append($"{C<Code>(m.Groups["code"].Value)}");
				if (m.Success) i += m.Length - 1;
			}
		}

		return sb.ToString().Replace("\u201C", "\"");
	}

	public static byte[] AddEndAndAlign(byte[] data) {
		int plusEndSize = data.Length + 1;
		int paddingSize = (4 - (plusEndSize % 4)) % 4;
		byte[] newData = new byte[plusEndSize + paddingSize];
		data.CopyTo(newData, 0); // Copy old data into the new array
		newData[data.Length] = (byte)Code.END; // Add END control code

		return newData;
	}

	public static string Endec(byte[] data, bool bin, StringsDict replacements) {
		string t = bin ? Encoding.UTF8.GetString(data) : SturmScharf.EncodingProvider.Latin1.GetString(data);
		foreach (KeyValuePair<string, string> rep in replacements)
			t = bin ? t.Replace(rep.Key, rep.Value) : t.Replace(rep.Value, rep.Key);

		return t;
	}

	public static StringsDict LoadCharMap(string[] lines) {
		StringsDict cm = new();

		foreach (string line in lines) {
			string[] splitLine = line.Split('=');
			string key = splitLine[0];
			string val = Convert.ToChar(Convert.ToUInt32(splitLine[1], 16)).ToString();
			cm.Add(key, val);
		}

		return cm;
	}

	public static void AddSingle(this List<byte> list, MessageEntry entry) {
		list.AddRange(Utility.ByteArray.FromU16(entry.ID, false));
		list.Add((byte)entry.BoxType);
		list.Add((byte)entry.BoxPos);
		list.AddRange(Utility.ByteArray.FromI32(entry.Content!.Count, false));
		list.AddRange(entry.Content.ToArray());
	}

	public static List<MessageEntry> Parse(string input) {
		List<MessageEntry> textMessages = new();

		// Match DEFINE_MESSAGE macro
		string pattern = @"DEFINE_MESSAGE\((\w+),\s*(\w+),\s*(\w+),\s*([\s\S]*?)\n\s*\)";
		Regex regex = new(pattern, RegexOptions.Singleline);

		foreach (Match match in regex.Matches(input)) {
			ushort textId = Convert.ToUInt16(match.Groups[1].Value, 16);
			TextBoxType type = (TextBoxType)Enum.Parse(typeof(TextBoxType), match.Groups[2].Value);
			TextBoxPosition yPos = (TextBoxPosition)Enum.Parse(typeof(TextBoxPosition), match.Groups[3].Value);
			string messageText = EvalCodes(match.Groups[4].Value.Replace("\n", " ").Replace("\r", " "));
			List<byte> messageData = new(AddEndAndAlign(SturmScharf.EncodingProvider.Latin1.GetBytes(messageText)));

			// Add text message to list
			textMessages.Add(new MessageEntry { ID = textId, BoxType = type, BoxPos = yPos, Content = messageData });
		}

		return textMessages;
	}
}
