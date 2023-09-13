/* Licensed under the Open Software License version 3.0 */

using System.Runtime;
using Con = System.Console;

namespace OTRMod.Console;

internal class Helper {
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
}