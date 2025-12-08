/* Licensed under the Open Software License version 3.0 */

using Ionic.Zip;
using SturmScharf;
using MemStream = System.IO.MemoryStream;

namespace OTRMod.OTR;

public class Generate {
	private static readonly Dictionary<string, MemStream> _files = new();

	public static void AddFile(string path, byte[] data)
		=> _files.Add(path.Replace(@"\", "/"), new MemStream(data));

	public static void AddFile(string path, MemStream data)
		=> _files.Add(path.Replace(@"\", "/"), data);

	/* OTR (MPQ-based) */
	public static void FromImage(ref MemStream otrStream) {
		MpqArchiveBuilder builder = new();

		foreach (KeyValuePair<string, MemStream> pair in _files) {
			builder.Add(pair.Value, pair.Key);
			pair.Value.Close();
		}

		_files.Clear();

		builder.SaveTo(otrStream, true);
	}

	/* O2R (ZIP-based) */
	public static void FromImageO2R(ref MemStream o2rStream) {
		var date = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);

		using ZipFile zipFile = new();
		zipFile.CompressionLevel = Ionic.Zlib.CompressionLevel.BestCompression;
		zipFile.CompressionMethod = CompressionMethod.Deflate;
		zipFile.EmitTimesInWindowsFormatWhenSaving = false;

		foreach (KeyValuePair<string, MemStream> pair in _files) {
			var entry = zipFile.AddEntry(pair.Key, pair.Value);
			entry.SetEntryTimes(date, date, date);
			entry.Attributes = System.IO.FileAttributes.Normal;
		}

		_files.Clear();

		zipFile.Save(o2rStream);
		zipFile.Dispose();
	}
}