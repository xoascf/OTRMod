/* Licensed under the Open Software License version 3.0 */

using SturmScharf;
using MemStream = System.IO.MemoryStream;

namespace OTRMod.OTR;

public class Generate {
	private static readonly Dictionary<string, MemStream> Files = new();

	public static void AddFile(string path, byte[] data)
		=> Files.Add(path, new MemStream(data));

	public static void FromImage(ref MemStream otrStream) {
		MpqArchiveBuilder builder = new();
		foreach (KeyValuePair<string, MemStream> pair in Files)
			builder.Add(pair.Value, pair.Key.Replace(@"\", "/"));

		Files.Clear();
		builder.SaveTo(otrStream, true);
	}
}