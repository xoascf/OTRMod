using System.IO;

namespace SturmScharf.Extensions;

public static class StreamWriterExtensions {
	public static void WriteListFile(this StreamWriter writer, ListFile listFile) => listFile.WriteTo(writer);
}