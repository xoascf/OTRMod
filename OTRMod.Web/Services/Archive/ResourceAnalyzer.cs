using OTRMod.ID;
using OTRMod.Utility;

namespace OTRMod.Web.Services.Archive;

/// <summary>
/// Service for analyzing OTR/O2R resource data.
/// </summary>
public interface IResourceAnalyzer {
	/// <summary>
	/// Analyzes raw resource data and extracts metadata only (fast, no decoding).
	/// </summary>
	ResourceInfo? AnalyzeMetadata(byte[] data);

	/// <summary>
	/// Gets texture object for preview/download.
	/// </summary>
	OTRMod.Z.Texture? GetTexture(byte[] data);

	/// <summary>
	/// Gets background object for preview/download.
	/// </summary>
	OTRMod.Z.Background? GetBackground(byte[] data);
}

/// <summary>
/// Implementation of resource analyzer.
/// </summary>
public sealed class ResourceAnalyzer : IResourceAnalyzer {
	private const int HeaderSize = 0x40;

	public ResourceInfo? AnalyzeMetadata(byte[] data) {
		if (data == null || data.Length < HeaderSize)
			return null;

		var type = (ResourceType)data.ToI32(0x04, false);
		var version = data.ToI32(0x08, false);
		var isModded = data[0x18] == 1;

		var info = new ResourceInfo(type, version, isModded);

		return type switch {
			ResourceType.Texture => info with { Texture = AnalyzeTextureMetadata(data) },
			ResourceType.Background => info with { IsBackground = true },
			ResourceType.Text => info with { Text = AnalyzeTextMetadata(data) },
			_ => info
		};
	}

	public OTRMod.Z.Texture? GetTexture(byte[] data) {
		try {
			var res = OTRMod.Z.Resource.Read(data);
			if (res.Type != ResourceType.Texture)
				return null;

			return OTRMod.Z.Texture.LoadFrom(res);
		}
		catch {
			return null;
		}
	}

	public OTRMod.Z.Background? GetBackground(byte[] data) {
		try {
			var res = OTRMod.Z.Resource.Read(data);
			if (res.Type != ResourceType.Background)
				return null;

			return OTRMod.Z.Background.LoadFrom(res);
		}
		catch {
			return null;
		}
	}

	private static TextureInfo? AnalyzeTextureMetadata(byte[] data) {
		try {
			int off = HeaderSize;
			return new TextureInfo(
				(OTRMod.ID.Texture.Codec)data.ToI32(off, false),
				data.ToI32(off + 4, false),
				data.ToI32(off + 8, false));
		}
		catch { return null; }
	}

	private static TextInfo? AnalyzeTextMetadata(byte[] data) {
		try {
			// For text, we need to parse to get message count
			// This is acceptable as text resources are typically small
			var text = OTRMod.Z.Text.LoadFrom(OTRMod.Z.Resource.Read(data));
			return new TextInfo(text.Entries.Count);
		}
		catch { return null; }
	}
}