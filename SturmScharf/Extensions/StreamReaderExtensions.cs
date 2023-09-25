using System.IO;

namespace SturmScharf.Extensions; 
public static class StreamReaderExtensions {
	public static ListFile ReadListFile(this StreamReader reader) => new(reader);
}