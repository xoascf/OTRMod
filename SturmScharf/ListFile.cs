using System.Collections.Generic;
using System.IO;

namespace SturmScharf; 
public sealed class ListFile {
	public const string FileName = "(listfile)";

	internal ListFile() {
	}

	internal ListFile(StreamReader reader) {
		ReadFrom(reader);
	}

	public List<string> FileNames { get; private set; } = new();

	internal void ReadFrom(StreamReader reader) {
		while (!reader.EndOfStream) {
			string? fileName = reader.ReadLine();
			if (!fileName.IsNullOrEmpty())
				FileNames.Add(fileName);
		}
	}

	internal void WriteTo(StreamWriter writer) {
		foreach (string fileName in FileNames)
			writer.WriteLine(fileName);
	}
}