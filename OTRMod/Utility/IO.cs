/* Licensed under the Open Software License version 3.0 */

using OTRMod.OTR;
using War3Net.IO.Mpq;
using System.IO;

namespace OTRMod.Utility;

internal static class IO {
	public static byte[] Get(this byte[] input, int start, int length) {
		byte[] bytes = new byte[length];

		using MemoryStream s = new(input); s.Seek(start, SeekOrigin.Begin);
		_ = s.Read(bytes, 0, length);

		return bytes;
	}

	public static byte[] GetAllFrom(byte[] input, int start) {
		int length = input.Length - start;

		return input.Get(start, length);
	}

	public static void Set(this byte[] array, int offset, object newData) {
		using MemoryStream s = new(array);
		s.Seek(offset, SeekOrigin.Begin);

		switch (newData) {
			case byte[] bytes: foreach (byte t in bytes) s.WriteByte(t);
				break;

			case byte data: s.WriteByte(data);
				break;
		}
	}

	public static Span<T> Slice<T>(this T[] input, int start) {
		return new(input, start, input.Length - start);
	}

	public static Span<T> Slice<T>(this T[] input, int start, int length) {
		return new(input, start, length);
	}

	public static string Concatenate(params string[] paths) {
		string newPath = paths[0];

		for (int i = 1; i < paths.Length; i++)
			newPath = Path.Combine(newPath, paths[i]);

		return newPath;
	}

	public static void Save(byte[] data, string path) => Generate.AddFile(path, data);

	public static void Add(this MpqArchiveBuilder ab, Stream stream, string fileName) {
		MpqFile file = MpqFile.New(stream, fileName);

		ab.AddFile(file);
	}
}