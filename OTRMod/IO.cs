/* Licensed under the Open Software License version 3.0 */
// From OpenOcarinaBuilder.

using War3Net.IO.Mpq;

namespace OTRMod;

internal static class IO
{
	public static byte[] GetFrom(byte[] input, int start, int length)
	{
		byte[] bytes = new byte[length];
		using (MemoryStream s = new MemoryStream(input))
		{
			s.Seek(start, SeekOrigin.Begin);
			_ = s.Read(bytes, 0, length);
		}

		return bytes;
	}

	public static byte[] GetAllFrom(byte[] input, int start)
	{
		int length = input.Length - start;

		return GetFrom(input, start, length);
	}

	// Substitutes (overwrites) anything after offset with new data.
	public static void Set(this byte[] array, int offset, byte[] newData)
	{
		using (MemoryStream s = new MemoryStream(array))
		{
			s.Seek(offset, SeekOrigin.Begin);
			for (int i = 0; i < newData.Length; i++)
			{
				s.WriteByte(newData[i]);
			}
		}
	}

	public static Span<T> Slice<T>(this T[] input, int start)
	{
		return input.AsSpan().Slice(start);
	}

	public static Span<T> Slice<T>(this T[] input, int start, int length)
	{
		return input.AsSpan().Slice(start, length);
	}

	public static string Concatenate(params string[] paths)
	{
		string newPath = paths[0];
		for (int i = 1; i < paths.Length; i++)
			newPath = Path.Combine(newPath, paths[i]);

		return newPath;
	}

	public static void Save
		(byte[] bytes, string output, ref Dictionary<string, byte[]> fileList)
	{ fileList.Add(output, bytes); }

	public static void Add(this MpqArchiveBuilder ab, Stream s, string destiny)
	{
		MpqFile file = MpqFile.New(s, destiny);
		ab.AddFile(file);
	}
}