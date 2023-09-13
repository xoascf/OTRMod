using System.IO;

using Ionic.BZip2;

namespace SturmScharf.Compression; 
/// <summary>
/// Provides methods to decompress BZip2 compressed data.
/// </summary>
public static class BZip2Compression {
	/// <summary>
	/// Decompresses the input data.
	/// </summary>
	/// <param name="data">Byte array containing compressed data.</param>
	/// <param name="expectedLength">The expected length (in bytes) of the decompressed data.</param>
	/// <returns>Byte array containing the decompressed data.</returns>
	public static byte[] Decompress(byte[] data, uint expectedLength) {
		using MemoryStream memoryStream = new(data);
		return Decompress(memoryStream, expectedLength);
	}

	/// <summary>
	/// Decompresses the input stream.
	/// </summary>
	/// <param name="data">Stream containing compressed data.</param>
	/// <param name="expectedLength">The expected length (in bytes) of the decompressed data.</param>
	/// <returns>Byte array containing the decompressed data.</returns>
	public static byte[] Decompress(Stream data, uint expectedLength) {
		using (MemoryStream output = new((int)expectedLength)) {
#if true
			using BZip2InputStream bZip2InputStream = new(data, true);
			bZip2InputStream.CopyTo(output);
#else
			ICSharpCode.SharpZipLib.BZip2.BZip2.Decompress(data, output, false);
#endif
			return output.ToArray();
		}
	}
}