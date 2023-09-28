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

	// https://stackoverflow.com/a/29404026
	public static byte[] SwapByteArray(byte[] a) {
		// If array is odd we set limit to a.Length - 1.
		int limit = a.Length - (a.Length % 2);
		if (limit < 1) throw new Exception("Array too small to be swapped.");
		for (int i = 0; i < limit - 1; i += 2)
			(a[i + 1], a[i]) = (a[i], a[i + 1]);

		return a;
	}

	public static byte[] GetData(this byte[] data, object start, object length)
		=> data.Get(start.AsInt(), length.AsInt());

	public static TEnum Parse<TEnum>(string value) where TEnum : struct
		=> (TEnum)Enum.Parse(typeof(TEnum), value);
}