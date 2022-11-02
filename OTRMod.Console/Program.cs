using OTRMod.OTR;
using OTRMod.Utility;

var otrMs = new MemoryStream();
bool romMode;

void Run(string pathImgData, string pathScript)
{
	try
	{
		byte[] imgBytes;
		if (romMode)
			imgBytes = OTRMod.ROM.Decompress.DecompressedData
			(OTRMod.ROM.Convert.ToBigEndian
				(File.ReadAllBytes(pathImgData)));
		else
			imgBytes = File.ReadAllBytes(pathImgData);

		ScriptParser sParser = new ScriptParser
		{
			ScriptStrings = File.ReadAllLines(pathScript),
			ImageData = imgBytes,
		};
		sParser.ParseScript();
		otrMs.SetLength(0);
		Generate.FromImage(ref otrMs);
	}
	catch (Exception e)
	{
		Console.WriteLine(e);
		throw;
	}

}

Console.Write("Is it a ROM?: ");
romMode = Convert.ToBoolean(Console.ReadLine());

Console.Write("Image: ");
string img = Console.ReadLine() ?? string.Empty;
if (img == string.Empty)
	return;

Console.Write("Script: ");
string script = Console.ReadLine() ?? string.Empty;
if (script == string.Empty)
	return;

Run(img, script);

Console.Write("Output: ");
string output = Console.ReadLine() ?? string.Empty;
if (output == string.Empty)
	return;

File.WriteAllBytes(output, otrMs.ToArray());