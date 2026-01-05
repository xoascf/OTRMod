namespace OTRMod.Web.Services;

/// <summary>
/// Interface for formatting file sizes.
/// </summary>
public interface IFileSizeFormatter {
	/// <summary>
	/// Formats a file size in bytes to a human-readable string.
	/// </summary>
	string Format(long bytes);
}

/// <summary>
/// Formats file sizes into human-readable strings.
/// </summary>
public sealed class FileSizeFormatter : IFileSizeFormatter {
	private const long KiB = 1024;
	private const long MiB = KiB * 1024;
	private const long GiB = MiB * 1024;

	public string Format(long bytes) {
		return bytes switch {
			< KiB => $"{bytes} B",
			< MiB => $"{bytes / (double)KiB:F1} KiB",
			< GiB => $"{bytes / (double)MiB:F1} MiB",
			_ => $"{bytes / (double)GiB:F1} GiB"
		};
	}
}