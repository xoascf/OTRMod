/* Licensed under the Open Software License version 3.0 */

using OTRMod;
using OTRMod.OTR;
using OTRMod.ROM;

static MemStream Run(string pathImgData, bool romMode, bool calc, out string outName) {
	bool decompressOnly = romMode && calc;
	outName = "Decompressed.z64";
	try {
		byte[] iData = File.ReadAllBytes(pathImgData);
		if (romMode)
			iData = Decompressor.Data(iData.ToBigEndian(), calc: calc);
		MemStream ms;
		if (decompressOnly)
			ms = new MemStream(iData);
		else {
			ms = new MemStream();
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
		Con.WriteLine(e);
		throw;
	}
}

static void Save(Stream s, string path) {
	string? dir = Path.GetDirectoryName(path);
	if (!string.IsNullOrEmpty(dir))
		Directory.CreateDirectory(dir);

	using FileStream fs = new(path, FileMode.OpenOrCreate);
	_ = s.Seek(0, SeekOrigin.Begin);
	s.CopyTo(fs);
	fs.Flush();
	fs.Close();
}

static void TUIRun(bool romMode, bool calc) {
	using MemStream genMs = Run(ReadPath("Image"), romMode, calc, out string outName);
	Save(genMs, ReadPath($"Output (default: {outName})", outName, false));
	genMs.Flush();
	genMs.Close();
#if NETCOREAPP1_0_OR_GREATER
	CompactAndCollect();
#endif
}

static void Extract(string? filePath = null, string? outDir = null) {
	Dictionary<string, Stream> OTRFiles = new();
	filePath ??= ReadPath("OTR");

	Con.WriteLine("Loading...");
	using FileStream fs = new(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
	Load.From(fs, ref OTRFiles);
	outDir ??= ReadPath("Output (default: Extracted)", "Extracted", false);

	Con.WriteLine("Extracting...");
	foreach (KeyValuePair<string, Stream> file in OTRFiles)
		Save(file.Value, Path.Combine(outDir, EncodePath(file.Key)));

	OTRFiles.Clear();
	Con.WriteLine("Extraction complete.");
}

static void Build(string? inputDir = null, string ? outPath = null) {
	MemStream ms = new();
	ms.SetLength(0);

	inputDir ??= ReadPath("Input directory", checkIfExists: false);

	string[] filePaths = Directory.GetFiles(inputDir, "*", SearchOption.AllDirectories);

	foreach (string filePath in filePaths) {
#if NETCOREAPP3_0_OR_GREATER
		string newPath = filePath[inputDir.Length..].Replace(@"\", "/");
		if (newPath.StartsWith("/")) newPath = newPath[1..];
#else
		string newPath = filePath.Substring(inputDir.Length).Replace(@"\", "/");
		if (newPath.StartsWith("/")) newPath = newPath.Substring(1);
#endif
		byte[] fileBytes = File.ReadAllBytes(filePath);
		Generate.AddFile(DecodePath(newPath), fileBytes);
	}

	Generate.FromImage(ref ms);

	if (outPath == null) {
		outPath = $"{Path.GetFileName(inputDir)}.otr";
		outPath = ReadPath($"Output (default: {outPath})", outPath, false);
	}

	Save(ms, outPath);
	ms.Flush();
	ms.Close();
}

if (args.Length > 0) {
	string? otr = args.GetArg("--extract", "-e");
	string? dir = args.GetArg("--dir", "-d");

	if (otr == null && dir == null) {
		dir = args.GetArg("--build", "-b");
		otr = args.GetArg("--otr", "-o");

		Build(dir, otr);

	} else Extract(otr, dir);

	return;
}

Dictionary<int, Action> options = new() {
	{ 1, () => TUIRun(true, false) },
	{ 2, () => TUIRun(false, false) },
	{ 3, () => TUIRun(true, true) },
	{ 4, () => Extract() },
	{ 5, () => Build() },
	{ 6, () => Exit(0) }
};

Con.WriteLine(Con.Title = "OTRMod - Console Mode");

do {
	Con.Write("\nSelect an option:\n" +
		"1. Create OTR mod from ROM (with auto-decompress)\n" +
		"2. Create OTR mod from file\n" +
		"3. Don't create OTR mod, just auto-decompress ROM\n" +
		"4. Extract OTR mod\n" +
		"5. Build OTR from folder\n" +
		"6. Exit\n");

	if (!int.TryParse(Con.ReadLine(), out int choice)) {
		Con.WriteLine("Please enter a number.");
		continue;
	}
	if (!options.TryGetValue(choice, out Action? action)) {
		Con.WriteLine("Please select a valid option.");
		continue;
	}

	Con.WriteLine();
	action();
	Con.WriteLine();

	if (AnsweredYesTo("Do you want to start again?"))
		continue;
	Exit(0);

} while (true);