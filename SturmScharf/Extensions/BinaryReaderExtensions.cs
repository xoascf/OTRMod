using System.IO;

namespace SturmScharf.Extensions;

public static class BinaryReaderExtensions {
	public static Attributes ReadAttributes(this BinaryReader reader) => new(reader);
}