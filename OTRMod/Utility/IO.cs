/* Licensed under the Open Software License version 3.0 */

using OTRMod.OTR;
using SturmScharf;
using System.IO;

namespace OTRMod.Utility;

internal static class IO {
	public static byte[] Get(this byte[] input, int start, int length) {
		if (start < 0 || start >= input.Length || length <= 0) /* Invalid! */
#if NET40
			return new byte[0];
#else
			return Array.Empty<byte>();
#endif
		if (start + length > input.Length)
			length = input.Length - start; /* If exceeds the size take it all! */

		byte[] result = new byte[length];
		Array.Copy(input, start, result, 0, length);

		return result;
	}

	public static byte[] GetAllFrom(byte[] input, int start) {
		int length = input.Length - start;

		return input.Get(start, length);
	}

	public static void Set(this byte[] array, int offset, object newData) {
		if (offset < 0 || offset >= array.Length || newData == null)
			return; /* Invalid! */

		switch (newData) {
			case byte[] bytes:
				int length = Math.Min(array.Length - offset, bytes.Length);
				Array.Copy(bytes, 0, array, offset, length);
				break;

			case byte data:
				array[offset] = data;
				break;
		}
	}

#if NETCOREAPP2_1_OR_GREATER
	public static Span<T> Slice<T>(this T[] input, int start)
		=> new(input, start, input.Length - start);

	public static Span<T> Slice<T>(this T[] input, int start, int length)
		=> new(input, start, length);
#else
	public static ArraySegment<T> Slice<T>(this T[] input, int start) {
		if (input == null)
			throw new ArgumentNullException(nameof(input));

		if (start < 0 || start > input.Length)
			throw new ArgumentOutOfRangeException(nameof(start));

		return new ArraySegment<T>(input, start, input.Length - start);
	}

	public static ArraySegment<T> Slice<T>(this T[] input, int start, int length) {
		if (input == null)
			throw new ArgumentNullException(nameof(input));

		if (start < 0 || start > input.Length)
			throw new ArgumentOutOfRangeException(nameof(start));

		if (length < 0 || start + length > input.Length)
			throw new ArgumentOutOfRangeException(nameof(length));

		return new ArraySegment<T>(input, start, length);
	}
#endif

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