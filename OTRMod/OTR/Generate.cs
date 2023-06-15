/* Licensed under the Open Software License version 3.0 */

using War3Net.IO.Mpq;
using MS = System.IO.MemoryStream;

namespace OTRMod.OTR;

public class Generate {
	private static readonly Dictionary<string, MS> Files = new();

	public static void AddFile(string path, byte[] data) => Files.Add(path, new MS(data));

	public static void FromImage(ref MS otrStream) {
		MpqArchiveBuilder builder = new();
		foreach (KeyValuePair<string, MS> pair in Files)
			builder.Add(pair.Value, pair.Key.Replace(@"\", "/"));

		Files.Clear();
		builder.SaveTo(otrStream, true);
	}
}