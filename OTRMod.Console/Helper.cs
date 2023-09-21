/* Licensed under the Open Software License version 3.0 */

using System.Runtime;
using System.Text.RegularExpressions;

namespace OTRMod.Console;

internal static class Helper {
	internal static void Exit(int code) {
		Con.Write("Goodbye!"); Environment.Exit(code);
	}

#if NETCOREAPP1_0_OR_GREATER
	internal static void CompactAndCollect() {
		GCSettings.LargeObjectHeapCompactionMode =
			GCLargeObjectHeapCompactionMode.CompactOnce;
		GC.Collect();
	}
#endif

	internal static string ReadPath
		(string name, string fallback = "", bool checkIfExists = true) {
		string? path;
		do {
			Con.Write($"{name}: ");
			path = Con.ReadLine();
			if (string.IsNullOrWhiteSpace(path)) path = fallback;
		} while (string.IsNullOrWhiteSpace(path));

		if (path.StartsWith("\"") && path.EndsWith("\""))
#if NETCOREAPP3_0_OR_GREATER
			path = path[1..^1];
#else
			path = path.Substring(1, path.Length - 2);
#endif

		while (checkIfExists && !File.Exists(path)) {
			Con.WriteLine("File not found!");
			path = ReadPath(name);
		}

		return path;
	}

	internal static bool AnsweredYesTo(string question) {
		Con.Write($"{question} [Y/n] ");
		string? res = Con.ReadLine();
		return string.IsNullOrEmpty(res)
			|| res.StartsWith("y", StringComparison.OrdinalIgnoreCase);
	}

	internal static string? GetArg(this string[] args, string argMain, string argAlt) {
		int argIndex = Array.IndexOf(args, argMain);
		if (argIndex == -1) argIndex = Array.IndexOf(args, argAlt);

		int nextPos = argIndex + 1;
		if (argIndex != -1 && args.Length > nextPos)
			return args[nextPos];

		return null;
	}

	internal static char[] GetInvalidChars() {
		return new char[] { '<', '>', '|', ':', '*', '?' };
	}

	internal static readonly string
		InvalidCharsPattern = $"[{Regex.Escape(new string(GetInvalidChars()))}]";

	internal static string EncodePath(string path) {
		return Regex.Replace(path, InvalidCharsPattern, m => {
			return "%" + ((int)m.Value[0]).ToString("X2");
		});
	}

	internal static string DecodePath(string path) {
		return Regex.Replace(path, "%[0-9A-F]{2}", m => {
#if NETCOREAPP3_0_OR_GREATER
			string hexValue = m.Value[1..];
#else
			string hexValue = m.Value.Substring(1);
#endif
			char decodedChar = (char)Convert.ToInt32(hexValue, 16);
			if (Array.IndexOf(GetInvalidChars(), decodedChar) != -1)
				return decodedChar.ToString();

			return m.Value;
		});
	}
}