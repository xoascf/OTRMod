/* Licensed under the Open Software License version 3.0 */

using OTRMod.OTR;

namespace OTRMod.Util;

public class ScriptParser
{
	public string[] ScriptStrings;
	public byte[] ImageData;
	public string SubDir = "";

	Dictionary<string, byte[]> _var = new Dictionary<string, byte[]>();
	Dictionary<string, string> _def = new Dictionary<string, string>();

	public enum ActionKeywords
	{
		Get, // Get From Image ("var" start(h) length(h))
		Rep, // Replace All ("var" oldBytes(h) newBytes(h))
		Set, // Set or Update value for operation when saving
		Sub, // Substitute Explicit ("var" offset(h) newBytes(h))
		Mrg, // Merge message data and table.
		Dir, // Set New Directory To Save.
		Sav, // Manual Save ("var" "fileName")
		Exp, // Automatic Export
	}

	// In line
	private void WorkDo(ActionKeywords k, string[] val)
	{
		bool addHeader = false;
		if (_def.ContainsKey("AddH"))
			addHeader = bool.Parse(_def["AddH"]);

		switch (k)
		{
			case ActionKeywords.Get:
				byte[] got = GetFrom
					(ImageData, int.Parse(val[2],NumberStyles.HexNumber),
						int.Parse(val[3], NumberStyles.HexNumber));
				_var.Add(val[1], got);
				break;

			case ActionKeywords.Rep:
				_var[val[1]].Replace
				(ByteArray.FromString(val[2]),
					ByteArray.FromString(val[3]));
				break;

			case ActionKeywords.Set:
				if (_def.ContainsKey(val[1]))
					_def[val[1]] = val[2];
				else
					_def.Add(val[1], val[2]);
				break;

			case ActionKeywords.Mrg:
				string savePath = Concatenate(SubDir, val[4]);
				Save(Text.Merge(_var[val[1]], _var[val[2]],
						bool.Parse(val[3])), savePath, ref Generate.SavedFiles);
				break;

			case ActionKeywords.Dir:
				SubDir = val.Length == 1 ? "" : Concatenate(val.Skip(0));
				break;

			case ActionKeywords.Sav:
				SaveAddingHeader(val[1], val[2], addHeader);
				break;

			case ActionKeywords.Exp:
				AutoExport(val, addHeader);
				break;
		}
	}

	private void ParseTex(string texInfo, out Texture.Codec codec, out int w, out int h)
	{
		if (_def.ContainsKey("TexS"))
		{
			string[] size = _def["TexS"].Split('x');
			Enum.TryParse(texInfo, out codec);
			w = int.Parse(size[0]); h = int.Parse(size[1]);
		}
		else
			throw new Exception("'TexS' (texture size) has not been set!");
	}

	// Fast exporting of data.
	private void AutoExport(string[] str, bool addHeader)
	{
		ParseTex(str[1], out Texture.Codec codec, out int w, out int h);
		byte[] toExport = GetFrom
		(ImageData, int.Parse(str[2], NumberStyles.HexNumber),
			Texture.GetLengthFrom(codec, w * h));
		if (addHeader)
			toExport = Texture.AddHeader(codec, w, h, toExport);
		string savePath = Concatenate(SubDir, str[3]);

		Save(toExport, savePath, ref Generate.SavedFiles);
	}

	private void SaveAddingHeader(string varKey, string output, bool addHeader)
	{
		byte[] dataBytes = _var[varKey];
		if (addHeader && varKey.Contains("_"))
		{
			string[] varWords = varKey.Split('_');
			if (varWords.Length == 2)
			{
				ParseTex(varWords[0], out Texture.Codec codec, out int w, out int h);
				dataBytes = Texture.AddHeader(codec, w, h, dataBytes);
			}
			else if (varWords.Length == 3)
			{
				switch (varWords[0])
				{
					case "Seq":
						dataBytes = Format.Sequence.AddHeader
							(int.Parse(varWords[1]),
								int.Parse(varWords[2]), dataBytes);
						break;
					/*case "Fnt": // TODO!
						break; */
				}

			}
		}

		string savePath = Concatenate(SubDir, output);

		Save(dataBytes, savePath, ref Generate.SavedFiles);
	}

	public const string OTRDefaultFileName = "Mod.otr";
	public string OTRFileName = OTRDefaultFileName;

	public void ParseScript()
	{
		for (var i = 0; i < ScriptStrings.Length; i += 1)
		{
			string line = ScriptStrings[i];
			string[] words = line.Split(' ');

			if (string.IsNullOrEmpty(line)) continue;

			if (!Enum.TryParse(words[0], out ActionKeywords action))
			{
				int n = i + 1;
				throw new Exception("Invalid action in line " + n + ": " + line);
			}

			WorkDo(action, words);
		}

		OTRFileName = _def.ContainsKey("OTRFileName") ?
			_def["OTRFileName"] : OTRDefaultFileName;
	}
}