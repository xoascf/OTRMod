namespace OTRMod.Web.Services;

/// <summary>
/// Implementation of IModFileNameService for generating mod file names.
/// </summary>
public class ModFileNameService : IModFileNameService {
	private readonly IMessagePathService _pathService;

	public ModFileNameService(IMessagePathService pathService) {
		_pathService = pathService ?? throw new ArgumentNullException(nameof(pathService));
	}

	public string GenerateFileName(string messagePath, OutputFormat outputFormat, bool isOverride) {
		if (string.IsNullOrWhiteSpace(messagePath))
			return GetDefaultFileName(outputFormat, isOverride);

		var normalized = _pathService.NormalizePath(messagePath);
		var messageType = _pathService.ExtractMessageType(normalized);

		if (string.IsNullOrEmpty(messageType))
			return GetDefaultFileName(outputFormat, isOverride);

		var prefix = isOverride ? "Override" : "Generated";
		var extension = outputFormat == OutputFormat.O2R ? ".o2r" : ".otr";

		// Convert message type to a more readable format (e.g., "nes_message_data_static" -> "NesMessageDataStatic")
		var readableType = ToPascalCase(messageType);

		return $"{prefix}{readableType}{extension}";
	}

	private static string GetDefaultFileName(OutputFormat outputFormat, bool isOverride) {
		var prefix = isOverride ? "GeneratedOverrideMessages" : "GeneratedMessages";
		var extension = outputFormat == OutputFormat.O2R ? ".o2r" : ".otr";
		return $"{prefix}{extension}";
	}

	private static string ToPascalCase(string input) {
		if (string.IsNullOrWhiteSpace(input))
			return input;

		var parts = input.Split(new[] { '_', '-' }, StringSplitOptions.RemoveEmptyEntries);
		var result = string.Join("", parts.Select(part =>
			char.ToUpperInvariant(part[0]) + (part.Length > 1 ? part.Substring(1).ToLowerInvariant() : "")));

		return result;
	}
}