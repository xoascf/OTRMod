/* Licensed under the Open Software License version 3.0 */

namespace OTRMod.Utility;

public static class Misc
{
	private static InvalidCastException InvalidCast(string valueName)
		=> throw new InvalidCastException($"Invalid cast for '{valueName}'.");

	public static int AsInt(this object number) {
		try {
			return number switch {
				string hex => int.Parse(hex, NumberStyles.HexNumber),
				int integer => integer,
				_ => throw InvalidCast(nameof(number))
			};
		}
		catch { throw InvalidCast(nameof(number)); }
	}

	public static bool AsBool(this string boolean, bool fallback)
		=> bool.TryParse(boolean, out bool result) ? result : fallback;

	public static bool GetKey(this StringsDict dict, string key, out string str) {
		if (dict.TryGetValue(key, out string? value)) {
			str = value;
			return true;
		}

		str = null!;
		return false;
	}

	public static string GetKey(this StringsDict dict, string key,
		string desc, object fallback = null!, bool mayThrow = false) {
		if (GetKey(dict, key, out string str)) return str;
		if (mayThrow) throw new Exception($"'{key}' ({desc}) has not been set.");

		return fallback.ToString() ?? string.Empty;
	}

	public static ushort SwapEndian(this ushort val)
		=> (ushort)(val << 8 | val >> 8);

	public static int ToI32BigEndian(byte[] data, int offset)
		=>
		data[offset + 0] << 24 |
		data[offset + 1] << 16 |
		data[offset + 2] << 08 |
		data[offset + 3];
}