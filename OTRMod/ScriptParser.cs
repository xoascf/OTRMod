/* Licensed under the Open Software License version 3.0 */

using OTRMod.OTR;
using OTRMod.Utility;

namespace OTRMod;

public class ScriptParser
{
	public string[] ScriptStrings = null!;
	public byte[] ImageData = null!;
	public string SubDir = "";

	private readonly Dictionary<string, byte[]> _var = new();
	private readonly Dictionary<string, string> _def = new();

	private void WorkDo(ActionKeywords k, string[] val) { /* In line. */
		switch (k) {
			case ActionKeywords.Get:
				_var.Add(val[1], GetData(val[2], val[3]));
				break;

			case ActionKeywords.Rep:
				_var[val[1]].Replace(val[2].ReadHEX(), to: val[3].ReadHEX());
				break;

			case ActionKeywords.Set:
				_def.SetKey(val[1], val[2]);
				break;

			case ActionKeywords.Mrg:
				Save(Text.Merge(_var[val[1]], _var[val[2]],bool.Parse(val[3])),
					GetOutPath(val), ref Generate.SavedFiles);
				break;

			case ActionKeywords.Dir:
				SubDir = val.Length == 1 ? "" : Concatenate(val.Skip(0));
				break;

			case ActionKeywords.Sav:
				Save(_var[val[1]], GetOutPath(val), ref Generate.SavedFiles);
				break;

			case ActionKeywords.Exp:
				_def.GetKeyStr("AddH", out string addH);
				Save(GetExportData(val, addH.AsBool(true)),
					GetOutPath(val), ref Generate.SavedFiles);
				break;
		}
	}

	private byte[] GetExportData(string[] info, bool addHeader) {
		if (!Enum.TryParse(info[1], out Texture.Codec codec))
			return info[1] switch {
				"Pam" => PlayerAnimation.Export(GetData(info[2], info[3])),
				"Anm" => Animation.Export(GetData(info[2], info[3])),
				"Seq" => Audio.ExportSeq(int.Parse(info[2]), int.Parse(info[3]),
					GetData(info[4], info[5])),
				_ => throw new Exception("Invalid format to export!")
			};

		string texS = _def.GetKeyStr("TexS", "texture size", null!, true);
		string[] size = texS.Split('x');
		int w = int.Parse(size[0]); int h = int.Parse(size[1]);
		byte[] data = GetData(info[2], Texture.GetLengthFrom(codec, w * h));
		return addHeader ? Texture.Export(codec, w, h, data) : data;
	}

	private byte[] GetData(object s, object l) => ImageData.Get(s.AsInt(), l.AsInt());
	private string GetOutPath(string[] expInfo) => Concatenate(SubDir, expInfo[^1]);
	public string OTRFileName = "Mod.otr";

	public void ParseScript() {
		for (int i = 0; i < ScriptStrings.Length; i += 1) {
			string line = ScriptStrings[i];
			string[] words = line.Split(' ');

			if (string.IsNullOrEmpty(line)) continue;

			if (!Enum.TryParse(words[0], out ActionKeywords action))
				throw new Exception($"Invalid action in line {i + 1}: {line}");

			WorkDo(action, words);
		}

		OTRFileName = _def.GetKeyStr("OTRFileName", "", "Mod.otr");
	}
}