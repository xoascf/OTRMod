namespace SturmScharf;

[Flags]
public enum MpqCompressionType {
	Huffman = 0x01,

	ZLib = 0x02,

	PKLib = 0x08,

	BZip2 = 0x10,

	/// <summary>
	/// Lempel–Ziv–Markov chain.
	/// </summary>
	Lzma = 0x12,

	Sparse = 0x20,

	ImaAdpcmMono = 0x40,

	ImaAdpcmStereo = 0x80
}