using OTRMod.ID;
using static OTRMod.ID.Texture;

namespace OTRMod.Web.Services.Archive;

/// <summary>
/// Analyzed resource information extracted from archive entry data.
/// </summary>
public sealed record ResourceInfo(
	ResourceType Type,
	int Version,
	bool IsModded) {
	/// <summary>Texture-specific info (null if not a texture).</summary>
	public TextureInfo? Texture { get; init; }

	/// <summary>Text-specific info (null if not text).</summary>
	public TextInfo? Text { get; init; }

	/// <summary>Whether this is a background resource (JPEG image).</summary>
	public bool IsBackground { get; init; }

	/// <summary>Whether this resource can be previewed as an image.</summary>
	public bool HasImagePreview => Texture != null || IsBackground;

	/// <summary>Gets a human-readable type name.</summary>
	public string TypeName => Type switch {
		ResourceType.Texture => "Texture",
		ResourceType.Text => "Text",
		ResourceType.Animation => "Animation",
		ResourceType.Audio => "Audio",
		ResourceType.AudioSample => "Audio Sample",
		ResourceType.AudioSoundFont => "Sound Font",
		ResourceType.AudioSequence => "Sequence",
		ResourceType.Skeleton => "Skeleton",
		ResourceType.DisplayList => "Display List",
		ResourceType.Vertex => "Vertex",
		ResourceType.Cutscene => "Cutscene",
		ResourceType.Background => "Background",
		ResourceType.Path => "Path",
		ResourceType.Room => "Room",
		ResourceType.CollisionHeader => "Collision",
		ResourceType.Blob => "Binary Data",
		_ => Type.ToString()
	};

	/// <summary>Gets a Font Awesome icon class for the resource type.</summary>
	public string IconClass => Type switch {
		ResourceType.Texture or ResourceType.Background => "fa-image",
		ResourceType.Text => "fa-file-lines",
		ResourceType.Animation or ResourceType.PlayerAnimation => "fa-person-running",
		ResourceType.Audio or ResourceType.AudioSample or ResourceType.AudioSoundFont or ResourceType.AudioSequence => "fa-music",
		ResourceType.Skeleton or ResourceType.SkeletonLimb => "fa-bone",
		ResourceType.DisplayList or ResourceType.Vertex or ResourceType.Matrix => "fa-cube",
		ResourceType.Cutscene => "fa-film",
		ResourceType.Path => "fa-route",
		ResourceType.Room => "fa-door-open",
		ResourceType.CollisionHeader => "fa-shield",
		_ => "fa-file"
	};
}

/// <summary>Texture-specific metadata.</summary>
public sealed record TextureInfo(
	Codec Codec,
	int Width,
	int Height) {
	public string CodecName => Codec switch {
		Codec.RGBA32 => "RGBA32",
		Codec.RGBA16 => "RGBA16",
		Codec.CI4 => "CI4 (4-bit Palette)",
		Codec.CI8 => "CI8 (8-bit Palette)",
		Codec.I4 => "I4 (4-bit Grayscale)",
		Codec.I8 => "I8 (8-bit Grayscale)",
		Codec.IA4 => "IA4 (4-bit Gray+Alpha)",
		Codec.IA8 => "IA8 (8-bit Gray+Alpha)",
		Codec.IA16 => "IA16 (16-bit Gray+Alpha)",
		_ => Codec.ToString()
	};

	public string Dimensions => $"{Width}x{Height}"; // TODO: Localize!!
}

/// <summary>Text-specific metadata.</summary>
public sealed record TextInfo(int MessageCount);