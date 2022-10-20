/* Licensed under the Open Software License version 3.0 */

using War3Net.IO.Mpq;

namespace OTRMod.OTR;

public class Generate
{
	private static readonly MpqArchiveBuilder
		Builder = new MpqArchiveBuilder();

	public static Dictionary<string, byte[]>
		SavedFiles = new Dictionary<string, byte[]>();

	public static void FromImage(ref MemoryStream otrStream)
	{
		foreach (string file in SavedFiles.Keys)
			Builder.Add(new MemoryStream(SavedFiles[file]), 
				file.Replace(@"\", "/"));

		Builder.SaveTo(otrStream, true);
		SavedFiles.Clear();
	}
}