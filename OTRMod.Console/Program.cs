/* Licensed under the Open Software License version 3.0 */

using OTRMod;
using OTRMod.OTR;
using OTRMod.ROM;
using static OTRMod.Console.Helper;
using Convert = OTRMod.ROM.Convert;
using MS = System.IO.MemoryStream;

static MS Run(string pathImgData, bool romMode, bool calc, out string otrName) {
	otrName = "NoName.otr";
	try {
		byte[] imgBytes;
		if (romMode)
			imgBytes = Decompressor.Data(Convert.ToBigEndian
				(File.ReadAllBytes(pathImgData)), calc: calc);
		else
			imgBytes = File.ReadAllBytes(pathImgData);

		MS ms;
		if (romMode && calc) /*  Send only the decompressed ROM */
			ms = new MS(imgBytes);
		else {
			ms = new MS();
			ms.SetLength(0);
			ScriptParser sParser = new() {
				ScriptStrings = File.ReadAllLines(ReadPath("Script")),
				ImageData = imgBytes
			};
			sParser.ParseScript();
			otrName = sParser.OTRFileName;
			Generate.FromImage(ref ms);
		}

		return ms;
	}
	catch (Exception e) {
		Console.WriteLine(e);
		throw;
	}
}

static void Save(MS ms, string output) {
	using FileStream fs = new(output, FileMode.OpenOrCreate);
	_ = ms.Seek(0, SeekOrigin.Begin); ms.CopyTo(fs); fs.Flush(); fs.Close();
}

static void TUIRun(bool romMode, bool calc) {
	using MS genMS = Run(ReadPath("Image"), romMode, calc, out string otrName);
	Save(genMS, ReadPath($"Output (default: {otrName})", otrName, false));
	genMS.Flush(); genMS.Close();
	CompactAndCollect();
}

Dictionary<int, Action> options = new() {
	{ 1, () => TUIRun(true, false) },
	{ 2, () => TUIRun(false, false) },
	{ 3, () => TUIRun(true, true) },
	{ 4, () => Exit(0) }
};

Console.WriteLine(Console.Title = "OTRMod - Console Mode");

int choice;
do {
	Console.Write("\nSelect an option:\n" +
		"1. Create OTR mod from ROM (with auto-decompress)\n" +
		"2. Create OTR mod from file\n" +
		"3. Don't create OTR mod, just auto-decompress ROM\n" +
		"4. Exit\n");

	if (!int.TryParse(Console.ReadLine(), out choice)) {
		Console.WriteLine("Please enter a number.");
		continue;
	}
	if (!options.TryGetValue(choice, out var action)) {
		Console.WriteLine("Please select a valid option.");
		continue;
	}

	Console.WriteLine(); action(); Console.WriteLine();

	if (AnsweredYesTo("Do you want to start again?"))
		continue;
	else
		Exit(0);

} while (true);