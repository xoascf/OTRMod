/* Licensed under the Open Software License version 3.0 */

using OTRMod.Utility;
using Action = OTRMod.ID.Action;

namespace OTRMod;

public class ScriptParser {
	public string[] ScriptStrings = null!;
	public byte[] ImageData = null!;
	public string SubDir = "";

	private readonly Dictionary<string, byte[]> _var = new();
	private readonly StringsDict _def = new();

	private void WorkDo(Action k, string[] val) {
		switch (k) {
			case Action.Get:
				_var.Add(val[1], GetData(val[2], val[3]));
				break;

			case Action.Rep:
				_var[val[1]].Replace(val[2].ReadHex(), to: val[3].ReadHex());
				break;

			case Action.Set:
				_def[val[1]] = val[2];
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
				_def.GetKey("AddH", out string addH);
				Save(GetExportData(val, addH.AsBool(true)), GetOutPath(val));
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
				_ => throw new Exception("Invalid format to export.")
			};

		string texS = _def.GetKey("TexS", "texture size", null!, true);
		string[] size = texS.Split('x');
		int w = int.Parse(size[0]);
		int h = int.Parse(size[1]);
		byte[] data = GetData(info[2], Texture.GetSize(codec, w * h));
		return addHeader ? Tex.Export(codec, w, h, data) : data;
	}

	private byte[] GetData(object s, object l) => ImageData.Get(s.AsInt(), l.AsInt());
	private string GetOutPath(string[] expInfo) {
		string path;

#if NETCOREAPP3_0_OR_GREATER
		path = expInfo[^1];
#else
		path = expInfo[expInfo.Length - 1];
#endif

		return Concatenate(SubDir, path);
	}

	private const string DefaultFileName = "Mod.otr";
	public string OTRFileName = DefaultFileName;

	public void ParseScript() {
		for (int i = 0; i < ScriptStrings.Length; i += 1) {
			string line = ScriptStrings[i];
			if (line.StartsWith("#") || string.IsNullOrWhiteSpace(line)) continue;
			string[] words = line.Split(' ');

			if (!Enum.TryParse(words[0], out Action action))
				throw new Exception($"Invalid action on line {i + 1}: {words[0]}.");

			WorkDo(action, words);
		}

		OTRFileName = _def.GetKey("OTRFileName", "", DefaultFileName);
	}
}