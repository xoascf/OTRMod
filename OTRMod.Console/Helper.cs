/* Licensed under the Open Software License version 3.0 */

using System.Runtime;
using Con = System.Console;

namespace OTRMod.Console;

internal class Helper {
	internal static void Exit(int code) {
		Con.Write("Goodbye!"); Environment.Exit(code);
	}

	internal static void CompactAndCollect() {
		GCSettings.LargeObjectHeapCompactionMode =
			GCLargeObjectHeapCompactionMode.CompactOnce;
		GC.Collect();
	}

	internal static string ReadPath
		(string name, string fallback = "", bool checkIfExists = true) {
		string? path;
		do {
			Con.Write($"{name}: ");
			path = Con.ReadLine();
			if (string.IsNullOrWhiteSpace(path)) path = fallback;
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
		return string.IsNullOrEmpty(res)
			|| res.StartsWith("y", StringComparison.OrdinalIgnoreCase);
	}
}