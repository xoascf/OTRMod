using OTRMod.ID;

namespace OTRMod.Web.Services.Archive;

/// <summary>
/// Archive format type.
/// </summary>
public enum ArchiveFormat {
	/// <summary>OTR format (MPQ-based).</summary>
	OTR,

	/// <summary>O2R format (ZIP-based).</summary>
	O2R
}

/// <summary>
/// Represents an entry within an OTR/O2R archive.
/// </summary>
public sealed record ArchiveEntry(
	string Name,
	string Path,
	long Size,
	bool IsDirectory) {
	/// <summary>Resource info (populated after analysis).</summary>
	public ResourceInfo? ResourceInfo { get; init; }

	/// <summary>Checks if this is a text resource.</summary>
	public bool IsTextResource => ResourceInfo?.Type == ResourceType.Text;

	/// <summary>Checks if this is a texture resource.</summary>
	public bool IsTexture => ResourceInfo?.Type == ResourceType.Texture;

	/// <summary>Gets the icon class based on resource type.</summary>
	public string IconClass => ResourceInfo?.IconClass ?? (IsDirectory ? "fa-folder" : "fa-file");
}