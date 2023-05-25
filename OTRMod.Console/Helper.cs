/* Licensed under the Open Software License version 3.0 */

using System.Runtime;
using Cnsl = System.Console;

namespace OTRMod.Console;

internal class Helper {
	internal static void Exit(int code) {
		Cnsl.Write("Goodbye!"); Environment.Exit(code);
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
			Cnsl.Write($"{name}: ");
			path = Cnsl.ReadLine();
			if (string.IsNullOrWhiteSpace(path)) path = fallback;
		} while (string.IsNullOrWhiteSpace(path));

		if (path.StartsWith("\"") && path.EndsWith("\""))
			path = path[1..^1];

		while (checkIfExists && !File.Exists(path)) {
			Cnsl.WriteLine("File not found!");
			path = ReadPath(name);
		}

		return path;
	}

	internal static bool AnsweredYesTo(string question) {
		Cnsl.Write($"{question} [Y/n] ");
		string? res = Cnsl.ReadLine();
		return string.IsNullOrEmpty(res)
			|| res.StartsWith("y", StringComparison.OrdinalIgnoreCase);
	}
}