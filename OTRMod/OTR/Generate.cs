/* Licensed under the Open Software License version 3.0 */

using SturmScharf;
using MemStream = System.IO.MemoryStream;

namespace OTRMod.OTR;

public class Generate {
	private static readonly Dictionary<string, MemStream> _files = new();

	public static void AddFile(string path, byte[] data)
		=> _files.Add(path, new MemStream(data));

	public static void AddFile(string path, MemStream data)
		=> _files.Add(path, data);

	public static void FromImage(ref MemStream otrStream) {
		MpqArchiveBuilder builder = new();
		foreach (KeyValuePair<string, MemStream> pair in _files)
			builder.Add(pair.Value, pair.Key.Replace(@"\", "/"));

		_files.Clear();
		builder.SaveTo(otrStream, true);
	}
}