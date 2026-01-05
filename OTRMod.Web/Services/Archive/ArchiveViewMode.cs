namespace OTRMod.Web.Services.Archive;

/// <summary>
/// Defines the view mode for displaying files in the archive browser.
/// </summary>
public enum ArchiveViewMode {
	/// <summary>
	/// Simple list view showing file names.
	/// </summary>
	List,

	/// <summary>
	/// Detailed view showing file names, types, and sizes in a table.
	/// </summary>
	Details,

	/// <summary>
	/// Grid/tile view with icons.
	/// </summary>
	Grid
}