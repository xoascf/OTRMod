/* Licensed under the Open Software License version 3.0 */

using Ionic.Zip;
using SturmScharf;
using System.IO;
using MemStream = System.IO.MemoryStream;

namespace OTRMod.OTR;

public static class Load {
	// All from... (for MPQ files)
	public static void From(Stream s, ref Dictionary<string, Stream> files) {
		using MpqArchive archive = MpqArchive.Open(s, true);
		foreach (MpqFile file in archive.GetMpqFiles())
			if (file is MpqKnownFile kf) {
				if (kf.FileName is "(listfile)" or "(attributes)")
					continue;

				Stream dataStream = new MemStream();
				kf.MpqStream.CopyTo(dataStream);
				kf.MpqStream.Close();
				files.Add(kf.FileName, dataStream);
			}
	}

	// Search like... (for MPQ files)
	public static void OnlyFrom
	(string fileName, Stream s, ref Dictionary<string, Stream> files) {
		using MpqArchive archive = MpqArchive.Open(s, true);
		foreach (MpqFile file in archive.GetMpqFiles())
			if (file is MpqKnownFile kf && kf.FileName.Contains(fileName)) {
				Stream dataStream = new MemStream();
				kf.MpqStream.CopyTo(dataStream);
				kf.MpqStream.Close();
				files.Add(kf.FileName, dataStream);
			}
	}

	// All from... (for O2R/ZIP files)
	public static void FromO2R(Stream s, ref Dictionary<string, Stream> files) {
		using ZipFile zipFile = ZipFile.Read(s);
		foreach (ZipEntry entry in zipFile) {
			if (entry.IsDirectory)
				continue;

			Stream dataStream = new MemStream();
			entry.Extract(dataStream);
			dataStream.Position = 0;
			files.Add(entry.FileName, dataStream);
		}
	}

	// Search like... (for O2R/ZIP files)
	public static void OnlyFromO2R
	(string fileName, Stream s, ref Dictionary<string, Stream> files) {
		using ZipFile zipFile = ZipFile.Read(s);
		foreach (ZipEntry entry in zipFile) {
			if (entry.IsDirectory)
				continue;

			if (entry.FileName.Contains(fileName)) {
				Stream dataStream = new MemStream();
				entry.Extract(dataStream);
				dataStream.Position = 0;
				files.Add(entry.FileName, dataStream);
			}
		}
	}
}
