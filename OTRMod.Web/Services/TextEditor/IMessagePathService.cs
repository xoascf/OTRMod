namespace OTRMod.Web.Services.TextEditor;

/// <summary>
/// Service for managing message file paths, including override path transformation.
/// </summary>
public interface IMessagePathService {
	/// <summary>
	/// Transforms a standard message path to an override path.
	/// </summary>
	/// <param name="standardPath">The standard path (e.g., "text/nes_message_data_static/nes_message_data_static")</param>
	/// <returns>The override path with a generated GUID (e.g., "override/text/nes_message_data_static/{guid}")</returns>
	string ToOverridePath(string standardPath);

	/// <summary>
	/// Transforms an override path back to a standard path.
	/// </summary>
	/// <param name="overridePath">The override path</param>
	/// <returns>The standard path, or the original path if it's not an override path</returns>
	string ToStandardPath(string overridePath);

	/// <summary>
	/// Checks if a path is an override path.
	/// </summary>
	/// <param name="path">The path to check</param>
	/// <returns>True if the path starts with "override/"</returns>
	bool IsOverridePath(string path);

	/// <summary>
	/// Extracts the message type from a path (e.g., "nes_message_data_static" from "text/nes_message_data_static/...")
	/// </summary>
	/// <param name="path">The path to extract from</param>
	/// <returns>The message type, or null if not found</returns>
	string? ExtractMessageType(string path);

	/// <summary>
	/// Normalizes a path by replacing backslashes with forward slashes.
	/// </summary>
	/// <param name="path">The path to normalize</param>
	/// <returns>The normalized path</returns>
	string NormalizePath(string path);

	/// <summary>
	/// Extracts the directory path from a full file path (removes the filename).
	/// </summary>
	/// <param name="fullPath">The full path including filename</param>
	/// <returns>The directory path without the filename</returns>
	string GetDirectoryPath(string fullPath);
}