/* Licensed under the Open Software License version 3.0 */

using OTRMod;
using OTRMod.OTR;
using OTRMod.ROM;
using static OTRMod.Console.Helper;
using MS = System.IO.MemoryStream;

static MS Run(string pathImgData, bool romMode, bool calc, out string outName) {
	bool decompressOnly = romMode && calc;
	outName = "Decompressed.z64";
	try {
		byte[] iData = File.ReadAllBytes(pathImgData);
		if (romMode)
			iData = Decompressor.Data(iData.ToBigEndian(), calc: calc);
		MS ms;
		if (decompressOnly)
			ms = new MS(iData);
		else {
			ms = new MS();
			ms.SetLength(0);
			ScriptParser sParser = new() {
				ScriptStrings = File.ReadAllLines(ReadPath("Script")),
				ImageData = iData
			};
			sParser.ParseScript();
			outName = sParser.OTRFileName;
			Generate.FromImage(ref ms);
		}

		return ms;
	}
	catch (Exception e) {
		Console.WriteLine(e);
		throw;
	}
}

static void Save(MS ms, string path) {
	using FileStream fs = new(path, FileMode.OpenOrCreate);
	_ = ms.Seek(0, SeekOrigin.Begin);
	ms.CopyTo(fs);
	fs.Flush();
	fs.Close();
}

static void TUIRun(bool romMode, bool calc) {
	using MS genMs = Run(ReadPath("Image"), romMode, calc, out string outName);
	Save(genMs, ReadPath($"Output (default: {outName})", outName, false));
	genMs.Flush();
	genMs.Close();
	CompactAndCollect();
}

Dictionary<int, Action> options = new() {
	{ 1, () => TUIRun(true, false) },
	{ 2, () => TUIRun(false, false) },
	{ 3, () => TUIRun(true, true) },
	{ 4, () => Exit(0) }
};

Console.WriteLine(Console.Title = "OTRMod - Console Mode");

do {
	Console.Write("\nSelect an option:\n" +
		"1. Create OTR mod from ROM (with auto-decompress)\n" +
		"2. Create OTR mod from file\n" +
		"3. Don't create OTR mod, just auto-decompress ROM\n" +
		"4. Exit\n");

	if (!int.TryParse(Console.ReadLine(), out int choice)) {
		Console.WriteLine("Please enter a number.");
		continue;
	}
	if (!options.TryGetValue(choice, out Action? action)) {
		Console.WriteLine("Please select a valid option.");
		continue;
	}

	Console.WriteLine();
	action();
	Console.WriteLine();

	if (AnsweredYesTo("Do you want to start again?"))
		continue;
	Exit(0);

} while (true);