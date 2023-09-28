/* Licensed under the Open Software License version 3.0 */

using SturmScharf;
using System.IO;
using MemStream = System.IO.MemoryStream;

namespace OTRMod.OTR;

public static class Load {
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
}
