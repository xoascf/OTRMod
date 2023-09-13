namespace SturmScharf.Extensions;

internal static class StringExtensions {
	internal static bool ContainsInvalidChar(this string path) {
		for (int i = 0; i < path.Length; i++)
			if (path[i] >= 0x200) return true;

		return false;
	}

	internal static string? GetFileName(this string? s) {
		if (s == null)
			return null;

		string[] split =
			s.Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);
		return split.Length > 0 ? split[split.Length - 1] : null;
	}

	internal static ulong GetStringHash(this string s) {
		return MpqHash.GetHashedFileName(s);
	}
}