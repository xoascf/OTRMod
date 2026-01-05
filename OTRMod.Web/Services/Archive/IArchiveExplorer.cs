namespace OTRMod.Web.Services.Archive;

/// <summary>
/// Interface for exploring OTR/O2R archives.
/// </summary>
public interface IArchiveExplorer {
	/// <summary>Currently loaded archive name.</summary>
	string? ArchiveName { get; }

	/// <summary>Currently loaded archive format.</summary>
	ArchiveFormat? Format { get; }

	/// <summary>Whether an archive is currently loaded.</summary>
	bool IsLoaded { get; }

	/// <summary>Event raised when archive changes.</summary>
	event Action? OnArchiveChanged;

	/// <summary>Load an archive from a stream.</summary>
	Task LoadAsync(Stream stream, string fileName);

	/// <summary>Get all entries in the archive.</summary>
	IReadOnlyList<ArchiveEntry> GetEntries();

	/// <summary>Get entries in a specific directory.</summary>
	IReadOnlyList<ArchiveEntry> GetEntries(string directory);

	/// <summary>Get file content as bytes.</summary>
	Task<byte[]?> GetContentAsync(string path);

	/// <summary>Analyze a resource and get its info (metadata only, fast).</summary>
	ResourceInfo? AnalyzeMetadata(string path);

	/// <summary>Get texture object for preview generation.</summary>
	Task<OTRMod.Z.Texture?> GetTextureAsync(string path);

	/// <summary>Close the current archive.</summary>
	void Close();
}