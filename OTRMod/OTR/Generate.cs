/* Licensed under the Open Software License version 3.0 */

using War3Net.IO.Mpq;
using MS = System.IO.MemoryStream;

namespace OTRMod.OTR;

public class Generate {
	private static readonly Dictionary<string, MS> _files = new();

	public static void AddFile(string path, byte[] data) => _files.Add(path, new MS(data));

	public static void FromImage(ref MS otrStream) {
		MpqArchiveBuilder builder = new();
		foreach (KeyValuePair<string, MS> pair in _files)
			builder.Add(pair.Value, pair.Key.Replace(@"\", "/"));

		_files.Clear();
		builder.SaveTo(otrStream, true);
	}
}