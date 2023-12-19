using OTRMod.Utility;
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
	
	public static string Endec(string t, bool bin, StringsDict replacements) {
		foreach (KeyValuePair<string, string> rep in replacements)
			t = bin ? t.Replace(rep.Key, rep.Value) : t.Replace(rep.Value, rep.Key);

		return t;
	}

	public static string Endec(byte[] data, bool bin, StringsDict replacements) {
		string t = bin ? Encoding.UTF8.GetString(data) : SturmScharf.EncodingProvider.Latin1.GetString(data);

		return Endec(t, bin, replacements);
	}

	public static StringsDict LoadCharMap(string[] lines) {
		StringsDict cm = new();

		foreach (string line in lines) {
			if (!line.Contains("="))
				continue;

			string[] rLine = line.Split(new char[] { '=' }, 2);
			if (rLine[0].IsNullOrEmpty() || rLine[1].IsNullOrEmpty())
				continue;

			if (uint.TryParse(rLine[1], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out uint val))
				if (val > 0x10FFFF)
					continue;
				cm.Add(rLine[0], char.ConvertFromUtf32((int)val));
		}

		return cm;
	}

	public static void AddSingle(this List<byte> list, MessageEntry entry, ref int index) {
		if (entry.Content != null) {
			list.AddRange(ByteArray.FromU16(entry.ID, false));
			list.Add((byte)entry.BoxType);
			list.Add((byte)entry.BoxPos);
			list.AddRange(ByteArray.FromI32(entry.Content.Count, false));
			list.AddRange(entry.Content.ToArray());

			index += 8;
		}
	}

	public static List<MessageEntry> Parse(string input) {
		List<MessageEntry> textMessages = new();

		// Match DEFINE_MESSAGE macro
		string pattern = @"DEFINE_MESSAGE\((\w+),\s*(\w+),\s*(\w+),\s*([\s\S]*?)\n\s*\)";
		Regex regex = new(pattern, RegexOptions.Singleline);

		foreach (Match match in regex.Matches(input)) {
			string messageText = EvalCodes(match.Groups[4].Value.Replace("\n", " ").Replace("\r", " ")) + (char)Code.END;

			// Add text message to list
			textMessages.Add(
			new MessageEntry {
				ID = Convert.ToUInt16(match.Groups[1].Value, 16),
				BoxType = (TextBoxType)Enum.Parse(typeof(TextBoxType), match.Groups[2].Value),
				BoxPos = (TextBoxPosition)Enum.Parse(typeof(TextBoxPosition), match.Groups[3].Value),
				Content = GetContent(0, SturmScharf.EncodingProvider.Latin1.GetBytes(messageText))
			});
		}

		return textMessages;
	}

	public static List<byte> GetContent(int msgOffset, byte[] msgData) {
		List<byte> content = new();

		int msg = msgOffset;
		byte c = msgData[msg];
		int extra = 0;
		bool stop = false;

		while ((c != '\0' && !stop) || extra > 0) {
			content.Add(c);
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

		return content;
	}

	private static string FormatChar(int value) {
		return $"\\x{value:X2}";
	}

	// msgdis.py
	public static string Decode(byte[] readBytes, StringsDict extractionCharmap) {
		bool nextIsColor = false;
		bool nextIsHighscore = false;
		bool nextIsByteMod = false;
		int nextIsHwordMod = 0;
		int nextIsBackground = 0;

		List<string> buf = new();
		foreach (byte b in readBytes) {
			char byteChar = (char)b;
			if (nextIsByteMod) {
				string value = $"\"{FormatChar(b)}\"";
				if (nextIsHighscore) {
					value = ((Highscore)b).ToString();
					nextIsHighscore = false;
				}
				else if (nextIsColor) {
					value = ((Color)b).ToString();
					nextIsColor = false;
				}
				buf.Add(value + ") \"");
				nextIsByteMod = false;
			}
			else if (nextIsHwordMod == 1) {
				buf.Add($"\"{FormatChar(b)}");
				nextIsHwordMod = 2;
			}
			else if (nextIsHwordMod == 2) {
				buf.Add($"{FormatChar(b)}\") \"");
				nextIsHwordMod = 0;
			}
			else if (nextIsBackground == 1) {
				buf.Add($"\"{FormatChar(b)}\", ");
				nextIsBackground = 2;
			}
			else if (nextIsBackground == 2) {
				buf.Add($"\"{FormatChar(b)}\", ");
				nextIsBackground = 3;
			}
			else if (nextIsBackground == 3) {
				buf.Add($"\"{FormatChar(b)}\") \"");
				nextIsBackground = 0;
			}
			else {
				bool foundControlCode = false;
				if (byteChar == '\x01') { // new line
					buf.Add("\n");
					foundControlCode = true;
				}
				else if (Enum.IsDefined(typeof(Code), b)) {
					string name = ((Code)byteChar).ToString();
					switch ((Code)b) {
						case Code.COLOR:
							buf.Add($"\" {name}(");
							nextIsColor = true;
							nextIsByteMod = true;
							break;

						case Code.SHIFT:
						case Code.BOX_BREAK_DELAYED:
						case Code.FADE:
						case Code.ITEM_ICON:
						case Code.TEXT_SPEED:
							buf.Add($"\" {name}(");
							nextIsByteMod = true;
							break;

						case Code.HIGHSCORE:
							buf.Add($"\" {name}(");
							nextIsHighscore = true;
							nextIsByteMod = true;
							break;

						case Code.TEXTID:
						case Code.FADE2:
						case Code.SFX:
							buf.Add($"\" {name}(");
							nextIsHwordMod = 1;
							break;

						case Code.BACKGROUND:
							buf.Add($"\" {name}(");
							nextIsBackground = 1;
							break;

						case Code.BOX_BREAK:
							buf.Add($"\"{name}\"");
							break;

						default:
							if (byteChar == '\x02')
								buf.Add("");
							else
								buf.Add($"\" {name} \"");
							break;
					}
					foundControlCode = true;
				}
				if (foundControlCode)
					continue;

				string charVal = ((char)b).ToString();

				// FIXME: Maybe use Endec instead
				if (extractionCharmap.TryGetKey(charVal, out string key))
					buf.Add(key);
				else {
					string decoded = charVal;
					if (decoded == "\"")
						decoded = "\\\"";
					buf.Add(decoded);
				}
			}
		}

		return string.Concat(buf);
	}

	public static string FixupMessage(string message) {
		return Regex.Replace("\"" + message.Replace("\n", "\\n\"\n\"") + "\"", "(?<!\\)(\"\"))", "").Replace("\n ", "\n")
		   .Replace("\n\"\" ", "\n").Replace("\\\"\"", "\u201C").Replace("\"\"", "")
		   .Replace("BOX_BREAK\"", "\nBOX_BREAK\n\"").Replace("BOX_BREAK ", "\nBOX_BREAK\n")
		   .Replace("\u201C", "\\\"\"").Trim();
	}

	public static string GetTbMsgFormatted(MessageEntry msgEntry, StringsDict extractionCharmap) {
		return $@"DEFINE_MESSAGE(0x{msgEntry.ID:X4}, {msgEntry.BoxType}, {msgEntry.BoxPos},
{FixupMessage(Decode(msgEntry.Content.ToArray(), extractionCharmap))}
)

";
	}
}