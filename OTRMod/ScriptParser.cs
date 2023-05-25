/* Licensed under the Open Software License version 3.0 */

using OTRMod.ID;
using OTRMod.Utility;
using Action = OTRMod.ID.Action;
using str = System.String;

namespace OTRMod;

public class ScriptParser {
	public str[] ScriptStrings = null!;
	public byte[] ImageData = null!;
	public str SubDir = "";

	private readonly Dictionary<str, byte[]> _var = new();
	private readonly Dictionary<str, str> _def = new();

	private void WorkDo(Action k, str[] val) { /* In line. */
		switch (k) {
			case Action.Get:
				_var.Add(val[1], GetData(val[2], val[3]));
				break;

			case Action.Rep:
				_var[val[1]].Replace(val[2].ReadHex(), to: val[3].ReadHex());
				break;

			case Action.Set:
				_def.SetKey(val[1], val[2]);
				break;

			case Action.Mrg:
				Save(Txt.Merge(_var[val[1]], _var[val[2]],bool.Parse(val[3])),
				GetOutPath(val));
				break;

			case Action.Dir:
				SubDir = val.Length == 1 ? "" : Concatenate(val.Skip(0));
				break;

			case Action.Sav:
				Save(_var[val[1]], GetOutPath(val));
				break;

			case Action.Exp:
				_def.GetKeyStr("AddH", out str addH);
				Save(GetExportData(val, addH.AsBool(true)), GetOutPath(val));
				break;
		}
	}

	private byte[] GetExportData(str[] info, bool addHeader) {
		if (!Enum.TryParse(info[1], out Texture.Codec codec))
			return info[1] switch {
				"Pam" => PlayerAnimation.Export(GetData(info[2], info[3])),
				"Anm" => Animation.Export(GetData(info[2], info[3])),
				"Seq" => Audio.ExportSeq(int.Parse(info[2]), int.Parse(info[3]),
					GetData(info[4], info[5])),
				_ => throw new Exception("Invalid format to export!")
			};

		str texS = _def.GetKeyStr("TexS", "texture size", null!, true);
		str[] size = texS.Split('x');
		int w = int.Parse(size[0]); int h = int.Parse(size[1]);
		byte[] data = GetData(info[2], Texture.GetSize(codec, w * h));
		return addHeader ? Tex.Export(codec, w, h, data) : data;
	}

	private byte[] GetData(object s, object l) => ImageData.Get(s.AsInt(), l.AsInt());
	private str GetOutPath(str[] expInfo) => Concatenate(SubDir, expInfo[^1]);
	private const str DEFAULT_FILE_NAME = "Mod.otr";
	public str OTRFileName = DEFAULT_FILE_NAME;

	public void ParseScript() {
		for (int i = 0; i < ScriptStrings.Length; i += 1) {
			str line = ScriptStrings[i];
			str[] words = line.Split(' ');

			if (line.StartsWith("#") || str.IsNullOrWhiteSpace(line) || str.IsNullOrEmpty(line))
				continue;

			if (!Enum.TryParse(words[0], out Action action))
				throw new Exception($"Invalid action in line {i + 1}: {line}");

			WorkDo(action, words);
		}

		OTRFileName = _def.GetKeyStr("OTRFileName", "", DEFAULT_FILE_NAME);
	}
}