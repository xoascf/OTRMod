using System.IO;

namespace SturmScharf.Extensions;

public static class BinaryWriterExtensions {
	public static void Write(this BinaryWriter writer, Attributes attributes) {
		attributes.WriteTo(writer);
	}
}