/* Licensed under the Open Software License version 3.0 */

namespace OTRMod.Utility;

public static class Misc
{
	private static InvalidCastException InvalidCast(string valueName)
	/* Invalid Cast (s) */=> throw new InvalidCastException($"Invalid cast for '{valueName}'!");

	public static int AsInt(this object number)
	{
		try
		{
			return number switch
			{
				string hex => int.Parse(hex, NumberStyles.HexNumber),
				int integer => integer,
				_ => throw InvalidCast(nameof(number))
			};
		}
		catch { throw InvalidCast(nameof(number)); }
	}

	public static bool AsBool(this string boolean, bool fallback)
	/* As Bool (s, b) */=> bool.TryParse(boolean, out bool result) ? result : fallback;

	public static void SetKey(this Dictionary<string, string> dict, string key,
		string value)
	{
		if (dict.ContainsKey(key))
			dict[key] = value;
		else
			dict.Add(key, value);
	}

	public static bool GetKeyStr(this Dictionary<string, string> dict, string key,
		out string str)
	{
		if (dict.ContainsKey(key))
		{
			str = dict[key];
			return true;
		}

		str = null!;
		return false;
	}

	public static string GetKeyStr(this Dictionary<string, string> dict, string key,
		string desc, object fallback = null!, bool mayThrow = false)
	{
		if (GetKeyStr(dict, key, out string str)) return str;
		if (mayThrow) throw new Exception($"'{key}' ({desc}) has not been set!");

		return fallback.ToString() ?? string.Empty;
	}
}