using System.Text.RegularExpressions;

namespace OTRMod.Web.Services;

/// <summary>
/// Implementation of IMessagePathService for managing message file paths.
/// </summary>
public class MessagePathService : IMessagePathService {
	private const string OverridePrefix = "override/";
	private static readonly Regex MessageTypePattern = new(@"text/([^/]+)/", RegexOptions.Compiled);

	public string ToOverridePath(string standardPath) {
		if (string.IsNullOrWhiteSpace(standardPath))
			throw new ArgumentException("Path cannot be null or empty", nameof(standardPath));

		var normalized = NormalizePath(standardPath);

		// If already an override path, return as-is
		if (IsOverridePath(normalized))
			return normalized;

		// Extract message type from path
		var messageType = ExtractMessageType(normalized);
		if (string.IsNullOrEmpty(messageType)) {
			// If we can't extract message type, use the last segment before the filename
			var parts = normalized.Split('/', StringSplitOptions.RemoveEmptyEntries);
			if (parts.Length >= 2)
				messageType = parts[^2];
			else
				messageType = "message_data_static"; // Default fallback
		}

		// Generate a new GUID for the override file
		var guid = Guid.NewGuid().ToString("D");

		// Construct override path: override/text/{messageType}/{guid}
		return $"{OverridePrefix}text/{messageType}/{guid}";
	}

	public string ToStandardPath(string overridePath) {
		if (string.IsNullOrWhiteSpace(overridePath))
			throw new ArgumentException("Path cannot be null or empty", nameof(overridePath));

		var normalized = NormalizePath(overridePath);

		if (!IsOverridePath(normalized))
			return normalized;

		// Remove "override/" prefix
		var withoutOverride = normalized.Substring(OverridePrefix.Length);

		// Extract message type
		var messageType = ExtractMessageType(withoutOverride);
		if (string.IsNullOrEmpty(messageType))
			return withoutOverride;

		// Return standard format: text/{messageType}/{messageType}
		return $"text/{messageType}/{messageType}";
	}

	public bool IsOverridePath(string path) {
		if (string.IsNullOrWhiteSpace(path))
			return false;

		var normalized = NormalizePath(path);
		return normalized.StartsWith(OverridePrefix, StringComparison.OrdinalIgnoreCase);
	}

	public string? ExtractMessageType(string path) {
		if (string.IsNullOrWhiteSpace(path))
			return null;

		var normalized = NormalizePath(path);
		var match = MessageTypePattern.Match(normalized);

		return match.Success ? match.Groups[1].Value : null;
	}

	public string NormalizePath(string path) {
		if (string.IsNullOrWhiteSpace(path))
			return path;

		return path.Replace('\\', '/').Trim();
	}

	public string GetDirectoryPath(string fullPath) {
		if (string.IsNullOrWhiteSpace(fullPath))
			return string.Empty;

		var normalized = NormalizePath(fullPath);
		var lastSlash = normalized.LastIndexOf('/');

		if (lastSlash < 0)
			return string.Empty; // No directory, just filename

		return normalized.Substring(0, lastSlash);
	}
}