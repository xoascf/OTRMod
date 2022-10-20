using OTRMod.OTR;
using OTRMod.Util;

var otrMs = new MemoryStream();

void Run(string pathROM, string pathScript)
{
	try
	{
		byte[] romBytes = OTRMod.ROM.Convert.ToBigEndian
			(File.ReadAllBytes(pathROM));

		ScriptParser sParser = new ScriptParser
		{
			ScriptStrings = File.ReadAllLines(pathScript),
			ImageData = OTRMod.ROM.Decompress.DecompressedBytes(romBytes),
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

Console.Write("ROM: ");
string rom = Console.ReadLine() ?? string.Empty;
if (rom == string.Empty)
	return;

Console.Write("Script: ");
string script = Console.ReadLine() ?? string.Empty;
if (script == string.Empty)
	return;

Run(rom, script);

Console.Write("Output: ");
string output = Console.ReadLine() ?? string.Empty;
if (output == string.Empty)
	return;

File.WriteAllBytes(output, otrMs.ToArray());