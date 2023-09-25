using System.Diagnostics.CodeAnalysis;

#pragma warning disable CA1050 // Declare types in namespaces
// ReSharper disable once CheckNamespace
public static class StringExtensions {
#pragma warning restore CA1050 // Declare types in namespaces
	public static bool ContainsInvalidChar(this string path) {
		for (int i = 0; i < path.Length; i++)
			if (path[i] >= 0x200)
				return true;

		return false;
	}

	public static string? GetFileName(this string? s) {
		if (s == null)
			return null;

		string[] split =
			s.Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);
		return split.Length > 0 ? split[^1] : null;
	}

	internal static ulong GetStringHash(this string s)
		=> SturmScharf.MpqHash.GetHashedFileName(s);

	public static bool IsNullOrEmpty([NotNullWhen(false)] this string? data)
		=> string.IsNullOrEmpty(data);
}