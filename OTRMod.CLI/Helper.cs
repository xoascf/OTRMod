/* Licensed under the Open Software License version 3.0 */

#if NETCOREAPP1_0_OR_GREATER
using System.Runtime;
#endif
using System.Text.RegularExpressions;

namespace OTRMod.CLI;

internal static class Helper {
	internal static void Exit(int code) {
		Con.WriteLine("Exiting...");
		Environment.Exit(code);
	}

#if NETCOREAPP1_0_OR_GREATER
	internal static void CompactAndCollect() {
		GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
		GC.Collect();
	}
#endif

	internal static string ReadPath(string name, string fallback = "", bool checkIfExists = true) {
		string? path;
		do {
			if (!string.IsNullOrWhiteSpace(fallback))
				name += $" (default: {fallback})";

			Con.Write($"{name}: ");
			path = Con.ReadLine();
			if (string.IsNullOrWhiteSpace(path))
				path = fallback;

		} while (string.IsNullOrWhiteSpace(path));

		if (path.StartsWith("\"") && path.EndsWith("\""))
			path = path[1..^1];

		while (checkIfExists && !File.Exists(path)) {
			Con.WriteLine("File not found!");
			path = ReadPath(name);
		}

		return path;
	}

	internal static bool AnsweredYesTo(string question) {
		Con.Write($"{question} [Y/n] ");
		string? res = Con.ReadLine();
		return res.IsNullOrEmpty()
			|| res.StartsWith("y", StringComparison.OrdinalIgnoreCase);
	}

	internal static string? GetIfExists(this string[] array, int index)
		=> array.Length > index ? array[index] : null;

	internal static int GetArgIndex(this string[] args, string argMain, char argAlt) {
		int argIndex = Array.IndexOf(args, $"--{argMain}");

		return argIndex == -1 ? Array.IndexOf(args, $"-{argAlt}") : argIndex;
	}

	internal static string? GetArg(this string[] args, string argMain, char argAlt) {
		int argIndex = args.GetArgIndex(argMain, argAlt);

		return argIndex != -1 ? args.GetIfExists(argIndex + 1) : null;
	}

	internal static char[] GetInvalidChars()
		=> new[] { '<', '>', '|', ':', '*', '?' };

	internal static readonly string InvalidCharsPattern
		= $"[{Regex.Escape(new string(GetInvalidChars()))}]";

	internal static string EncodePath(string path)
		=> Regex.Replace(path, InvalidCharsPattern, m
		=> "%" + ((int)m.Value[0]).ToString("X2"));

	internal static string DecodePath(string path)
		=> Regex.Replace(path, "%[0-9A-F]{2}", hex => {
			char decodedChar = (char)Convert.ToInt32(hex.Value[1..], 16);
			return Array.IndexOf(GetInvalidChars(), decodedChar) != -1
			? decodedChar.ToString()
			: hex.Value;
		});

	internal static FileStream GetReadable(this string file)
		=> new(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

	internal static void Save(this Stream s, string path) {
		string? dir = Path.GetDirectoryName(path);
		if (!dir.IsNullOrEmpty())
			Directory.CreateDirectory(dir);

		using FileStream fs = new(path, FileMode.OpenOrCreate);
		_ = s.Seek(0, SeekOrigin.Begin);
		s.CopyTo(fs);
		fs.Flush();
		fs.Close();
	}

	internal static void Warn(string issue, int line, string file)
		=> Con.WriteLine($"::warning file={file},line={line}::{issue}");
}