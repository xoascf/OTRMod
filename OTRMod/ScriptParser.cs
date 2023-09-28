/* Licensed under the Open Software License version 3.0 */

using OTRMod.Utility;
using OTRMod.Z;
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
				_var.Add(val[1], ImageData.GetData(val[2], val[3]));
				break;

			case Action.Rep:
				_var[val[1]].Replace(val[2].ReadHex(), to: val[3].ReadHex());
				break;

			case Action.Set:
				_def[val[1]] = val.Join(" ", 2);
				break;

			case Action.Mrg:
				Save(Text.Merge(_var[val[1]], _var[val[2]], bool.Parse(val[3])),
					GetOutPath(val));
				break;

			case Action.Dir:
				SubDir = val.Length == 1 ? "" : Concatenate(val.Skip(0));
				break;

			case Action.Sav:
				Save(_var[val[1]], GetOutPath(val));
				break;

			case Action.Exp:
				Save(GetExportData(val), GetOutPath(val));
				break;
		}
	}

	private byte[] GetTextureData(byte[] input, ID.Texture.Codec codec, string start) {
		_def.GetKey("AddH", out string addH);
		string texS = _def.GetKey("TexS", "texture size", null!, true);
		string[] size = texS.Split('x');
		int w = int.Parse(size[0]); int h = int.Parse(size[1]);
		byte[] data = input.GetData(start, ID.Texture.GetSize(codec, w * h));
		return addH.AsBool(true) ? Z.Texture.Export(codec, w, h, data) : data;
	}

	private byte[] GetExportData(string[] info) {
		if (Enum.TryParse(info[1], out ID.Texture.Codec codec))
			return GetTextureData(ImageData, codec, info[2]);

		byte[] obj = _var[_def["Obj"]];
		return info[1] switch {
			"Anm" => Animation.Export(obj, info[2].AsInt()),
			"Tex" => GetTextureData(obj, Misc.Parse<ID.Texture.Codec>(info[2]), info[3]),
			_ => throw new Exception("Invalid format to export.")
		};
	}

	private string GetOutPath(string[] expInfo) => Concatenate(SubDir, expInfo[^1]);

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